using Jellyfin.Plugin.InnerShelf.Configuration;
using Jellyfin.Plugin.InnerShelf.Subtitles;
using Xunit;

namespace Jellyfin.Plugin.InnerShelf.Tests.Subtitles;

public class PathMapperTests
{
    [Fact]
    public void Map_NoMappings_ReturnsInputUnchanged()
    {
        var result = PathMapper.Map("/media/jav/SSIS-001.mp4", []);
        Assert.Equal("/media/jav/SSIS-001.mp4", result);
    }

    [Fact]
    public void Map_NoMatchingPrefix_ReturnsInputUnchanged()
    {
        var mappings = new PathMapping[]
        {
            new() { JellyfinPrefix = "/media/movies", RemotePrefix = "/Volumes/movies" },
        };
        var result = PathMapper.Map("/media/jav/X.mp4", mappings);
        Assert.Equal("/media/jav/X.mp4", result);
    }

    [Fact]
    public void Map_SimpleMatch_ReplacesPrefix()
    {
        var mappings = new PathMapping[]
        {
            new() { JellyfinPrefix = "/media/jav", RemotePrefix = "/Volumes/nas-jav" },
        };
        var result = PathMapper.Map("/media/jav/SSIS-001.mp4", mappings);
        Assert.Equal("/Volumes/nas-jav/SSIS-001.mp4", result);
    }

    [Fact]
    public void Map_TrailingSlashOnPrefix_NormalizedBeforeMatch()
    {
        var mappings = new PathMapping[]
        {
            new() { JellyfinPrefix = "/media/jav/", RemotePrefix = "/Volumes/nas-jav/" },
        };
        var result = PathMapper.Map("/media/jav/X.mp4", mappings);
        Assert.Equal("/Volumes/nas-jav/X.mp4", result);
    }

    [Fact]
    public void Map_LongestPrefixWins()
    {
        var mappings = new PathMapping[]
        {
            new() { JellyfinPrefix = "/media", RemotePrefix = "/Volumes/all" },
            new() { JellyfinPrefix = "/media/jav", RemotePrefix = "/Volumes/nas-jav" },
        };
        var result = PathMapper.Map("/media/jav/X.mp4", mappings);
        Assert.Equal("/Volumes/nas-jav/X.mp4", result);
    }

    [Fact]
    public void Map_PrefixOrderIndependent()
    {
        // Same as previous, but the more-specific rule listed first.
        var mappings = new PathMapping[]
        {
            new() { JellyfinPrefix = "/media/jav", RemotePrefix = "/Volumes/nas-jav" },
            new() { JellyfinPrefix = "/media", RemotePrefix = "/Volumes/all" },
        };
        var result = PathMapper.Map("/media/jav/X.mp4", mappings);
        Assert.Equal("/Volumes/nas-jav/X.mp4", result);
    }

    [Fact]
    public void Map_DoesNotMatchPartialPathSegment()
    {
        // /media/jav must not match /media/javier — would corrupt the path.
        var mappings = new PathMapping[]
        {
            new() { JellyfinPrefix = "/media/jav", RemotePrefix = "/Volumes/nas-jav" },
        };
        var result = PathMapper.Map("/media/javier/X.mp4", mappings);
        Assert.Equal("/media/javier/X.mp4", result);
    }

    [Fact]
    public void Map_ExactMatchWithoutTrailingSlash()
    {
        var mappings = new PathMapping[]
        {
            new() { JellyfinPrefix = "/media/jav", RemotePrefix = "/Volumes/nas-jav" },
        };
        var result = PathMapper.Map("/media/jav", mappings);
        Assert.Equal("/Volumes/nas-jav", result);
    }

    [Fact]
    public void Map_EmptyPath_ReturnsEmpty()
    {
        var mappings = new PathMapping[]
        {
            new() { JellyfinPrefix = "/media/jav", RemotePrefix = "/Volumes/nas-jav" },
        };
        Assert.Equal(string.Empty, PathMapper.Map(string.Empty, mappings));
    }

    [Fact]
    public void Map_EmptyPrefixIgnored()
    {
        var mappings = new PathMapping[]
        {
            new() { JellyfinPrefix = string.Empty, RemotePrefix = "/whatever" },
            new() { JellyfinPrefix = "/media/jav", RemotePrefix = "/Volumes/nas-jav" },
        };
        var result = PathMapper.Map("/media/jav/X.mp4", mappings);
        Assert.Equal("/Volumes/nas-jav/X.mp4", result);
    }
}
