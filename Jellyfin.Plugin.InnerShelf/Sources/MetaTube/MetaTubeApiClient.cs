using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.InnerShelf.Sources.MetaTube;

/// <summary>
/// HTTP client for the MetaTube backend server.
/// </summary>
public class MetaTubeApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetaTubeApiClient"/> class.
    /// </summary>
    public MetaTubeApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Searches for movies.
    /// </summary>
    public async Task<MetaTubeSearchResponse?> SearchMovieAsync(string query, CancellationToken cancellationToken)
    {
        var path = $"v1/movies/search?q={Uri.EscapeDataString(query)}";
        return await GetJsonAsync<MetaTubeSearchResponse>(path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets movie details by provider and ID.
    /// </summary>
    public async Task<MetaTubeMovieResponse?> GetMovieAsync(string provider, string id, CancellationToken cancellationToken)
    {
        var path = $"v1/movies/{Uri.EscapeDataString(provider)}/{Uri.EscapeDataString(id)}";
        return await GetJsonAsync<MetaTubeMovieResponse>(path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for actors.
    /// </summary>
    public async Task<MetaTubeActorSearchResponse?> SearchActorAsync(string name, CancellationToken cancellationToken)
    {
        var path = $"v1/actors/search?q={Uri.EscapeDataString(name)}";
        return await GetJsonAsync<MetaTubeActorSearchResponse>(path, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T?> GetJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        var serverUrl = config?.MetaTubeServerUrl;
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            return default;
        }

        var httpClient = _httpClientFactory.CreateClient(PluginServiceRegistrator.MetaTubeHttpClientName);
        var requestUri = new Uri(new Uri(serverUrl.TrimEnd('/') + "/"), path);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        if (!string.IsNullOrEmpty(config?.MetaTubeToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.MetaTubeToken);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

// --- MetaTube API response models ---

/// <summary>MetaTube search response.</summary>
public class MetaTubeSearchResponse
{
    /// <summary>Gets or sets the results.</summary>
    [JsonPropertyName("data")]
    public List<MetaTubeSearchItem> Data { get; set; } = [];
}

/// <summary>MetaTube search result item.</summary>
public class MetaTubeSearchItem
{
    /// <summary>Gets or sets the provider name.</summary>
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the ID within the provider.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the product code.</summary>
    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    /// <summary>Gets or sets the title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the cover URL.</summary>
    [JsonPropertyName("cover_url")]
    public string? CoverUrl { get; set; }

    /// <summary>Gets or sets the release date.</summary>
    [JsonPropertyName("release_date")]
    public DateTime? ReleaseDate { get; set; }
}

/// <summary>MetaTube movie detail response.</summary>
public class MetaTubeMovieResponse
{
    /// <summary>Gets or sets the data.</summary>
    [JsonPropertyName("data")]
    public MetaTubeMovieData? Data { get; set; }
}

/// <summary>MetaTube movie detail data.</summary>
public class MetaTubeMovieData
{
    /// <summary>Gets or sets the product code.</summary>
    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    /// <summary>Gets or sets the title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the summary.</summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>Gets or sets the release date.</summary>
    [JsonPropertyName("release_date")]
    public DateTime? ReleaseDate { get; set; }

    /// <summary>Gets or sets the runtime in minutes.</summary>
    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    /// <summary>Gets or sets the director.</summary>
    [JsonPropertyName("director")]
    public string? Director { get; set; }

    /// <summary>Gets or sets the maker/studio.</summary>
    [JsonPropertyName("maker")]
    public string? Maker { get; set; }

    /// <summary>Gets or sets the label.</summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>Gets or sets the series.</summary>
    [JsonPropertyName("series")]
    public string? Series { get; set; }

    /// <summary>Gets or sets the genres.</summary>
    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = [];

    /// <summary>Gets or sets the actors.</summary>
    [JsonPropertyName("actors")]
    public List<MetaTubeActorData> Actors { get; set; } = [];

    /// <summary>Gets or sets the cover URL.</summary>
    [JsonPropertyName("cover_url")]
    public string? CoverUrl { get; set; }

    /// <summary>Gets or sets the backdrop URL.</summary>
    [JsonPropertyName("backdrop_url")]
    public string? BackdropUrl { get; set; }

    /// <summary>Gets or sets the provider name.</summary>
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the source ID.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

/// <summary>MetaTube actor data.</summary>
public class MetaTubeActorData
{
    /// <summary>Gets or sets the name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the image URL.</summary>
    [JsonPropertyName("images")]
    public List<string> Images { get; set; } = [];
}

/// <summary>MetaTube actor search response.</summary>
public class MetaTubeActorSearchResponse
{
    /// <summary>Gets or sets the results.</summary>
    [JsonPropertyName("data")]
    public List<MetaTubeActorSearchItem> Data { get; set; } = [];
}

/// <summary>MetaTube actor search result.</summary>
public class MetaTubeActorSearchItem
{
    /// <summary>Gets or sets the name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider.</summary>
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the images.</summary>
    [JsonPropertyName("images")]
    public List<string> Images { get; set; } = [];
}
