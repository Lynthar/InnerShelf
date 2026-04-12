namespace Jellyfin.Plugin.InnerShelf.Sources.Models;

/// <summary>
/// Source-agnostic movie metadata model.
/// </summary>
public class MovieMetadata
{
    /// <summary>Gets or sets the product code (番号).</summary>
    public required string Code { get; set; }

    /// <summary>Gets or sets the original (Japanese) title.</summary>
    public string? OriginalTitle { get; set; }

    /// <summary>Gets or sets the translated title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the plot synopsis.</summary>
    public string? Overview { get; set; }

    /// <summary>Gets or sets the release date.</summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>Gets or sets the runtime in minutes.</summary>
    public int? RuntimeMinutes { get; set; }

    /// <summary>Gets or sets the director name.</summary>
    public string? Director { get; set; }

    /// <summary>Gets or sets the studio/maker name.</summary>
    public string? Studio { get; set; }

    /// <summary>Gets or sets the label name.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the series name.</summary>
    public string? Series { get; set; }

    /// <summary>Gets or sets the genre tags.</summary>
    public IReadOnlyList<string> Genres { get; set; } = [];

    /// <summary>Gets or sets the actor information.</summary>
    public IReadOnlyList<ActorInfo> Actors { get; set; } = [];

    /// <summary>Gets or sets the front cover image URL.</summary>
    public string? CoverUrl { get; set; }

    /// <summary>Gets or sets the full (back) cover image URL.</summary>
    public string? BackdropUrl { get; set; }

    /// <summary>Gets or sets the source provider name.</summary>
    public string? SourceName { get; set; }

    /// <summary>Gets or sets the source-specific ID for retrieval.</summary>
    public string? SourceId { get; set; }
}

/// <summary>
/// Actor information within movie metadata.
/// </summary>
public class ActorInfo
{
    /// <summary>Gets or sets the actor name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the actor photo URL.</summary>
    public string? ImageUrl { get; set; }
}
