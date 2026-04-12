using Jellyfin.Plugin.InnerShelf.Mapping;
using Jellyfin.Plugin.InnerShelf.Sources;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.InnerShelf.Providers;

/// <summary>
/// Remote image provider for movie covers.
/// </summary>
public class MovieImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly ILogger<MovieImageProvider> _logger;
    private readonly MetadataSourceManager _sourceManager;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieImageProvider"/> class.
    /// </summary>
    public MovieImageProvider(ILogger<MovieImageProvider> logger, MetadataSourceManager sourceManager, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _sourceManager = sourceManager;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public string Name => "InnerShelf";

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is Movie;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return [ImageType.Primary, ImageType.Backdrop];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (!item.ProviderIds.TryGetValue(MetadataMapper.ProviderKey, out var code) || string.IsNullOrEmpty(code))
        {
            return [];
        }

        var movie = await _sourceManager.GetMovieByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (movie is null)
        {
            return [];
        }

        var images = new List<RemoteImageInfo>();

        if (!string.IsNullOrEmpty(movie.CoverUrl))
        {
            images.Add(new RemoteImageInfo
            {
                ProviderName = Name,
                Url = movie.CoverUrl,
                Type = ImageType.Primary
            });
        }

        if (!string.IsNullOrEmpty(movie.BackdropUrl))
        {
            images.Add(new RemoteImageInfo
            {
                ProviderName = Name,
                Url = movie.BackdropUrl,
                Type = ImageType.Backdrop
            });
        }

        return images;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _httpClientFactory.CreateClient("InnerShelf").GetAsync(new Uri(url), cancellationToken);
    }
}
