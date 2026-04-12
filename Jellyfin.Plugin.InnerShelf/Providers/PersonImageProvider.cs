using Jellyfin.Plugin.InnerShelf.Sources;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.InnerShelf.Providers;

/// <summary>
/// Remote image provider for actor photos.
/// </summary>
public class PersonImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly ILogger<PersonImageProvider> _logger;
    private readonly MetadataSourceManager _sourceManager;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonImageProvider"/> class.
    /// </summary>
    public PersonImageProvider(ILogger<PersonImageProvider> logger, MetadataSourceManager sourceManager, IHttpClientFactory httpClientFactory)
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
    public bool Supports(BaseItem item) => item is Person;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return [ImageType.Primary];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var actor = await _sourceManager.GetActorAsync(item.Name, cancellationToken).ConfigureAwait(false);
        if (actor is null || string.IsNullOrEmpty(actor.ImageUrl))
        {
            return [];
        }

        return
        [
            new RemoteImageInfo
            {
                ProviderName = Name,
                Url = actor.ImageUrl,
                Type = ImageType.Primary
            }
        ];
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _httpClientFactory.CreateClient("InnerShelf").GetAsync(new Uri(url), cancellationToken);
    }
}
