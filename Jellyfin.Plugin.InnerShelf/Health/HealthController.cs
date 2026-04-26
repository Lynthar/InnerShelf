using System.Net.Mime;
using Jellyfin.Plugin.InnerShelf.Sources;
using Jellyfin.Plugin.InnerShelf.Subtitles;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.InnerShelf.Health;

/// <summary>
/// Plugin health endpoint. Lets the configuration UI verify that the bits
/// the user just configured (sources, subtitle-forge) are actually working,
/// without requiring them to wait for a library scan to fail.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("InnerShelf/Health")]
[Produces(MediaTypeNames.Application.Json)]
public class HealthController : ControllerBase
{
    // 5s ceiling for the subtitle-forge probe. Long enough for a slow GPU host
    // on the LAN, short enough that the dashboard "Test connection" button
    // stays responsive when the host is unplugged.
    private static readonly TimeSpan SubtitleForgeProbeTimeout = TimeSpan.FromSeconds(5);

    private readonly IEnumerable<IMetadataSource> _sources;
    private readonly SubtitleForgeClient _subtitleForge;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthController"/> class.
    /// </summary>
    public HealthController(IEnumerable<IMetadataSource> sources, SubtitleForgeClient subtitleForge)
    {
        _sources = sources;
        _subtitleForge = subtitleForge;
    }

    /// <summary>
    /// Returns a snapshot of plugin health: version, source enable states,
    /// and reachability of the configured subtitle-forge server.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<HealthResponse> Get(CancellationToken cancellationToken)
    {
        var sources = _sources
            .OrderBy(s => s.Priority)
            .Select(s => new SourceHealth
            {
                Name = s.Name,
                Enabled = s.IsEnabled,
                Priority = s.Priority,
            })
            .ToList();

        var subtitleForge = new SubtitleForgeHealth
        {
            Configured = SubtitleForgeClient.IsConfigured,
            Url = Plugin.Instance?.Configuration.SubtitleForgeServerUrl ?? string.Empty,
            Reachable = null,
        };

        if (subtitleForge.Configured)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(SubtitleForgeProbeTimeout);
            try
            {
                subtitleForge.Reachable = await _subtitleForge
                    .PingAsync(cts.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Local timeout fired — caller's CT is fine, so report unreachable
                // rather than letting the cancellation bubble up as a 5xx.
                subtitleForge.Reachable = false;
            }
        }

        return new HealthResponse
        {
            PluginVersion = Plugin.Instance?.Version?.ToString() ?? "unknown",
            Sources = sources,
            SubtitleForge = subtitleForge,
        };
    }
}

/// <summary>Plugin health snapshot.</summary>
public class HealthResponse
{
    /// <summary>Gets or sets the plugin assembly version.</summary>
    public string PluginVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets the per-source health, ordered by priority (lower wins).</summary>
    public IReadOnlyList<SourceHealth> Sources { get; set; } = [];

    /// <summary>Gets or sets the subtitle-forge health.</summary>
    public SubtitleForgeHealth SubtitleForge { get; set; } = new();
}

/// <summary>Per-metadata-source health.</summary>
public class SourceHealth
{
    /// <summary>Gets or sets the source name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the source is currently enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the dispatch priority (lower wins).</summary>
    public int Priority { get; set; }
}

/// <summary>Subtitle-forge connectivity health.</summary>
public class SubtitleForgeHealth
{
    /// <summary>Gets or sets a value indicating whether a server URL is configured.</summary>
    public bool Configured { get; set; }

    /// <summary>Gets or sets the configured server URL (empty when not configured).</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets reachability — null when not configured, true when the
    /// server replied with any HTTP status, false on connection failure or timeout.
    /// </summary>
    public bool? Reachable { get; set; }
}
