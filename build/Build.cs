using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.AzureSignTool;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Octokit;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.CompressionTasks;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[GitHubActions(
    "ci",
    GitHubActionsImage.WindowsLatest,
    AutoGenerate = true,
    OnPushBranches = new[] { "main", "release/*" },
    OnPullRequestBranches = new[] { "main" },
    InvokedTargets = new[] { nameof(Compile), nameof(SignArtifacts) },
    ImportSecrets = new[]
    {
        nameof(AzureKeyVaultUrl),
        nameof(AzureKeyVaultClientId),
        nameof(AzureKeyVaultClientSecret),
        nameof(AzureKeyVaultCertificate)
    }
)]
[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);

    protected override void OnBuildInitialized()
    {
        base.OnBuildInitialized();

        ProcessTasks.DefaultLogInvocation = true;
        ProcessTasks.DefaultLogOutput = true;

        if (GitVersion is null)
        {
            throw new Exception("Could not initialize GitVersion.");
        }

        Logger.Block("Info");

        Logger.Info("IsLocalBuild           : {0}", IsLocalBuild.ToString());
        Logger.Info("Branch                 : {0}", GitRepository.Branch);
        Logger.Info("Configuration          : {0}", Configuration);

        Logger.Info("Informational   Version: {0}", GitVersion.InformationalVersion);
        Logger.Info("SemVer          Version: {0}", GitVersion.SemVer);
        Logger.Info("AssemblySemVer  Version: {0}", GitVersion.AssemblySemVer);
        Logger.Info("MajorMinorPatch Version: {0}", GitVersion.MajorMinorPatch);
        Logger.Info("NuGet           Version: {0}", GitVersion.NuGetVersion);
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")] readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution(GenerateProjects = true)] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion(Framework = "netcoreapp3.1")] readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath OutputDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            try
            {
                SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(EnsureCleanDirectory);
                TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(EnsureCleanDirectory);
                OutputDirectory.GlobDirectories("*.*").ForEach(EnsureCleanDirectory);
            }
            catch
            {
            }
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetVersion(GitRepository.IsOnReleaseBranch() ? GitVersion.MajorMinorPatch : GitVersion.NuGetVersion)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore()
            );
        });

    Target Publish => _ => _
        .DependsOn(Compile)
        .OnlyWhenStatic(() => GitRepository.IsOnMainBranch() || GitRepository.IsOnReleaseBranch())
        .OnlyWhenStatic(() => EnvironmentInfo.IsWin)
        .Executes(() =>
        {
            DotNetPublish(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .SetVersion(GitRepository.IsOnReleaseBranch() ? GitVersion.MajorMinorPatch : GitVersion.NuGetVersion)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetFramework("netcoreapp3.1")
                .SetRuntime("win-x64")
                .SetPublishTrimmed(false)
                .SetSelfContained(true)
                .SetPublishSingleFile(true)
            );

            DotNetPublish(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .SetVersion(GitRepository.IsOnReleaseBranch() ? GitVersion.MajorMinorPatch : GitVersion.NuGetVersion)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetFramework("net5.0-windows")
                .SetRuntime("win-x64")
                .SetPublishTrimmed(false)
                .SetSelfContained(true)
                .SetPublishSingleFile(true)
                .SetProperty("IncludeNativeLibrariesForSelfExtract", true)
                .SetProperty("IncludeAllContentForSelfExtract", true)
            );
        });

    Target CompressArtifacts => _ => _
        .DependsOn(Publish)
        .OnlyWhenStatic(() => GitRepository.IsOnMainBranch() || GitRepository.IsOnReleaseBranch())
        .OnlyWhenStatic(() => EnvironmentInfo.IsWin)
        .Executes(() =>
        {
            Compress(SourceDirectory / "IconPacks.Browser" / "bin" / Configuration / "net47", OutputDirectory / "net47" / $"IconPacks.Browser-v{GitVersion.NuGetVersion}.zip");
            Compress(SourceDirectory / "IconPacks.Browser" / "bin" / Configuration / "netcoreapp3.1" / "win-x64" / "publish", OutputDirectory / "netcoreapp3.1" / $"IconPacks.Browser-v{GitVersion.NuGetVersion}.zip");
            Compress(SourceDirectory / "IconPacks.Browser" / "bin" / Configuration / "net5.0-windows" / "win-x64" / "publish", OutputDirectory / "net5.0-windows" / $"IconPacks.Browser-v{GitVersion.NuGetVersion}.zip");
        });

    Target SignArtifacts => _ => _
        .DependsOn(Publish)
        .OnlyWhenStatic(() => GitRepository.IsOnMainBranch() || GitRepository.IsOnReleaseBranch())
        .OnlyWhenStatic(() => EnvironmentInfo.IsWin)
        .Executes(() =>
        {
            SignFiles(
                new[]
                {
                    (SourceDirectory / "IconPacks.Browser" / "bin" / Configuration / "net47" / "IconPacks.Browser.exe").ToString(),
                    (SourceDirectory / "IconPacks.Browser" / "bin" / Configuration / "netcoreapp3.1" / "win-x64" / "publish" / "IconPacks.Browser.exe").ToString(),
                    (SourceDirectory / "IconPacks.Browser" / "bin" / Configuration / "net5.0-windows" / "win-x64" / "publish" / "IconPacks.Browser.exe").ToString()
                }
                , "IconPacks Browser"
                , GitRepository.HttpsUrl);
        });

    [Parameter] [Secret] readonly string AzureKeyVaultUrl;
    [Parameter] [Secret] readonly string AzureKeyVaultClientId;
    [Parameter] [Secret] readonly string AzureKeyVaultClientSecret;
    [Parameter] [Secret] readonly string AzureKeyVaultCertificate;

    void SignFiles(IEnumerable<string> files, string description, string descriptionUrl)
    {
        var azureSignToolSettings = new CustomAzureSignToolSettings()
                .SetFiles(files)
                .SetFileDigest(AzureSignToolDigestAlgorithm.sha256)
                .SetDescription(description)
                .SetDescriptionUrl(descriptionUrl)
                .ToggleNoPageHashing()
                .SetTimestampRfc3161Url("http://timestamp.digicert.com")
                .SetTimestampDigest(AzureSignToolDigestAlgorithm.sha256)
                .SetKeyVaultUrl(EnvironmentInfo.GetParameter<string>(nameof(AzureKeyVaultUrl)))
                .SetKeyVaultClientId(EnvironmentInfo.GetParameter<string>(nameof(AzureKeyVaultClientId)))
                .SetKeyVaultClientSecret(EnvironmentInfo.GetParameter<string>(nameof(AzureKeyVaultClientSecret)))
                .SetKeyVaultCertificateName(EnvironmentInfo.GetParameter<string>(nameof(AzureKeyVaultCertificate)))
            ;

        AzureSignToolTasks.AzureSignTool(azureSignToolSettings);
    }
}