using Jellyfin.Plugin.InnerShelf.Sources;
using Jellyfin.Plugin.InnerShelf.Sources.Models;
using Xunit;

namespace Jellyfin.Plugin.InnerShelf.Tests.Sources;

public class MovieMetadataCacheTests
{
    private static MovieMetadata Sample(string code = "SSIS-001") =>
        new() { Code = code, Title = "Sample" };

    [Fact]
    public void TryGet_EmptyCache_ReturnsFalse()
    {
        var cache = new MovieMetadataCache();
        Assert.False(cache.TryGet("SSIS-001", out var v));
        Assert.Null(v);
    }

    [Fact]
    public void Set_ThenTryGet_ReturnsCachedValue()
    {
        var cache = new MovieMetadataCache();
        var movie = Sample();
        cache.Set("SSIS-001", movie);

        Assert.True(cache.TryGet("SSIS-001", out var got));
        Assert.Same(movie, got);
    }

    [Fact]
    public void Set_Null_IsCachedAsNegativeResult()
    {
        // Caching nulls is the whole point of "don't re-query for codes
        // we already know nobody can resolve".
        var cache = new MovieMetadataCache();
        cache.Set("UNKNOWN-001", null);

        Assert.True(cache.TryGet("UNKNOWN-001", out var got));
        Assert.Null(got);
    }

    [Fact]
    public void TryGet_AfterTtl_ReturnsFalseAndDropsEntry()
    {
        var cache = new MovieMetadataCache(TimeSpan.FromMilliseconds(50), maxEntries: 100);
        cache.Set("SSIS-001", Sample());
        Assert.Equal(1, cache.Count);

        Thread.Sleep(120);

        Assert.False(cache.TryGet("SSIS-001", out _));
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Set_OverwritesExistingEntry()
    {
        var cache = new MovieMetadataCache();
        var first = new MovieMetadata { Code = "SSIS-001", Title = "First" };
        var second = new MovieMetadata { Code = "SSIS-001", Title = "Second" };

        cache.Set("SSIS-001", first);
        cache.Set("SSIS-001", second);

        Assert.True(cache.TryGet("SSIS-001", out var got));
        Assert.Same(second, got);
    }

    [Fact]
    public void Set_EmptyOrNullKey_IsNoop()
    {
        var cache = new MovieMetadataCache();
        cache.Set(string.Empty, Sample());
        cache.Set(null!, Sample());

        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void TryGet_EmptyOrNullKey_ReturnsFalse()
    {
        var cache = new MovieMetadataCache();
        Assert.False(cache.TryGet(string.Empty, out _));
        Assert.False(cache.TryGet(null!, out _));
    }

    [Fact]
    public void Set_AtCapacity_SweepsExpiredFirst()
    {
        // Fill with entries that'll expire shortly. After expiry a new
        // write should succeed because the sweep makes room.
        var cache = new MovieMetadataCache(TimeSpan.FromMilliseconds(50), maxEntries: 3);
        cache.Set("a", Sample("a"));
        cache.Set("b", Sample("b"));
        cache.Set("c", Sample("c"));
        Assert.Equal(3, cache.Count);

        Thread.Sleep(120);
        cache.Set("d", Sample("d"));

        // Old expired entries swept; only the new one remains.
        Assert.True(cache.TryGet("d", out var got));
        Assert.NotNull(got);
        Assert.Equal("d", got!.Code);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Set_AtCapacityWithNoExpired_DropsNewWrite()
    {
        // If we're at cap and nothing has expired, the new write is dropped
        // rather than evicting a live entry. Best-effort cache semantics.
        var cache = new MovieMetadataCache(TimeSpan.FromHours(1), maxEntries: 2);
        cache.Set("a", Sample("a"));
        cache.Set("b", Sample("b"));
        cache.Set("c", Sample("c"));   // dropped

        Assert.Equal(2, cache.Count);
        Assert.True(cache.TryGet("a", out _));
        Assert.True(cache.TryGet("b", out _));
        Assert.False(cache.TryGet("c", out _));
    }

    [Fact]
    public void Constructor_RejectsNonPositiveTtl()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MovieMetadataCache(TimeSpan.Zero, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MovieMetadataCache(TimeSpan.FromSeconds(-1), 100));
    }

    [Fact]
    public void Constructor_RejectsNonPositiveMaxEntries()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MovieMetadataCache(TimeSpan.FromMinutes(1), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MovieMetadataCache(TimeSpan.FromMinutes(1), -5));
    }
}
