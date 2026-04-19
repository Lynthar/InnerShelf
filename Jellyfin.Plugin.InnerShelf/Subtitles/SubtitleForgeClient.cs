using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.InnerShelf.Subtitles;

/// <summary>
/// HTTP client for the subtitle-forge server. Reads server URL and bearer
/// token from <see cref="Configuration.PluginConfiguration"/> on each call so
/// configuration changes take effect without restarting Jellyfin.
/// </summary>
public class SubtitleForgeClient
{
    private readonly ILogger<SubtitleForgeClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleForgeClient"/> class.
    /// </summary>
    public SubtitleForgeClient(ILogger<SubtitleForgeClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Gets a value indicating whether subtitle-forge is configured.
    /// </summary>
    public static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Plugin.Instance?.Configuration.SubtitleForgeServerUrl);

    /// <summary>
    /// Submits a subtitle generation job. Returns the assigned job id.
    /// </summary>
    public async Task<SubtitleJobAccepted> SubmitJobAsync(SubtitleJobRequest request, CancellationToken cancellationToken)
    {
        var response = await SendAsync<SubtitleJobAccepted>(
            HttpMethod.Post,
            "jobs",
            request,
            cancellationToken).ConfigureAwait(false)
            ?? throw new SubtitleForgeException("subtitle-forge returned an empty response to job submission");
        return response;
    }

    /// <summary>
    /// Fetches the current state of a previously submitted job.
    /// </summary>
    public async Task<SubtitleJobStatus?> GetJobAsync(string jobId, CancellationToken cancellationToken)
    {
        return await SendAsync<SubtitleJobStatus>(
            HttpMethod.Get,
            $"jobs/{Uri.EscapeDataString(jobId)}",
            body: null,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        var serverUrl = config?.SubtitleForgeServerUrl;
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            throw new SubtitleForgeException("subtitle-forge server URL is not configured");
        }

        var httpClient = _httpClientFactory.CreateClient(PluginServiceRegistrator.SubtitleForgeHttpClientName);
        var requestUri = new Uri(new Uri(serverUrl.TrimEnd('/') + "/"), path);

        using var request = new HttpRequestMessage(method, requestUri);

        var token = config?.SubtitleForgeToken;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var bodyText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogWarning(
                    "subtitle-forge {Method} {Path} returned {Status}: {Body}",
                    method, path, (int)response.StatusCode, bodyText);
                throw new SubtitleForgeException(
                    $"subtitle-forge returned {(int)response.StatusCode}: {bodyText}");
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to reach subtitle-forge at {Uri}", requestUri);
            throw new SubtitleForgeException(
                $"Failed to reach subtitle-forge at {requestUri}: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Thrown when subtitle-forge is unreachable, returns a non-success status,
/// or otherwise misbehaves. Surfaced as 502 Bad Gateway by the controller.
/// </summary>
public class SubtitleForgeException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="SubtitleForgeException"/> class.</summary>
    public SubtitleForgeException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SubtitleForgeException"/> class.</summary>
    public SubtitleForgeException(string message, Exception inner) : base(message, inner)
    {
    }
}

// --- Request / response DTOs ---

/// <summary>Subtitle job submission payload.</summary>
public class SubtitleJobRequest
{
    /// <summary>Gets or sets the absolute video path as the subtitle-forge server sees it.</summary>
    [JsonPropertyName("video_path")]
    public required string VideoPath { get; set; }

    /// <summary>Gets or sets the target subtitle languages.</summary>
    [JsonPropertyName("target_languages")]
    public required IReadOnlyList<string> TargetLanguages { get; set; }

    /// <summary>Gets or sets the source language; null means auto-detect.</summary>
    [JsonPropertyName("source_language")]
    public string? SourceLanguage { get; set; }

    /// <summary>Gets or sets a value indicating whether bilingual subtitles should be produced.</summary>
    [JsonPropertyName("bilingual")]
    public bool Bilingual { get; set; }

    /// <summary>Gets or sets a value indicating whether the original-language subtitle should be kept.</summary>
    [JsonPropertyName("keep_original")]
    public bool KeepOriginal { get; set; } = true;
}

/// <summary>Returned immediately after a job is accepted.</summary>
public class SubtitleJobAccepted
{
    /// <summary>Gets or sets the job id.</summary>
    [JsonPropertyName("job_id")]
    public string JobId { get; set; } = string.Empty;

    /// <summary>Gets or sets the initial status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>Full job status returned by GET /jobs/{id}.</summary>
public class SubtitleJobStatus
{
    /// <summary>Gets or sets the job id.</summary>
    [JsonPropertyName("job_id")]
    public string JobId { get; set; } = string.Empty;

    /// <summary>Gets or sets the current status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the video path on the subtitle-forge server.</summary>
    [JsonPropertyName("video_path")]
    public string VideoPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the target languages.</summary>
    [JsonPropertyName("target_languages")]
    public IReadOnlyList<string> TargetLanguages { get; set; } = [];

    /// <summary>Gets or sets the (possibly auto-detected) source language.</summary>
    [JsonPropertyName("source_language")]
    public string? SourceLanguage { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the start timestamp.</summary>
    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; set; }

    /// <summary>Gets or sets the completion timestamp.</summary>
    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>Gets or sets the error message, if any.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Gets or sets the produced output files.</summary>
    [JsonPropertyName("outputs")]
    public IReadOnlyList<SubtitleJobOutput> Outputs { get; set; } = [];
}

/// <summary>A single output file produced by a subtitle job.</summary>
public class SubtitleJobOutput
{
    /// <summary>Gets or sets the language code.</summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    /// <summary>Gets or sets the absolute path of the output file (on the subtitle-forge server).</summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}
