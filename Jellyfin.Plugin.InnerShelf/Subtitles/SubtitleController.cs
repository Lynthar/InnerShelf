using System.Net.Mime;
using Jellyfin.Plugin.InnerShelf.Configuration;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.InnerShelf.Subtitles;

/// <summary>
/// Plugin controller exposing subtitle-generation endpoints under
/// <c>/InnerShelf/Subtitles/...</c>. Admin-only (RequiresElevation) because
/// it spawns expensive GPU work on a separate machine.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("InnerShelf/Subtitles")]
[Produces(MediaTypeNames.Application.Json)]
public class SubtitleController : ControllerBase
{
    private readonly ILogger<SubtitleController> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly SubtitleForgeClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleController"/> class.
    /// </summary>
    public SubtitleController(
        ILogger<SubtitleController> logger,
        ILibraryManager libraryManager,
        SubtitleForgeClient client)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _client = client;
    }

    /// <summary>
    /// Submits a subtitle-generation job for the given Jellyfin item.
    /// </summary>
    [HttpPost("Generate")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<SubtitleGenerateResponse>> Generate(
        [FromQuery, BindRequired] Guid itemId,
        [FromQuery] string? languages,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || !SubtitleForgeClient.IsConfigured)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "subtitle-forge not configured",
                detail: "Set Server URL in InnerShelf settings before calling this endpoint.");
        }

        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound(new { detail = $"Item not found: {itemId}" });
        }

        if (item is not Movie)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Unsupported item type",
                detail: $"Subtitle generation only supports Movie items, got {item.GetType().Name}.");
        }

        if (string.IsNullOrEmpty(item.Path))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Item has no file path",
                detail: $"Item {itemId} has no associated file on disk.");
        }

        var targetLanguages = ParseLanguages(languages, config.SubtitleLanguages);
        if (targetLanguages.Count == 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "No target languages",
                detail: "Provide ?languages=zh,en or set default languages in plugin config.");
        }

        var remotePath = PathMapper.Map(item.Path, config.SubtitlePathMappings);
        _logger.LogInformation(
            "Submitting subtitle job for item {ItemId}: {LocalPath} -> {RemotePath}",
            itemId, item.Path, remotePath);

        try
        {
            var accepted = await _client.SubmitJobAsync(
                new SubtitleJobRequest
                {
                    VideoPath = remotePath,
                    TargetLanguages = targetLanguages,
                    Bilingual = config.SubtitleBilingual,
                    KeepOriginal = config.SubtitleKeepOriginal,
                },
                cancellationToken).ConfigureAwait(false);

            return Accepted(new SubtitleGenerateResponse
            {
                JobId = accepted.JobId,
                Status = accepted.Status,
                ItemId = itemId,
                LocalPath = item.Path,
                RemotePath = remotePath,
                TargetLanguages = targetLanguages,
            });
        }
        catch (SubtitleForgeException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "subtitle-forge error",
                detail: ex.Message);
        }
    }

    /// <summary>
    /// Returns the current state of a previously submitted subtitle job.
    /// </summary>
    [HttpGet("Jobs/{jobId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<SubtitleJobStatus>> GetJob(
        [FromRoute] string jobId,
        CancellationToken cancellationToken)
    {
        if (!SubtitleForgeClient.IsConfigured)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "subtitle-forge not configured");
        }

        try
        {
            var status = await _client.GetJobAsync(jobId, cancellationToken).ConfigureAwait(false);
            if (status is null)
            {
                return NotFound(new { detail = $"Job not found: {jobId}" });
            }

            return Ok(status);
        }
        catch (SubtitleForgeException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "subtitle-forge error",
                detail: ex.Message);
        }
    }

    private static List<string> ParseLanguages(string? fromQuery, string fromConfig)
    {
        var source = !string.IsNullOrWhiteSpace(fromQuery) ? fromQuery : fromConfig;
        if (string.IsNullOrWhiteSpace(source))
        {
            return [];
        }

        return source
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .ToList();
    }
}

/// <summary>Response body of POST /Subtitles/Generate.</summary>
public class SubtitleGenerateResponse
{
    /// <summary>Gets or sets the subtitle-forge job id.</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>Gets or sets the initial status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the Jellyfin item id.</summary>
    public Guid ItemId { get; set; }

    /// <summary>Gets or sets the path Jellyfin sees.</summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the path subtitle-forge sees after mapping.</summary>
    public string RemotePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the target subtitle languages.</summary>
    public IReadOnlyList<string> TargetLanguages { get; set; } = [];
}
