namespace Jellyfin.Plugin.InnerShelf.Subtitles;

/// <summary>
/// Decides which target subtitle languages are still missing for a given
/// video by checking for sidecar SRT files alongside the video. Pure (no
/// statics, no I/O when a custom <c>fileExists</c> probe is supplied) so the
/// backfill task's idempotency check is unit-testable without touching disk.
/// </summary>
public static class SidecarSubtitleProbe
{
    /// <summary>
    /// Returns the subset of <paramref name="targetLanguages"/> that don't
    /// already have a <c>&lt;basename&gt;.&lt;lang&gt;.srt</c> sidecar next to
    /// <paramref name="videoPath"/>. An empty result means "all configured
    /// languages already present — skip this item".
    /// </summary>
    /// <param name="videoPath">Absolute path of the video file.</param>
    /// <param name="targetLanguages">Languages the user configured.</param>
    /// <param name="fileExists">Override the existence probe (tests). Defaults to <see cref="File.Exists"/>.</param>
    public static IReadOnlyList<string> MissingLanguages(
        string videoPath,
        IReadOnlyList<string> targetLanguages,
        Func<string, bool>? fileExists = null)
    {
        fileExists ??= File.Exists;

        var dir = Path.GetDirectoryName(videoPath);
        var basename = Path.GetFileNameWithoutExtension(videoPath);
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(basename))
        {
            return [];
        }

        var missing = new List<string>();
        foreach (var lang in targetLanguages)
        {
            var srtPath = Path.Combine(dir, $"{basename}.{lang}.srt");
            if (!fileExists(srtPath))
            {
                missing.Add(lang);
            }
        }

        return missing;
    }
}
