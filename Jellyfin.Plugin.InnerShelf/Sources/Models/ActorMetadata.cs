namespace Jellyfin.Plugin.InnerShelf.Sources.Models;

/// <summary>
/// Source-agnostic actor/actress metadata model.
/// </summary>
public class ActorMetadata
{
    /// <summary>Gets or sets the primary name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets alternative names/aliases.</summary>
    public IReadOnlyList<string> Aliases { get; set; } = [];

    /// <summary>Gets or sets the profile image URL.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the date of birth.</summary>
    public DateTime? BirthDate { get; set; }

    /// <summary>Gets or sets the height in cm.</summary>
    public int? HeightCm { get; set; }
}
