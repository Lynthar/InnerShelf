namespace Jellyfin.Plugin.InnerShelf.Sources.Models;

/// <summary>
/// A search result from a metadata source.
/// </summary>
public class SourceSearchResult
{
    /// <summary>Gets or sets the product code.</summary>
    public required string Code { get; set; }

    /// <summary>Gets or sets the title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the cover thumbnail URL.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Gets or sets the release date.</summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>Gets or sets the source provider name.</summary>
    public required string SourceName { get; set; }

    /// <summary>Gets or sets the source-specific ID for retrieval.</summary>
    public required string SourceId { get; set; }
}
