using AngleSharp;
using AngleSharp.Dom;
using Jellyfin.Plugin.InnerShelf.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.InnerShelf.Sources.BuiltIn;

/// <summary>
/// Built-in metadata source that scrapes JavBus.
/// </summary>
public class JavBusSource : IMetadataSource
{
    private const string BaseUrl = "https://www.javbus.com";
    private readonly ILogger<JavBusSource> _logger;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="JavBusSource"/> class.
    /// </summary>
    public JavBusSource(ILogger<JavBusSource> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("InnerShelf");
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

        var context = BrowsingContext.New(AngleSharp.Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html), cancellationToken).ConfigureAwait(false);

        var results = new List<SourceSearchResult>();
        var items = document.QuerySelectorAll("#waterfall .item");

        foreach (var item in items)
        {
            var link = item.QuerySelector("a");
            var href = link?.GetAttribute("href");
            var img = item.QuerySelector(".photo-frame img");
            var title = img?.GetAttribute("title");
            var thumbSrc = img?.GetAttribute("src");

            if (href is null)
            {
                continue;
            }

            // Extract code from the URL (last segment)
            var sourceId = href.Split('/').LastOrDefault() ?? string.Empty;

            DateTime? releaseDate = null;
            var dateElements = item.QuerySelectorAll("date");
            if (dateElements.Length >= 2 && DateTime.TryParse(dateElements[1].TextContent, out var parsed))
            {
                releaseDate = parsed;
            }

            results.Add(new SourceSearchResult
            {
                Code = sourceId,
                Title = title,
                ThumbnailUrl = thumbSrc,
                ReleaseDate = releaseDate,
                SourceName = Name,
                SourceId = sourceId
            });
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

        var context = BrowsingContext.New(AngleSharp.Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html), cancellationToken).ConfigureAwait(false);

        var container = document.QuerySelector(".container");
        if (container is null)
        {
            return null;
        }

        // Title and code
        var titleEl = container.QuerySelector("h3");
        var fullTitle = titleEl?.TextContent.Trim();
        var code = sourceId.ToUpperInvariant();

        // Strip code prefix from title if present
        var title = fullTitle;
        if (title is not null && title.StartsWith(code, StringComparison.OrdinalIgnoreCase))
        {
            title = title[code.Length..].TrimStart();
        }

        // Cover image
        var coverImg = container.QuerySelector(".screencap img");
        var coverUrl = coverImg?.GetAttribute("src");

        // Info fields
        var movie = new MovieMetadata
        {
            Code = code,
            OriginalTitle = title,
            Title = title,
            CoverUrl = coverUrl,
            SourceName = Name,
            SourceId = sourceId
        };

        // Parse info section
        var infoSection = container.QuerySelector(".info");
        if (infoSection is not null)
        {
            ParseInfoSection(infoSection, movie);
        }

        // Parse actors
        var actorLinks = container.QuerySelectorAll("#star-list .star-name a");
        var actors = new List<ActorInfo>();
        foreach (var actorLink in actorLinks)
        {
            var actorName = actorLink.TextContent.Trim();
            if (!string.IsNullOrEmpty(actorName))
            {
                // Try to find actor photo from the adjacent img
                var actorBox = actorLink.Closest(".star-box");
                var actorImg = actorBox?.QuerySelector("img");
                var actorImgUrl = actorImg?.GetAttribute("src");

                actors.Add(new ActorInfo
                {
                    Name = actorName,
                    ImageUrl = actorImgUrl
                });
            }
        }

        movie.Actors = actors;

        // Backdrop: full cover is typically the big image
        if (!string.IsNullOrEmpty(coverUrl))
        {
            // JavBus uses /cover/ for front crop and /pics/ for full image
            movie.BackdropUrl = coverUrl.Replace("/cover/", "/pics/", StringComparison.OrdinalIgnoreCase);
        }

        return movie;
    }

    /// <inheritdoc />
    public async Task<ActorMetadata?> GetActorAsync(string name, CancellationToken cancellationToken)
    {
        // Search for the actor page
        var searchResults = await SearchActorAsync(name, cancellationToken).ConfigureAwait(false);
        if (searchResults is null)
        {
            return null;
        }

        return searchResults;
    }

    private async Task<ActorMetadata?> SearchActorAsync(string name, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/searchstar/{Uri.EscapeDataString(name)}";
        var html = await FetchHtmlAsync(url, cancellationToken).ConfigureAwait(false);
        if (html is null)
        {
            return null;
        }

        var context = BrowsingContext.New(AngleSharp.Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html), cancellationToken).ConfigureAwait(false);

        var firstResult = document.QuerySelector("#waterfall .item a.avatar-box");
        if (firstResult is null)
        {
            return null;
        }

        var actorName = firstResult.QuerySelector("span")?.TextContent.Trim();
        var imageUrl = firstResult.QuerySelector("img")?.GetAttribute("src");

        return new ActorMetadata
        {
            Name = actorName ?? name,
            ImageUrl = imageUrl
        };
    }

    private static void ParseInfoSection(IElement infoSection, MovieMetadata movie)
    {
        var paragraphs = infoSection.QuerySelectorAll("p");
        foreach (var p in paragraphs)
        {
            var header = p.QuerySelector(".header");
            if (header is null)
            {
                continue;
            }

            var label = header.TextContent.Trim().TrimEnd(':');
            var value = GetInfoValue(p);

            switch (label)
            {
                case "發行日期":
                case "发行日期":
                    if (DateTime.TryParse(value, out var date))
                    {
                        movie.ReleaseDate = date;
                    }

                    break;
                case "長度":
                case "长度":
                    if (int.TryParse(value.Replace("分鐘", string.Empty).Replace("分钟", string.Empty).Trim(), out var runtime))
                    {
                        movie.RuntimeMinutes = runtime;
                    }

                    break;
                case "導演":
                case "导演":
                    movie.Director = value;
                    break;
                case "製作商":
                case "制作商":
                    movie.Studio = value;
                    break;
                case "發行商":
                case "发行商":
                    movie.Label = value;
                    break;
                case "系列":
                    movie.Series = value;
                    break;
                case "類別":
                case "类别":
                    var genreLinks = p.QuerySelectorAll("a");
                    movie.Genres = genreLinks
                        .Select(a => a.TextContent.Trim())
                        .Where(g => !string.IsNullOrEmpty(g))
                        .ToList();
                    break;
            }
        }
    }

    private static string GetInfoValue(IElement paragraph)
    {
        // The value is typically after the <span class="header"> element
        var link = paragraph.QuerySelector("a");
        if (link is not null)
        {
            return link.TextContent.Trim();
        }

        // Fall back to extracting text after the header span
        var header = paragraph.QuerySelector(".header");
        if (header is null)
        {
            return paragraph.TextContent.Trim();
        }

        var fullText = paragraph.TextContent;
        var headerText = header.TextContent;
        var idx = fullText.IndexOf(headerText, StringComparison.Ordinal);
        if (idx >= 0)
        {
            return fullText[(idx + headerText.Length)..].Trim();
        }

        return fullText.Trim();
    }

    private async Task<string?> FetchHtmlAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept-Language", "zh-TW,zh;q=0.9");

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
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
