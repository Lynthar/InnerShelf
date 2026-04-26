using Jellyfin.Plugin.InnerShelf.Sources.Models;

namespace Jellyfin.Plugin.InnerShelf.Sources;

/// <summary>
/// Combines a primary metadata record (from the highest-priority source that
/// returned data) with later-source records. Strategy:
///
/// <list type="bullet">
///   <item>Scalar fields use <em>fill-only-missing</em> — primary's non-empty
///         value always wins; secondary contributes only when primary is null/empty.</item>
///   <item><see cref="MovieMetadata.Genres"/> are union-merged (deduped, primary
///         order preserved). Different sources frequently translate genre names
///         differently and their lists are largely complementary, so first-wins
///         loses real data.</item>
///   <item><see cref="MovieMetadata.Actors"/> are unioned by <see cref="ActorInfo.Name"/>.
///         Primary's photo wins on conflict; if primary lacks a photo, a
///         secondary photo for the same actor fills it in.</item>
///   <item><c>SourceName</c>/<c>SourceId</c> stay pinned to the baseline source
///         for traceability.</item>
/// </list>
/// </summary>
public static class MovieMetadataMerger
{
    /// <summary>
    /// Returns a new <see cref="MovieMetadata"/> with secondary's missing fields
    /// folded into primary. Inputs are not mutated.
    /// </summary>
    public static MovieMetadata Merge(MovieMetadata primary, MovieMetadata secondary)
    {
        return new MovieMetadata
        {
            Code = primary.Code,
            OriginalTitle = FirstNonEmpty(primary.OriginalTitle, secondary.OriginalTitle),
            Title = FirstNonEmpty(primary.Title, secondary.Title),
            Overview = FirstNonEmpty(primary.Overview, secondary.Overview),
            ReleaseDate = primary.ReleaseDate ?? secondary.ReleaseDate,
            RuntimeMinutes = primary.RuntimeMinutes ?? secondary.RuntimeMinutes,
            Director = FirstNonEmpty(primary.Director, secondary.Director),
            Studio = FirstNonEmpty(primary.Studio, secondary.Studio),
            Label = FirstNonEmpty(primary.Label, secondary.Label),
            Series = FirstNonEmpty(primary.Series, secondary.Series),
            CoverUrl = FirstNonEmpty(primary.CoverUrl, secondary.CoverUrl),
            BackdropUrl = FirstNonEmpty(primary.BackdropUrl, secondary.BackdropUrl),
            Genres = UnionPreservingOrder(primary.Genres, secondary.Genres),
            Actors = MergeActors(primary.Actors, secondary.Actors),
            SourceName = primary.SourceName,
            SourceId = primary.SourceId,
        };
    }

    /// <summary>
    /// Returns true when the "main fields" are all populated. The orchestrator
    /// uses this as an early-stop signal to avoid querying remaining sources
    /// once nothing further can be filled.
    /// </summary>
    public static bool HasAllPrimaryFields(MovieMetadata m) =>
        !string.IsNullOrEmpty(m.Title)
        && !string.IsNullOrEmpty(m.CoverUrl)
        && m.ReleaseDate.HasValue
        && m.Genres.Count > 0
        && m.Actors.Count > 0;

    private static string? FirstNonEmpty(string? a, string? b) =>
        string.IsNullOrEmpty(a) ? b : a;

    private static IReadOnlyList<string> UnionPreservingOrder(
        IReadOnlyList<string> primary,
        IReadOnlyList<string> secondary)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>(primary.Count + secondary.Count);

        foreach (var s in primary)
        {
            if (!string.IsNullOrEmpty(s) && seen.Add(s))
            {
                result.Add(s);
            }
        }

        foreach (var s in secondary)
        {
            if (!string.IsNullOrEmpty(s) && seen.Add(s))
            {
                result.Add(s);
            }
        }

        return result;
    }

    private static IReadOnlyList<ActorInfo> MergeActors(
        IReadOnlyList<ActorInfo> primary,
        IReadOnlyList<ActorInfo> secondary)
    {
        var byName = new Dictionary<string, ActorInfo>(StringComparer.OrdinalIgnoreCase);
        var order = new List<string>();

        foreach (var a in primary)
        {
            if (string.IsNullOrEmpty(a.Name))
            {
                continue;
            }

            if (!byName.ContainsKey(a.Name))
            {
                byName[a.Name] = new ActorInfo { Name = a.Name, ImageUrl = a.ImageUrl };
                order.Add(a.Name);
            }
        }

        foreach (var a in secondary)
        {
            if (string.IsNullOrEmpty(a.Name))
            {
                continue;
            }

            if (byName.TryGetValue(a.Name, out var existing))
            {
                if (string.IsNullOrEmpty(existing.ImageUrl) && !string.IsNullOrEmpty(a.ImageUrl))
                {
                    existing.ImageUrl = a.ImageUrl;
                }
            }
            else
            {
                byName[a.Name] = new ActorInfo { Name = a.Name, ImageUrl = a.ImageUrl };
                order.Add(a.Name);
            }
        }

        return order.Select(n => byName[n]).ToList();
    }
}
