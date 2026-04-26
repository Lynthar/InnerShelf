using Jellyfin.Plugin.InnerShelf.Subtitles;
using Xunit;

namespace Jellyfin.Plugin.InnerShelf.Tests.Subtitles;

public class SidecarSubtitleProbeTests
{
    private static Func<string, bool> ExistsIn(params string[] paths)
    {
        var set = new HashSet<string>(paths, StringComparer.Ordinal);
        return p => set.Contains(p);
    }

    [Fact]
    public void AllLanguagesPresent_ReturnsEmpty()
    {
        var missing = SidecarSubtitleProbe.MissingLanguages(
            "/media/jav/SSIS-001.mp4",
            ["zh", "en"],
            ExistsIn("/media/jav/SSIS-001.zh.srt", "/media/jav/SSIS-001.en.srt"));

        Assert.Empty(missing);
    }

    [Fact]
    public void NoLanguagesPresent_ReturnsAll()
    {
        var missing = SidecarSubtitleProbe.MissingLanguages(
            "/media/jav/SSIS-001.mp4",
            ["zh", "en"],
            ExistsIn());

        Assert.Equal(["zh", "en"], missing);
    }

    [Fact]
    public void PartialPresence_ReturnsOnlyMissing()
    {
        var missing = SidecarSubtitleProbe.MissingLanguages(
            "/media/jav/SSIS-001.mp4",
            ["zh", "en", "ja"],
            ExistsIn("/media/jav/SSIS-001.zh.srt"));

        Assert.Equal(["en", "ja"], missing);
    }

    [Fact]
    public void SidecarLookupUsesBasenameWithoutExtension()
    {
        // .mkv file ⇒ probe looks for .zh.srt, not .mkv.zh.srt
        var missing = SidecarSubtitleProbe.MissingLanguages(
            "/media/jav/MIDV-001.mkv",
            ["zh"],
            ExistsIn("/media/jav/MIDV-001.zh.srt"));

        Assert.Empty(missing);
    }

    [Fact]
    public void NestedSubfolderPath_LooksInSameDirectory()
    {
        var missing = SidecarSubtitleProbe.MissingLanguages(
            "/media/jav/SSIS-001/SSIS-001.mp4",
            ["zh"],
            ExistsIn("/media/jav/SSIS-001/SSIS-001.zh.srt"));

        Assert.Empty(missing);
    }

    [Fact]
    public void EmptyLanguageList_ReturnsEmpty()
    {
        var missing = SidecarSubtitleProbe.MissingLanguages(
            "/media/jav/SSIS-001.mp4",
            [],
            _ => true);

        Assert.Empty(missing);
    }

    [Fact]
    public void EmptyPath_ReturnsEmpty()
    {
        var missing = SidecarSubtitleProbe.MissingLanguages(
            string.Empty,
            ["zh"],
            _ => true);

        Assert.Empty(missing);
    }

    [Fact]
    public void PathWithNoDirectory_ReturnsEmpty()
    {
        // A bare filename with no directory component can't have a sidecar at
        // a known location — treat as "nothing to do" rather than guessing CWD.
        var missing = SidecarSubtitleProbe.MissingLanguages(
            "SSIS-001.mp4",
            ["zh"],
            _ => false);

        Assert.Empty(missing);
    }

    [Fact]
    public void DefaultsToFileExists_WhenProbeNotProvided()
    {
        // Use a real temp file to verify the default path goes through File.Exists.
        var dir = Directory.CreateTempSubdirectory("inner-shelf-probe-").FullName;
        try
        {
            var video = Path.Combine(dir, "SSIS-001.mp4");
            File.WriteAllText(video, string.Empty);
            File.WriteAllText(Path.Combine(dir, "SSIS-001.zh.srt"), string.Empty);

            var missing = SidecarSubtitleProbe.MissingLanguages(video, ["zh", "en"]);

            Assert.Equal(["en"], missing);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
