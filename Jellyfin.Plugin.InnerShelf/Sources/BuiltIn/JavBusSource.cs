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
            var thumbSrc = MakeAbsolute(img?.GetAttribute("src"));

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
        var coverUrl = MakeAbsolute(coverImg?.GetAttribute("src"));

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

        // Parse info section (date, runtime, director, studio, label, series)
        var infoSection = container.QuerySelector(".info");
        if (infoSection is not null)
        {
            ParseInfoSection(infoSection, movie);
        }

        // Parse genres directly — the genres live inside `.info` but their <p> has no
        // `.header` span (the header is on the PREVIOUS sibling p with `class="header"`).
        // Use a direct selector that filters out the "submit" button span.
        if (infoSection is not null)
        {
            var genreLinks = infoSection.QuerySelectorAll(".genre > label > a");
            movie.Genres = genreLinks
                .Select(a => a.TextContent.Trim())
                .Where(g => !string.IsNullOrEmpty(g))
                .ToList();
        }

        // Parse actors from #avatar-waterfall .avatar-box — each box has <span> name + <img> photo.
        // Note: the older #star-list selector does not exist on current JavBus pages.
        var avatarBoxes = container.QuerySelectorAll(".avatar-box");
        var actors = new List<ActorInfo>();
        foreach (var box in avatarBoxes)
        {
            var nameEl = box.QuerySelector("span");
            var actorName = nameEl?.TextContent.Trim();
            if (string.IsNullOrEmpty(actorName))
            {
                continue;
            }

            var imgEl = box.QuerySelector("img");
            var actorImgUrl = MakeAbsolute(imgEl?.GetAttribute("src"));

            actors.Add(new ActorInfo
            {
                Name = actorName,
                ImageUrl = actorImgUrl
            });
        }

        movie.Actors = actors;

        // JavBus has only a single cover image per movie (no separate backdrop). Reuse the
        // same URL for both Primary and Backdrop so Jellyfin has something to display as the
        // detail page background — the previous `/cover/ -> /pics/` URL rewrite was incorrect
        // (the URL is already `/pics/cover/xxx_b.jpg` from JavBus).
        movie.BackdropUrl = coverUrl;

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
        var imageUrl = MakeAbsolute(firstResult.QuerySelector("img")?.GetAttribute("src"));

        return new ActorMetadata
        {
            Name = actorName ?? name,
            ImageUrl = imageUrl
        };
    }

    /// <summary>
    /// Resolves a possibly-relative URL to an absolute URL against <see cref="BaseUrl"/>.
    /// Handles: absolute http(s), protocol-relative (//cdn/...), root-relative (/path),
    /// and bare paths. Returns null for null/empty input.
    /// </summary>
    private static string? MakeAbsolute(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (url.StartsWith("//"))
        {
            return "https:" + url;
        }

        if (url.StartsWith('/'))
        {
            return BaseUrl.TrimEnd('/') + url;
        }

        return BaseUrl.TrimEnd('/') + "/" + url;
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
