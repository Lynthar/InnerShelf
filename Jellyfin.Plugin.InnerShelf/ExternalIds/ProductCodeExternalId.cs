using Jellyfin.Plugin.InnerShelf.Mapping;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.InnerShelf.ExternalIds;

/// <summary>
/// Registers the product code (番号) as an external ID in Jellyfin,
/// making it visible and editable in the metadata editor UI.
/// </summary>
public class ProductCodeExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "InnerShelf";

    /// <inheritdoc />
    public string Key => MetadataMapper.ProviderKey;

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Movie;

    /// <inheritdoc />
    public string? UrlFormatString => null;

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Movie;
}
