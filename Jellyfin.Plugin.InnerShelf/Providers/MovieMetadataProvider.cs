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
        // Always parse the filename for version markers (chinese sub, uncensored, HD remaster, etc),
        // even if a provider ID already exists. Version flags are filename-derived metadata and need
        // re-parsing on every scan so that tags stay accurate if the file is renamed.
        var parsedCode = ProductCodeParser.Parse(info.Name) ?? ProductCodeParser.Parse(info.Path);

        // Determine which code to look up on the metadata source: prefer an existing provider ID
        // (user may have manually corrected it via the UI), fall back to what we parsed.
        string? lookupCode;
        if (info.ProviderIds.TryGetValue(MetadataMapper.ProviderKey, out var existingCode) && !string.IsNullOrEmpty(existingCode))
        {
            lookupCode = existingCode;
            _logger.LogDebug("Using existing provider ID for lookup: {Code}", lookupCode);
        }
        else if (parsedCode is not null)
        {
            lookupCode = parsedCode.Normalized;
            _logger.LogDebug("Parsed product code: {Code} (category: {Category}, versions: {Versions})", parsedCode.Normalized, parsedCode.Category, parsedCode.Versions);
        }
        else
        {
            _logger.LogDebug("No product code found in name '{Name}' or path '{Path}'", info.Name, info.Path);
            return new MetadataResult<Movie>();
        }

        var metadata = await _sourceManager.GetMovieByCodeAsync(lookupCode, cancellationToken).ConfigureAwait(false);
        if (metadata is null)
        {
            _logger.LogDebug("No metadata found for {Code} from any source", lookupCode);
            return new MetadataResult<Movie>();
        }

        return MetadataMapper.ToMovieResult(metadata, parsedCode);
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
