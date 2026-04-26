using Jellyfin.Plugin.InnerShelf.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.InnerShelf.Sources;

/// <summary>
/// Orchestrates queries across metadata sources by priority.
/// Returns the first successful result (no merging).
/// </summary>
public class MetadataSourceManager
{
    private readonly ILogger<MetadataSourceManager> _logger;
    private readonly IEnumerable<IMetadataSource> _sources;
    private readonly MovieMetadataCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataSourceManager"/> class.
    /// </summary>
    public MetadataSourceManager(
        ILogger<MetadataSourceManager> logger,
        IEnumerable<IMetadataSource> sources,
        MovieMetadataCache cache)
    {
        _logger = logger;
        _sources = sources;
        _cache = cache;
    }

    /// <summary>
    /// Searches all enabled sources by priority and returns the first non-empty result.
    /// </summary>
    public async Task<IReadOnlyList<SourceSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        foreach (var source in GetEnabledSources())
        {
            try
            {
                var results = await source.SearchAsync(query, cancellationToken).ConfigureAwait(false);
                if (results.Count > 0)
                {
                    _logger.LogDebug("Search for '{Query}' returned {Count} results from {Source}", query, results.Count, source.Name);
                    return results;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Search failed on source {Source} for query '{Query}'", source.Name, query);
            }
        }

        return [];
    }

    /// <summary>
    /// Retrieves movie metadata from a specific source.
    /// </summary>
    public async Task<MovieMetadata?> GetMovieAsync(string sourceName, string sourceId, CancellationToken cancellationToken)
    {
        var source = _sources.FirstOrDefault(s => s.Name == sourceName && s.IsEnabled);
        if (source is null)
        {
            _logger.LogWarning("Source '{Source}' not found or disabled", sourceName);
            return null;
        }

        try
        {
            return await source.GetMovieAsync(sourceId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "GetMovie failed on source {Source} for ID '{Id}'", sourceName, sourceId);
            return null;
        }
    }

    /// <summary>
    /// Retrieves movie metadata by querying every enabled source in priority
    /// order. The highest-priority hit becomes the baseline; later sources
    /// fill in fields that the baseline left empty (see
    /// <see cref="MovieMetadataMerger"/>). The walk stops early once the
    /// baseline is fully populated. Results (including misses) are cached
    /// for a short TTL so the metadata provider and image provider don't
    /// both hit the network during a single library scan.
    /// </summary>
    public async Task<MovieMetadata?> GetMovieByCodeAsync(string code, CancellationToken cancellationToken)
    {
        if (_cache.TryGet(code, out var cached))
        {
            _logger.LogDebug("Cache hit for code '{Code}'", code);
            return cached;
        }

        MovieMetadata? merged = null;

        foreach (var source in GetEnabledSources())
        {
            try
            {
                var results = await source.SearchAsync(code, cancellationToken).ConfigureAwait(false);
                if (results.Count == 0)
                {
                    continue;
                }

                var movie = await source.GetMovieAsync(results[0].SourceId, cancellationToken).ConfigureAwait(false);
                if (movie is null)
                {
                    continue;
                }

                if (merged is null)
                {
                    _logger.LogDebug("Baseline metadata for '{Code}' from {Source}", code, source.Name);
                    merged = movie;
                }
                else
                {
                    _logger.LogDebug("Filling missing fields for '{Code}' from {Source}", code, source.Name);
                    merged = MovieMetadataMerger.Merge(merged, movie);
                }

                if (MovieMetadataMerger.HasAllPrimaryFields(merged))
                {
                    break;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "GetMovieByCode failed on source {Source} for '{Code}'", source.Name, code);
            }
        }

        // Cache the result (including null) so repeated lookups for the same
        // code don't re-walk every source.
        _cache.Set(code, merged);
        return merged;
    }

    /// <summary>
    /// Retrieves actor metadata from all sources, returning the first match.
    /// </summary>
    public async Task<ActorMetadata?> GetActorAsync(string name, CancellationToken cancellationToken)
    {
        foreach (var source in GetEnabledSources())
        {
            try
            {
                var actor = await source.GetActorAsync(name, cancellationToken).ConfigureAwait(false);
                if (actor is not null)
                {
                    return actor;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "GetActor failed on source {Source} for '{Name}'", source.Name, name);
            }
        }

        return null;
    }

    private IOrderedEnumerable<IMetadataSource> GetEnabledSources()
    {
        return _sources.Where(s => s.IsEnabled).OrderBy(s => s.Priority);
    }
}
