using System.Collections.Concurrent;
using Jellyfin.Plugin.InnerShelf.Sources.Models;

namespace Jellyfin.Plugin.InnerShelf.Sources;

/// <summary>
/// Short-TTL cache for <see cref="MovieMetadata"/> keyed by product code.
///
/// Jellyfin invokes the metadata provider and the image provider as two
/// separate calls (the image provider runs even after a metadata refresh
/// already embedded image URLs in <c>RemoteImages</c>). Without this cache
/// each library scan would hit the upstream source twice per item.
///
/// The cache also stores negative results (null) so repeated lookups for
/// codes that no source can resolve don't re-hammer the network.
///
/// TTL is short on purpose: a longer window would mask configuration
/// changes (enabling a new source, fixing a network proxy) until expiry.
/// 2 minutes is enough to coalesce within-scan duplicate calls without
/// noticeably delaying after-the-fact corrections.
/// </summary>
public class MovieMetadataCache
{
    private readonly TimeSpan _ttl;
    private readonly int _maxEntries;
    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    /// <summary>
    /// Initializes a new instance with default TTL (2 min) and capacity (500).
    /// </summary>
    public MovieMetadataCache() : this(TimeSpan.FromMinutes(2), 500)
    {
    }

    /// <summary>
    /// Initializes a new instance with the given TTL and capacity. Intended
    /// for tests; production code should use the parameterless constructor.
    /// </summary>
    public MovieMetadataCache(TimeSpan ttl, int maxEntries)
    {
        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "TTL must be positive");
        }

        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries), "maxEntries must be positive");
        }

        _ttl = ttl;
        _maxEntries = maxEntries;
    }

    /// <summary>
    /// Attempts to retrieve a cached entry. Returns true on cache hit
    /// (including a cached null result); false on miss or expired entry.
    /// </summary>
    public bool TryGet(string code, out MovieMetadata? value)
    {
        value = null;
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        if (!_entries.TryGetValue(code, out var entry))
        {
            return false;
        }

        if (DateTime.UtcNow >= entry.ExpiresAt)
        {
            _entries.TryRemove(code, out _);
            return false;
        }

        value = entry.Value;
        return true;
    }

    /// <summary>
    /// Stores an entry. Pass null for <paramref name="value"/> to cache a
    /// negative lookup. Silently no-ops on empty key.
    /// </summary>
    public void Set(string code, MovieMetadata? value)
    {
        if (string.IsNullOrEmpty(code))
        {
            return;
        }

        // If we're at capacity, sweep expired entries opportunistically.
        // No background task — the cache is best-effort and if even after
        // sweeping we're still full, we drop the new write rather than
        // evict a live entry by random choice.
        if (_entries.Count >= _maxEntries)
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _entries)
            {
                if (kvp.Value.ExpiresAt <= now)
                {
                    _entries.TryRemove(kvp.Key, out _);
                }
            }

            if (_entries.Count >= _maxEntries)
            {
                return;
            }
        }

        _entries[code] = new Entry(DateTime.UtcNow + _ttl, value);
    }

    /// <summary>Gets the current number of entries (including expired-but-not-yet-swept).</summary>
    public int Count => _entries.Count;

    private sealed record Entry(DateTime ExpiresAt, MovieMetadata? Value);
}
