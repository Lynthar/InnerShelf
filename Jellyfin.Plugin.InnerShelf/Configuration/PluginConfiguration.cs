using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.InnerShelf.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the JavBus source is enabled.
    /// </summary>
    public bool EnableJavBus { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the FANZA source is enabled.
    /// </summary>
    public bool EnableFanza { get; set; } = true;

    /// <summary>
    /// Gets or sets the MetaTube server URL. Empty string disables MetaTube integration.
    /// </summary>
    public string MetaTubeServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MetaTube API token.
    /// </summary>
    public string MetaTubeToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title display template.
    /// Supported placeholders: {code}, {title}.
    /// </summary>
    public string TitleTemplate { get; set; } = "{code} {title}";

    /// <summary>
    /// Gets or sets the HTTP proxy URL for metadata source requests.
    /// </summary>
    public string HttpProxy { get; set; } = string.Empty;
}
