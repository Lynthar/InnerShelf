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

    /// <summary>
    /// Gets or sets the subtitle-forge server URL. Empty string disables subtitle generation.
    /// Example: http://desktop.local:8765
    /// </summary>
    public string SubtitleForgeServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subtitle-forge bearer token, sent as Authorization header.
    /// </summary>
    public string SubtitleForgeToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comma-separated list of target subtitle languages.
    /// Each language gets its own SRT file.
    /// </summary>
    public string SubtitleLanguages { get; set; } = "zh";

    /// <summary>
    /// Gets or sets a value indicating whether bilingual subtitles should be generated.
    /// When true, source and target are merged into one SRT.
    /// </summary>
    public bool SubtitleBilingual { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the original-language SRT
    /// should be kept alongside the translated ones.
    /// </summary>
    public bool SubtitleKeepOriginal { get; set; } = true;

    /// <summary>
    /// Gets or sets the path mapping rules. Each rule rewrites a Jellyfin-side
    /// path prefix (as seen by the Jellyfin container) to a subtitle-forge-side
    /// path prefix (as seen by the GPU machine). Longest match wins.
    /// </summary>
    public PathMapping[] SubtitlePathMappings { get; set; } = [];
}

/// <summary>
/// A single Jellyfin-path-prefix → remote-path-prefix rewrite rule.
/// </summary>
public class PathMapping
{
    /// <summary>Gets or sets the Jellyfin-side path prefix (e.g. /media/jav).</summary>
    public string JellyfinPrefix { get; set; } = string.Empty;

    /// <summary>Gets or sets the subtitle-forge-side path prefix (e.g. /Volumes/nas-jav).</summary>
    public string RemotePrefix { get; set; } = string.Empty;
}
