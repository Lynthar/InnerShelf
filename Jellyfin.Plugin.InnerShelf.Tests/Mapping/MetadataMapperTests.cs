using Jellyfin.Data.Enums;
using Jellyfin.Plugin.InnerShelf.Mapping;
using Jellyfin.Plugin.InnerShelf.Sources.Models;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.InnerShelf.Tests.Mapping;

public class MetadataMapperTests
{
    [Fact]
    public void ToMovieResult_MapsAllFields()
    {
        var source = new MovieMetadata
        {
            Code = "SSIS-001",
            OriginalTitle = "テストタイトル",
            Title = "Test Title",
            Overview = "A test movie",
            ReleaseDate = new DateTime(2024, 1, 15),
            RuntimeMinutes = 120,
            Director = "Test Director",
            Studio = "S1 NO.1 STYLE",
            Label = "S1",
            Series = "Test Series",
            Genres = ["Drama", "Romance"],
            Actors =
            [
                new ActorInfo { Name = "Test Actress", ImageUrl = "https://example.com/photo.jpg" }
            ],
            CoverUrl = "https://example.com/cover.jpg",
            BackdropUrl = "https://example.com/backdrop.jpg"
        };

        var result = MetadataMapper.ToMovieResult(source);

        Assert.True(result.HasMetadata);
        var movie = result.Item;

        // Title (default template: "{code} {title}")
        Assert.Contains("SSIS-001", movie.Name);
        Assert.Contains("Test Title", movie.Name);
        Assert.Equal("テストタイトル", movie.OriginalTitle);
        Assert.Equal("A test movie", movie.Overview);
        Assert.Equal("XXX", movie.OfficialRating);
        Assert.Equal(new DateTime(2024, 1, 15), movie.PremiereDate);
        Assert.Equal(2024, movie.ProductionYear);
        Assert.Equal(TimeSpan.FromMinutes(120).Ticks, movie.RunTimeTicks);

        // Provider ID
        Assert.Equal("SSIS-001", movie.ProviderIds[MetadataMapper.ProviderKey]);

        // Studios
        Assert.Contains("S1 NO.1 STYLE", movie.Studios);

        // Genres
        Assert.Contains("Drama", movie.Genres);
        Assert.Contains("Romance", movie.Genres);

        // Tags
        Assert.Contains("Label: S1", movie.Tags);
        Assert.Contains("Series: Test Series", movie.Tags);

        // People
        Assert.Single(result.People, p => p.Name == "Test Actress" && p.Type == PersonKind.Actor);
        Assert.Single(result.People, p => p.Name == "Test Director" && p.Type == PersonKind.Director);

        // Images
        Assert.Contains(result.RemoteImages, i => i.Url == "https://example.com/cover.jpg" && i.Type == ImageType.Primary);
        Assert.Contains(result.RemoteImages, i => i.Url == "https://example.com/backdrop.jpg" && i.Type == ImageType.Backdrop);
    }

    [Fact]
    public void ToMovieResult_HandlesMinimalData()
    {
        var source = new MovieMetadata
        {
            Code = "TEST-001"
        };

        var result = MetadataMapper.ToMovieResult(source);

        Assert.True(result.HasMetadata);
        Assert.Contains("TEST-001", result.Item.Name);
        Assert.Equal("XXX", result.Item.OfficialRating);
        Assert.Empty(result.People);
    }

    [Fact]
    public void ToSearchResult_MapsFields()
    {
        var source = new SourceSearchResult
        {
            Code = "SSIS-001",
            Title = "Test",
            ThumbnailUrl = "https://example.com/thumb.jpg",
            ReleaseDate = new DateTime(2024, 1, 15),
            SourceName = "JavBus",
            SourceId = "SSIS-001"
        };

        var result = MetadataMapper.ToSearchResult(source);

        Assert.Equal("Test", result.Name);
        Assert.Equal("https://example.com/thumb.jpg", result.ImageUrl);
        Assert.Equal(2024, result.ProductionYear);
    }

    [Fact]
    public void ToPersonResult_MapsFields()
    {
        var source = new ActorMetadata
        {
            Name = "Test Actress",
            ImageUrl = "https://example.com/photo.jpg",
            BirthDate = new DateTime(1995, 6, 15)
        };

        var result = MetadataMapper.ToPersonResult(source);

        Assert.True(result.HasMetadata);
        Assert.Equal("Test Actress", result.Item.Name);
        Assert.Equal(new DateTime(1995, 6, 15), result.Item.PremiereDate);
        Assert.Single(result.RemoteImages);
    }
}
