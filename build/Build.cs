using System;
using System.Collections.Generic;
using System.IO;
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
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Octokit;
using Serilog;
using static Nuke.Common.IO.CompressionTasks;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[GitHubActions(
    "ci",
    GitHubActionsImage.WindowsLatest,
    AutoGenerate = true,
    OnPushBranches = new[] { "main", "develop" },
    OnPullRequestBranches = new[] { "main", "develop" },
    InvokedTargets = new[] { nameof(Compile) }
)]
[GitHubActions(
    "ci-deploy",
    GitHubActionsImage.WindowsLatest,
    AutoGenerate = true,
    OnPushTags = new[] { "*" },
    InvokedTargets = new[] { nameof(Compile), nameof(CreateRelease) },
    ImportSecrets = new[]
    {
        nameof(GithubToken),
        nameof(AzureKeyVaultUrl),
        nameof(AzureKeyVaultClientId),
        nameof(AzureKeyVaultTenantId),
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

        Log.Information("IsLocalBuild           : {IsLocalBuild}", IsLocalBuild.ToString());
        Log.Information("Branch                 : {Branch}", GitRepository.Branch);
        Log.Information("Configuration          : {Configuration}", Configuration.ToString());

        Log.Information("Informational   Version: {InformationalVersion}", GitVersion.InformationalVersion);
        Log.Information("SemVer          Version: {SemVer}", GitVersion.SemVer);
        Log.Information("AssemblySemVer  Version: {AssemblySemVer}", GitVersion.AssemblySemVer);
        Log.Information("MajorMinorPatch Version: {MajorMinorPatch}", GitVersion.MajorMinorPatch);
        Log.Information("NuGet           Version: {NuGetVersion}", GitVersion.NuGetVersion);
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")] readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution(GenerateProjects = true)] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion(Framework = "net5.0")] readonly GitVersion GitVersion;

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
                Log.Warning("Can not clean folders");
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
        .DependsOn(Compile)
        .DependsOn(Publish)
        .OnlyWhenStatic(() => EnvironmentInfo.IsWin)
        .Executes(() =>
        {
            Compress(SourceDirectory / "IconPacks.Browser" / "bin" / Configuration / "net47", OutputDirectory / $"IconPacks.Browser-net47-v{GitVersion.NuGetVersion}.zip");
            Compress(SourceDirectory / "IconPacks.Browser" / "bin" / Configuration / "net5.0-windows" / "win-x64" / "publish", OutputDirectory / $"IconPacks.Browser-net50-v{GitVersion.NuGetVersion}.zip");
        });

    Target SignArtifacts => _ => _
        .DependsOn(Publish)
        .OnlyWhenStatic(() => EnvironmentInfo.IsWin)
        .Executes(() =>
        {
            var files = SourceDirectory.GlobFiles("**/bin/**/IconPacks.Browser.exe").Select(p => p.ToString());
            SignFiles(files, "IconPacks Browser", GitRepository.HttpsUrl);
        });

    Target CreateRelease => _ => _
        .DependsOn(SignArtifacts)
        .DependsOn(CompressArtifacts)
        .OnlyWhenStatic(() => EnvironmentInfo.IsWin)
        .Requires(() => GithubToken)
        .Executes(() =>
        {
            GitHubTasks.GitHubClient = new GitHubClient(new ProductHeaderValue(nameof(NukeBuild)))
            {
                Credentials = new Credentials(GithubToken)
            };

            var newRelease = new NewRelease(GitVersion.MajorMinorPatch)
            {
                TargetCommitish = GitVersion.Sha,
                Draft = true,
                Name = $"{GitRepository.GetGitHubName()} v{GitVersion.FullSemVer}",
                Prerelease = false
            };

            var createdRelease = GitHubTasks.GitHubClient.Repository.Release.Create(GitRepository.GetGitHubOwner(), GitRepository.GetGitHubName(), newRelease).Result;

            var files = OutputDirectory.GlobFiles("*.zip");
            if (files.IsEmpty())
            {
                Log.Warning("No files found in {OutputFolder}", OutputDirectory.Name);
            }

            files.ForEach(p => UploadReleaseAssetToGithub(GitHubTasks.GitHubClient, createdRelease, p));
        });

    private void UploadReleaseAssetToGithub(IGitHubClient client, Release release, AbsolutePath asset)
    {
        if (!asset.FileExists())
        {
            Log.Error("File {File} doesn't exist", asset.Name);
            return;
        }

        Log.Information("Started Uploading {FileName} to the release", asset.Name);

        using var archiveContents = File.OpenRead(asset);
        var assetUpload = new ReleaseAssetUpload()
        {
            FileName = asset.Name,
            ContentType = "application/zip",
            RawData = archiveContents
        };

        var _ = client.Repository.Release.UploadAsset(release, assetUpload).Result;

        Log.Information("Done Uploading {FileName} to the release", asset.Name);
    }

    [Parameter("GitHub Api key")] [Secret] string GithubToken = null;
    [Parameter] [Secret] readonly string AzureKeyVaultUrl;
    [Parameter] [Secret] readonly string AzureKeyVaultClientId;
    [Parameter] [Secret] readonly string AzureKeyVaultTenantId;
    [Parameter] [Secret] readonly string AzureKeyVaultClientSecret;
    [Parameter] [Secret] readonly string AzureKeyVaultCertificate;

    void SignFiles(IEnumerable<string> files, string description, string descriptionUrl)
    {
        var azureSignToolSettings = new AzureSignToolSettings()
                .SetFiles(files)
                .SetFileDigest(AzureSignToolDigestAlgorithm.sha256)
                .SetDescription(description)
                .SetDescriptionUrl(descriptionUrl)
                .ToggleNoPageHashing()
                .SetTimestampRfc3161Url("http://timestamp.digicert.com")
                .SetTimestampDigest(AzureSignToolDigestAlgorithm.sha256)
                .SetKeyVaultUrl(EnvironmentInfo.GetParameter<string>(nameof(AzureKeyVaultUrl)))
                .SetKeyVaultClientId(EnvironmentInfo.GetParameter<string>(nameof(AzureKeyVaultClientId)))
                .SetKeyVaultTenantId(EnvironmentInfo.GetParameter<string>(nameof(AzureKeyVaultTenantId)))
                .SetKeyVaultClientSecret(EnvironmentInfo.GetParameter<string>(nameof(AzureKeyVaultClientSecret)))
                .SetKeyVaultCertificateName(EnvironmentInfo.GetParameter<string>(nameof(AzureKeyVaultCertificate)))
            ;

        AzureSignToolTasks.AzureSignTool(azureSignToolSettings);
    }
}