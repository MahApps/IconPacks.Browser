using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.AzureSignTool;

// TODO: remove this class on next nuke version
public class CustomAzureSignToolSettings : AzureSignToolSettings
{
    /// <summary>
    ///   Files to sign.
    /// </summary>
    public virtual IReadOnlyList<string> Files => FilesInternal.AsReadOnly();

    internal List<string> FilesInternal { get; set; } = new List<string>();

    protected override Arguments ConfigureProcessArguments(Arguments arguments)
    {
        arguments =
            base.ConfigureProcessArguments(arguments)
                .Add("{value}", Files);
        return base.ConfigureProcessArguments(arguments);
    }
}

public static class CustomAzureSignToolSettingsExtensions
{
    /// <summary>
    ///   <p><em>Sets <see cref="CustomAzureSignToolSettings.Files"/> to a new list</em></p>
    ///   <p>Files to sign.</p>
    /// </summary>
    [Pure]
    public static T SetFiles<T>(this T toolSettings, params string[] files) where T : CustomAzureSignToolSettings
    {
        toolSettings = toolSettings.NewInstance();
        toolSettings.FilesInternal = files.ToList();
        return toolSettings;
    }

    /// <summary>
    ///   <p><em>Sets <see cref="CustomAzureSignToolSettings.Files"/> to a new list</em></p>
    ///   <p>Files to sign.</p>
    /// </summary>
    [Pure]
    public static T SetFiles<T>(this T toolSettings, IEnumerable<string> files) where T : CustomAzureSignToolSettings
    {
        toolSettings = toolSettings.NewInstance();
        toolSettings.FilesInternal = files.ToList();
        return toolSettings;
    }
}