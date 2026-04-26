using Jellyfin.Plugin.InnerShelf.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.InnerShelf.Sources.BuiltIn;

/// <summary>
/// Built-in metadata source that scrapes JavBus. The HTML parsing lives in
/// <see cref="JavBusParser"/> so it can be regression-tested with fixtures
/// without hitting the live site.
/// </summary>
public class JavBusSource : IMetadataSource
{
    private const string BaseUrl = "https://www.javbus.com";

    private readonly ILogger<JavBusSource> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="JavBusSource"/> class.
    /// </summary>
    public JavBusSource(ILogger<JavBusSource> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public string Name => "JavBus";

    /// <inheritdoc />
    public int Priority => 10;

    /// <inheritdoc />
    public bool IsEnabled => Plugin.Instance?.Configuration.EnableJavBus ?? true;

    /// <inheritdoc />
    public async Task<IReadOnlyList<SourceSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/search/{Uri.EscapeDataString(query)}";
        var html = await FetchHtmlAsync(url, cancellationToken).ConfigureAwait(false);
        if (html is null)
        {
            return [];
        }

        var results = await JavBusParser
            .ParseSearchResultsAsync(html, BaseUrl, Name, cancellationToken)
            .ConfigureAwait(false);

        // Search returning 0 results is normal (unknown code, typo, very new releases),
        // so this stays at Debug rather than Warning.
        if (results.Count == 0)
        {
            _logger.LogDebug("JavBus search for '{Query}' returned 0 items", query);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<MovieMetadata?> GetMovieAsync(string sourceId, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/{Uri.EscapeDataString(sourceId)}";
        var html = await FetchHtmlAsync(url, cancellationToken).ConfigureAwait(false);
        if (html is null)
        {
            return null;
        }

        var movie = await JavBusParser
            .ParseMoviePageAsync(html, BaseUrl, sourceId, Name, cancellationToken)
            .ConfigureAwait(false);

        // We fetched a real (non-CF, non-error) HTML body but the parser couldn't even
        // find `.container`. Either the code 404'd into a soft "not found" page (rare —
        // JavBus normally returns HTTP 404), or JavBus changed their DOM. Surface as
        // Warning so silent breakage is visible in logs.
        if (movie is null)
        {
            _logger.LogWarning(
                "JavBus returned a page for '{Code}' but no .container element was found — possible DOM change. Body length: {Length}",
                sourceId,
                html.Length);
        }

        return movie;
    }

    /// <inheritdoc />
    public async Task<ActorMetadata?> GetActorAsync(string name, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/searchstar/{Uri.EscapeDataString(name)}";
        var html = await FetchHtmlAsync(url, cancellationToken).ConfigureAwait(false);
        if (html is null)
        {
            return null;
        }

        var actor = await JavBusParser
            .ParseActorSearchResultAsync(html, BaseUrl, name, cancellationToken)
            .ConfigureAwait(false);

        // Actor not found is a normal outcome; debug only.
        if (actor is null)
        {
            _logger.LogDebug("JavBus actor search for '{Name}' returned no results", name);
        }

        return actor;
    }

    private async Task<string?> FetchHtmlAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient(PluginServiceRegistrator.HttpClientName);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept-Language", "zh-CN,zh-TW;q=0.9,zh;q=0.8,ja;q=0.5");
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("HTTP {Status} from {Url}", response.StatusCode, url);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            // Cloudflare frequently serves a 200 OK with a challenge page body for
            // requests it doesn't like. Without this check the scraper would parse
            // the challenge page, find none of the expected JavBus selectors, and
            // silently return empty results — looking exactly like "no match".
            if (CloudflareDetector.IsCloudflareInterstitial(body))
            {
                _logger.LogWarning(
                    "JavBus returned a Cloudflare interstitial for {Url} — request blocked. Configure HttpProxy in plugin settings to bypass.",
                    url);
                return null;
            }

            return body;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch {Url}", url);
            return null;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient surfaces its own timeout as TaskCanceledException with the caller's CT
            // not cancelled. Distinguish from genuine user cancellation, which we re-throw.
            _logger.LogWarning(ex, "Timed out fetching {Url}", url);
            return null;
        }
    }
}
