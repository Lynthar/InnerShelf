using Jellyfin.Plugin.InnerShelf.Sources.Models;

namespace Jellyfin.Plugin.InnerShelf.Sources;

/// <summary>
/// Abstraction for a JAV metadata source.
/// </summary>
public interface IMetadataSource
{
    /// <summary>Gets the display name of this source.</summary>
    string Name { get; }

    /// <summary>Gets the priority (lower = higher priority).</summary>
    int Priority { get; }

    /// <summary>Gets a value indicating whether this source is currently enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Searches for movies matching the given query (typically a product code).
    /// </summary>
    Task<IReadOnlyList<SourceSearchResult>> SearchAsync(string query, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves full movie metadata by source-specific ID.
    /// </summary>
    Task<MovieMetadata?> GetMovieAsync(string sourceId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves actor metadata by name.
    /// </summary>
    Task<ActorMetadata?> GetActorAsync(string name, CancellationToken cancellationToken);
}
