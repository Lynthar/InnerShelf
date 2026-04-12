using Jellyfin.Plugin.InnerShelf.Mapping;
using Jellyfin.Plugin.InnerShelf.Sources;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.InnerShelf.Providers;

/// <summary>
/// Remote metadata provider for persons (actors/actresses).
/// </summary>
public class PersonMetadataProvider : IRemoteMetadataProvider<Person, PersonLookupInfo>, IHasOrder
{
    private readonly ILogger<PersonMetadataProvider> _logger;
    private readonly MetadataSourceManager _sourceManager;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonMetadataProvider"/> class.
    /// </summary>
    public PersonMetadataProvider(ILogger<PersonMetadataProvider> logger, MetadataSourceManager sourceManager, IHttpClientFactory httpClientFactory)
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
    public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(info.Name))
        {
            return new MetadataResult<Person>();
        }

        var actor = await _sourceManager.GetActorAsync(info.Name, cancellationToken).ConfigureAwait(false);
        if (actor is null)
        {
            return new MetadataResult<Person>();
        }

        return MetadataMapper.ToPersonResult(actor);
    }

    /// <inheritdoc />
    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
    {
        // Person search is not supported; metadata is fetched by exact name match.
        return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _httpClientFactory.CreateClient("InnerShelf").GetAsync(new Uri(url), cancellationToken);
    }
}
