using Jellyfin.Plugin.InnerShelf.Configuration;

namespace Jellyfin.Plugin.InnerShelf.Subtitles;

/// <summary>
/// Rewrites a Jellyfin-side video path into the path the subtitle-forge server
/// sees on its own filesystem.
///
/// Multiple mapping rules can be configured; the longest matching prefix wins
/// so that nested mounts (e.g. /media/jav/sub vs /media/jav) resolve unambiguously.
/// If no rule matches, the input path is returned unchanged — useful when both
/// hosts mount the storage at the same path.
/// </summary>
public static class PathMapper
{
    /// <summary>
    /// Applies the configured mappings to <paramref name="jellyfinPath"/> and
    /// returns the path as the subtitle-forge server would see it.
    /// </summary>
    public static string Map(string jellyfinPath, IReadOnlyList<PathMapping> mappings)
    {
        if (string.IsNullOrEmpty(jellyfinPath) || mappings.Count == 0)
        {
            return jellyfinPath;
        }

        PathMapping? best = null;
        var bestLen = -1;

        foreach (var m in mappings)
        {
            var prefix = NormalizePrefix(m.JellyfinPrefix);
            if (prefix.Length == 0)
            {
                continue;
            }

            // Match either an exact prefix followed by '/' or the whole path
            // equal to the prefix. This avoids `/media/jav` matching `/media/javier`.
            if (jellyfinPath.Length >= prefix.Length
                && jellyfinPath.StartsWith(prefix, StringComparison.Ordinal)
                && (jellyfinPath.Length == prefix.Length || jellyfinPath[prefix.Length] == '/'))
            {
                if (prefix.Length > bestLen)
                {
                    best = m;
                    bestLen = prefix.Length;
                }
            }
        }

        if (best is null)
        {
            return jellyfinPath;
        }

        var bestPrefix = NormalizePrefix(best.JellyfinPrefix);
        var remote = NormalizePrefix(best.RemotePrefix);
        var tail = jellyfinPath[bestPrefix.Length..];
        return remote + tail;
    }

    /// <summary>Strips trailing slashes so prefix length comparisons are stable.</summary>
    private static string NormalizePrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return string.Empty;
        }

        return prefix.TrimEnd('/');
    }
}
