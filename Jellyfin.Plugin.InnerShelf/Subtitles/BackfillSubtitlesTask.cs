using Jellyfin.Data.Enums;
using Jellyfin.Plugin.InnerShelf.Mapping;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.InnerShelf.Subtitles;

/// <summary>
/// Library-wide subtitle backfill: scans every InnerShelf-managed movie and
/// submits a subtitle-forge job for any that's missing a sidecar SRT in one
/// of the configured target languages. Surfaces in Dashboard → Scheduled
/// Tasks under the "InnerShelf" category. No default trigger — runs only
/// when the admin clicks "Run now" or sets a cron themselves.
/// </summary>
public class BackfillSubtitlesTask : IScheduledTask
{
    private readonly ILogger<BackfillSubtitlesTask> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly SubtitleForgeClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackfillSubtitlesTask"/> class.
    /// </summary>
    public BackfillSubtitlesTask(
        ILogger<BackfillSubtitlesTask> logger,
        ILibraryManager libraryManager,
        SubtitleForgeClient client)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _client = client;
    }

    /// <inheritdoc />
    public string Name => "InnerShelf: Backfill subtitles";

    /// <inheritdoc />
    public string Key => "InnerShelfBackfillSubtitles";

    /// <inheritdoc />
    public string Description =>
        "Submits subtitle-forge jobs for InnerShelf-managed movies missing a sidecar SRT in any of the configured target languages.";

    /// <inheritdoc />
    public string Category => "InnerShelf";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || !SubtitleForgeClient.IsConfigured)
        {
            _logger.LogWarning("subtitle-forge is not configured; backfill skipped");
            progress.Report(100);
            return;
        }

        var targetLanguages = ParseLanguages(config.SubtitleLanguages);
        if (targetLanguages.Count == 0)
        {
            _logger.LogWarning("no target subtitle languages configured; backfill skipped");
            progress.Report(100);
            return;
        }

        // Only Movies, recursive across all libraries. Filtering for the
        // InnerShelf provider id and missing sidecars happens in code below;
        // InternalItemsQuery's HasAnyProviderId varies across Jellyfin versions
        // so the in-memory filter is the more portable choice.
        var query = new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            Recursive = true,
        };

        var candidates = new List<(Movie Movie, IReadOnlyList<string> Missing)>();
        foreach (var item in _libraryManager.GetItemList(query))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item is not Movie movie)
            {
                continue;
            }

            if (!movie.ProviderIds.ContainsKey(MetadataMapper.ProviderKey))
            {
                continue;
            }

            if (string.IsNullOrEmpty(movie.Path))
            {
                continue;
            }

            var missing = SidecarSubtitleProbe.MissingLanguages(movie.Path, targetLanguages);
            if (missing.Count > 0)
            {
                candidates.Add((movie, missing));
            }
        }

        if (candidates.Count == 0)
        {
            _logger.LogInformation("backfill: nothing to do, all InnerShelf movies have configured subtitles");
            progress.Report(100);
            return;
        }

        _logger.LogInformation("backfill: {Count} movie(s) need subtitle generation", candidates.Count);

        var done = 0;
        var submitted = 0;
        foreach (var (movie, missing) in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var remotePath = PathMapper.Map(movie.Path, config.SubtitlePathMappings);
                var accepted = await _client.SubmitJobAsync(
                    new SubtitleJobRequest
                    {
                        VideoPath = remotePath,
                        TargetLanguages = missing,
                        Bilingual = config.SubtitleBilingual,
                        KeepOriginal = config.SubtitleKeepOriginal,
                    },
                    cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "backfill: submitted job {JobId} for {ItemId} ({Path}), languages: {Languages}",
                    accepted.JobId, movie.Id, movie.Path, string.Join(",", missing));
                submitted++;
            }
            catch (SubtitleForgeException ex)
            {
                _logger.LogWarning(ex, "backfill: submission failed for {ItemId} ({Path})", movie.Id, movie.Path);
            }

            done++;
            progress.Report(100.0 * done / candidates.Count);
        }

        _logger.LogInformation("backfill complete: {Submitted}/{Total} jobs submitted", submitted, candidates.Count);
    }

    private static List<string> ParseLanguages(string raw) =>
        raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .ToList();
}
