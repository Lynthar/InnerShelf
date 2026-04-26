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

        return await JavBusParser
            .ParseSearchResultsAsync(html, BaseUrl, Name, cancellationToken)
            .ConfigureAwait(false);
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

        return await JavBusParser
            .ParseMoviePageAsync(html, BaseUrl, sourceId, Name, cancellationToken)
            .ConfigureAwait(false);
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

        return await JavBusParser
            .ParseActorSearchResultAsync(html, BaseUrl, name, cancellationToken)
            .ConfigureAwait(false);
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

            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch {Url}", url);
            return null;
        }
    }
}
