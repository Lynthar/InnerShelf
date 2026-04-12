using Jellyfin.Plugin.InnerShelf.Mapping;
using Jellyfin.Plugin.InnerShelf.Naming;
using Jellyfin.Plugin.InnerShelf.Sources;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.InnerShelf.Providers;

/// <summary>
/// Remote metadata provider for movies (JAV content).
/// </summary>
public class MovieMetadataProvider : IRemoteMetadataProvider<Movie, MovieInfo>, IHasOrder
{
    private readonly ILogger<MovieMetadataProvider> _logger;
    private readonly MetadataSourceManager _sourceManager;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieMetadataProvider"/> class.
    /// </summary>
    public MovieMetadataProvider(ILogger<MovieMetadataProvider> logger, MetadataSourceManager sourceManager, IHttpClientFactory httpClientFactory)
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
    public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
    {
        // Try existing provider ID first
        if (info.ProviderIds.TryGetValue(MetadataMapper.ProviderKey, out var existingCode) && !string.IsNullOrEmpty(existingCode))
        {
            _logger.LogDebug("Looking up metadata by existing code: {Code}", existingCode);
            var movie = await _sourceManager.GetMovieByCodeAsync(existingCode, cancellationToken).ConfigureAwait(false);
            if (movie is not null)
            {
                return MetadataMapper.ToMovieResult(movie);
            }
        }

        // Parse product code from the item name/path
        var code = ProductCodeParser.Parse(info.Name) ?? ProductCodeParser.Parse(info.Path);
        if (code is null)
        {
            _logger.LogDebug("No product code found in name '{Name}' or path '{Path}'", info.Name, info.Path);
            return new MetadataResult<Movie>();
        }

        _logger.LogDebug("Parsed product code: {Code} (category: {Category})", code.Normalized, code.Category);

        var metadata = await _sourceManager.GetMovieByCodeAsync(code.Normalized, cancellationToken).ConfigureAwait(false);
        if (metadata is null)
        {
            return new MetadataResult<Movie>();
        }

        return MetadataMapper.ToMovieResult(metadata);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
    {
        var query = searchInfo.Name;

        // Try to parse a product code from the search query
        var code = ProductCodeParser.Parse(query);
        if (code is not null)
        {
            query = code.Normalized;
        }

        var results = await _sourceManager.SearchAsync(query, cancellationToken).ConfigureAwait(false);
        return results.Select(MetadataMapper.ToSearchResult);
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _httpClientFactory.CreateClient("InnerShelf").GetAsync(new Uri(url), cancellationToken);
    }
}
