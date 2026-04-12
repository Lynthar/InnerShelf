using Jellyfin.Plugin.InnerShelf.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.InnerShelf.Sources.MetaTube;

/// <summary>
/// Optional metadata source that delegates to a MetaTube backend server.
/// Only active when a MetaTube server URL is configured.
/// </summary>
public class MetaTubeSource : IMetadataSource
{
    private readonly ILogger<MetaTubeSource> _logger;
    private readonly MetaTubeApiClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetaTubeSource"/> class.
    /// </summary>
    public MetaTubeSource(ILogger<MetaTubeSource> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _client = new MetaTubeApiClient(httpClientFactory.CreateClient("InnerShelf.MetaTube"));
    }

    /// <inheritdoc />
    public string Name => "MetaTube";

    /// <inheritdoc />
    public int Priority => 5; // Higher priority than built-in scrapers when enabled

    /// <inheritdoc />
    public bool IsEnabled
    {
        get
        {
            var config = Plugin.Instance?.Configuration;
            if (config is null || string.IsNullOrWhiteSpace(config.MetaTubeServerUrl))
            {
                return false;
            }

            // Reconfigure client on each check to pick up config changes
            _client.Configure(config.MetaTubeServerUrl, config.MetaTubeToken);
            return true;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SourceSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var response = await _client.SearchMovieAsync(query, cancellationToken).ConfigureAwait(false);
        if (response?.Data is null or { Count: 0 })
        {
            return [];
        }

        return response.Data.Select(item => new SourceSearchResult
        {
            Code = item.Number,
            Title = item.Title,
            ThumbnailUrl = item.CoverUrl,
            ReleaseDate = item.ReleaseDate,
            SourceName = Name,
            SourceId = $"{item.Provider}:{item.Id}"
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<MovieMetadata?> GetMovieAsync(string sourceId, CancellationToken cancellationToken)
    {
        var parts = sourceId.Split(':', 2);
        if (parts.Length != 2)
        {
            _logger.LogWarning("Invalid MetaTube source ID format: {SourceId}", sourceId);
            return null;
        }

        var provider = parts[0];
        var id = parts[1];

        var response = await _client.GetMovieAsync(provider, id, cancellationToken).ConfigureAwait(false);
        var data = response?.Data;
        if (data is null)
        {
            return null;
        }

        return new MovieMetadata
        {
            Code = data.Number,
            OriginalTitle = data.Title,
            Title = data.Title,
            Overview = data.Summary,
            ReleaseDate = data.ReleaseDate,
            RuntimeMinutes = data.Runtime,
            Director = data.Director,
            Studio = data.Maker,
            Label = data.Label,
            Series = data.Series,
            Genres = data.Genres,
            Actors = data.Actors.Select(a => new ActorInfo
            {
                Name = a.Name,
                ImageUrl = a.Images.FirstOrDefault()
            }).ToList(),
            CoverUrl = data.CoverUrl,
            BackdropUrl = data.BackdropUrl,
            SourceName = Name,
            SourceId = sourceId
        };
    }

    /// <inheritdoc />
    public async Task<ActorMetadata?> GetActorAsync(string name, CancellationToken cancellationToken)
    {
        var response = await _client.SearchActorAsync(name, cancellationToken).ConfigureAwait(false);
        var first = response?.Data?.FirstOrDefault();
        if (first is null)
        {
            return null;
        }

        return new ActorMetadata
        {
            Name = first.Name,
            ImageUrl = first.Images.FirstOrDefault()
        };
    }
}
