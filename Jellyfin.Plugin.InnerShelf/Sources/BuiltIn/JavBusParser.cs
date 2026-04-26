using AngleSharp;
using AngleSharp.Dom;
using Jellyfin.Plugin.InnerShelf.Sources.Models;

namespace Jellyfin.Plugin.InnerShelf.Sources.BuiltIn;

/// <summary>
/// Pure HTML parsing for JavBus pages, separated from network I/O so the
/// selector-against-DOM logic can be regression-tested with fixture HTML
/// without hitting the live site.
/// </summary>
public static class JavBusParser
{
    /// <summary>
    /// Parses a JavBus search results page (<c>/search/&lt;query&gt;</c>) into
    /// per-item search results.
    /// </summary>
    public static async Task<IReadOnlyList<SourceSearchResult>> ParseSearchResultsAsync(
        string html,
        string baseUrl,
        string sourceName,
        CancellationToken cancellationToken)
    {
        var document = await OpenAsync(html, cancellationToken).ConfigureAwait(false);
        var results = new List<SourceSearchResult>();

        foreach (var item in document.QuerySelectorAll("#waterfall .item"))
        {
            var link = item.QuerySelector("a");
            var href = link?.GetAttribute("href");
            if (string.IsNullOrEmpty(href))
            {
                continue;
            }

            // Last URL segment is the product code. TrimEnd('/') so trailing
            // slashes don't yield an empty source id.
            var sourceId = href.TrimEnd('/').Split('/').LastOrDefault() ?? string.Empty;
            if (string.IsNullOrEmpty(sourceId))
            {
                continue;
            }

            var img = item.QuerySelector(".photo-frame img");
            var title = img?.GetAttribute("title");
            var thumbSrc = MakeAbsolute(img?.GetAttribute("src"), baseUrl);

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
                SourceName = sourceName,
                SourceId = sourceId,
            });
        }

        return results;
    }

    /// <summary>
    /// Parses a JavBus movie detail page into a <see cref="MovieMetadata"/>.
    /// Returns null when the page lacks a <c>.container</c> root, which is the
    /// canonical "not a movie page" signal (404 redirects, Cloudflare, etc.).
    /// </summary>
    public static async Task<MovieMetadata?> ParseMoviePageAsync(
        string html,
        string baseUrl,
        string sourceId,
        string sourceName,
        CancellationToken cancellationToken)
    {
        var document = await OpenAsync(html, cancellationToken).ConfigureAwait(false);

        var container = document.QuerySelector(".container");
        if (container is null)
        {
            return null;
        }

        var titleEl = container.QuerySelector("h3");
        var fullTitle = titleEl?.TextContent.Trim();
        var code = sourceId.ToUpperInvariant();

        // Strip the leading product code from the displayed title — JavBus
        // formats h3 as "<CODE> <TITLE>", and we don't want the code echoed
        // twice once MetadataMapper renders the template "{code} {title}".
        var title = fullTitle;
        if (title is not null && title.StartsWith(code, StringComparison.OrdinalIgnoreCase))
        {
            title = title[code.Length..].TrimStart();
        }

        var coverImg = container.QuerySelector(".screencap img");
        var coverUrl = MakeAbsolute(coverImg?.GetAttribute("src"), baseUrl);

        var movie = new MovieMetadata
        {
            Code = code,
            OriginalTitle = title,
            Title = title,
            CoverUrl = coverUrl,
            SourceName = sourceName,
            SourceId = sourceId,
        };

        var infoSection = container.QuerySelector(".info");
        if (infoSection is not null)
        {
            ParseInfoSection(infoSection, movie);

            // Genres live in the same `.info` block but their header is on a
            // sibling p with class="header"; the values are .genre > label > a.
            var genreLinks = infoSection.QuerySelectorAll(".genre > label > a");
            movie.Genres = genreLinks
                .Select(a => a.TextContent.Trim())
                .Where(g => !string.IsNullOrEmpty(g))
                .ToList();
        }

        // Actors come from .avatar-box: each box has <span> name + <img> photo.
        // The older #star-list selector no longer exists on current JavBus pages.
        var actors = new List<ActorInfo>();
        foreach (var box in container.QuerySelectorAll(".avatar-box"))
        {
            var actorName = box.QuerySelector("span")?.TextContent.Trim();
            if (string.IsNullOrEmpty(actorName))
            {
                continue;
            }

            var actorImgUrl = MakeAbsolute(box.QuerySelector("img")?.GetAttribute("src"), baseUrl);
            actors.Add(new ActorInfo { Name = actorName, ImageUrl = actorImgUrl });
        }

        movie.Actors = actors;

        // JavBus has only one cover image per movie (no separate backdrop).
        // Reuse the same URL so Jellyfin still has something to render as the
        // detail-page background.
        movie.BackdropUrl = coverUrl;

        return movie;
    }

    /// <summary>
    /// Parses a JavBus actor search page (<c>/searchstar/&lt;name&gt;</c>) and
    /// returns the first match, or null if the page has no results.
    /// </summary>
    public static async Task<ActorMetadata?> ParseActorSearchResultAsync(
        string html,
        string baseUrl,
        string fallbackName,
        CancellationToken cancellationToken)
    {
        var document = await OpenAsync(html, cancellationToken).ConfigureAwait(false);

        var firstResult = document.QuerySelector("#waterfall .item a.avatar-box");
        if (firstResult is null)
        {
            return null;
        }

        var actorName = firstResult.QuerySelector("span")?.TextContent.Trim();
        var imageUrl = MakeAbsolute(firstResult.QuerySelector("img")?.GetAttribute("src"), baseUrl);

        return new ActorMetadata
        {
            Name = string.IsNullOrEmpty(actorName) ? fallbackName : actorName,
            ImageUrl = imageUrl,
        };
    }

    /// <summary>
    /// Resolves a possibly-relative URL to an absolute URL against
    /// <paramref name="baseUrl"/>. Handles absolute http(s), protocol-relative
    /// (<c>//cdn/...</c>), root-relative (<c>/path</c>), and bare paths.
    /// Returns null for null/empty input.
    /// </summary>
    internal static string? MakeAbsolute(string? url, string baseUrl)
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

        if (url.StartsWith("//", StringComparison.Ordinal))
        {
            return "https:" + url;
        }

        var trimmedBase = baseUrl.TrimEnd('/');
        if (url.StartsWith('/'))
        {
            return trimmedBase + url;
        }

        return trimmedBase + "/" + url;
    }

    private static async Task<IDocument> OpenAsync(string html, CancellationToken cancellationToken)
    {
        var context = BrowsingContext.New(AngleSharp.Configuration.Default);
        return await context
            .OpenAsync(req => req.Content(html), cancellationToken)
            .ConfigureAwait(false);
    }

    private static void ParseInfoSection(IElement infoSection, MovieMetadata movie)
    {
        foreach (var p in infoSection.QuerySelectorAll("p"))
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
                    var minutes = value.Replace("分鐘", string.Empty).Replace("分钟", string.Empty).Trim();
                    if (int.TryParse(minutes, out var runtime))
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
        // Most info fields wrap the value in <a> for the source's tag-cloud links.
        var link = paragraph.QuerySelector("a");
        if (link is not null)
        {
            return link.TextContent.Trim();
        }

        // Fields without a link (date, runtime) put the value as bare text after
        // the .header span — strip the header from the paragraph text.
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
}
