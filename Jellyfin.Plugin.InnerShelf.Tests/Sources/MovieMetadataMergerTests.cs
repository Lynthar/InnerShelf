using Jellyfin.Plugin.InnerShelf.Sources;
using Jellyfin.Plugin.InnerShelf.Sources.Models;
using Xunit;

namespace Jellyfin.Plugin.InnerShelf.Tests.Sources;

public class MovieMetadataMergerTests
{
    private static MovieMetadata Empty(string code = "SSIS-001") => new() { Code = code };

    // --- Scalar fill-only-missing ---

    [Fact]
    public void Merge_PrimaryHasField_KeepsPrimary()
    {
        var primary = new MovieMetadata
        {
            Code = "SSIS-001",
            Title = "Primary Title",
            Director = "Primary Director",
            ReleaseDate = new DateTime(2024, 1, 15),
        };
        var secondary = new MovieMetadata
        {
            Code = "SSIS-001",
            Title = "Secondary Title",
            Director = "Secondary Director",
            ReleaseDate = new DateTime(2023, 5, 20),
        };

        var merged = MovieMetadataMerger.Merge(primary, secondary);

        Assert.Equal("Primary Title", merged.Title);
        Assert.Equal("Primary Director", merged.Director);
        Assert.Equal(new DateTime(2024, 1, 15), merged.ReleaseDate);
    }

    [Fact]
    public void Merge_PrimaryMissing_FillsFromSecondary()
    {
        var primary = new MovieMetadata { Code = "SSIS-001" };
        var secondary = new MovieMetadata
        {
            Code = "SSIS-001",
            Title = "From Secondary",
            Overview = "From Secondary",
            Director = "From Secondary",
            Studio = "From Secondary",
            Label = "From Secondary",
            Series = "From Secondary",
            CoverUrl = "https://example.com/cover.jpg",
            BackdropUrl = "https://example.com/backdrop.jpg",
            ReleaseDate = new DateTime(2024, 1, 15),
            RuntimeMinutes = 120,
        };

        var merged = MovieMetadataMerger.Merge(primary, secondary);

        Assert.Equal("From Secondary", merged.Title);
        Assert.Equal("From Secondary", merged.Overview);
        Assert.Equal("From Secondary", merged.Director);
        Assert.Equal("From Secondary", merged.Studio);
        Assert.Equal("From Secondary", merged.Label);
        Assert.Equal("From Secondary", merged.Series);
        Assert.Equal("https://example.com/cover.jpg", merged.CoverUrl);
        Assert.Equal("https://example.com/backdrop.jpg", merged.BackdropUrl);
        Assert.Equal(new DateTime(2024, 1, 15), merged.ReleaseDate);
        Assert.Equal(120, merged.RuntimeMinutes);
    }

    [Fact]
    public void Merge_EmptyStringTreatedAsMissing()
    {
        var primary = new MovieMetadata { Code = "SSIS-001", Title = string.Empty };
        var secondary = new MovieMetadata { Code = "SSIS-001", Title = "From Secondary" };

        var merged = MovieMetadataMerger.Merge(primary, secondary);

        Assert.Equal("From Secondary", merged.Title);
    }

    [Fact]
    public void Merge_PreservesPrimaryCodeAndSourceTraceability()
    {
        var primary = new MovieMetadata
        {
            Code = "SSIS-001",
            SourceName = "MetaTube",
            SourceId = "javbus:SSIS-001",
        };
        var secondary = new MovieMetadata
        {
            Code = "SSIS-001",
            SourceName = "JavBus",
            SourceId = "SSIS-001",
        };

        var merged = MovieMetadataMerger.Merge(primary, secondary);

        Assert.Equal("MetaTube", merged.SourceName);
        Assert.Equal("javbus:SSIS-001", merged.SourceId);
    }

    [Fact]
    public void Merge_DoesNotMutateInputs()
    {
        var primary = new MovieMetadata { Code = "SSIS-001", Title = "Primary" };
        var secondary = new MovieMetadata { Code = "SSIS-001", Title = "Secondary", Director = "Sec Director" };

        var merged = MovieMetadataMerger.Merge(primary, secondary);

        Assert.Null(primary.Director);     // primary not given Director from secondary
        Assert.Equal("Primary", primary.Title);
        Assert.Equal("Secondary", secondary.Title);  // secondary unchanged
    }

    // --- Genres union-merge ---

    [Fact]
    public void Merge_Genres_UnionPrimaryFirst()
    {
        var primary = new MovieMetadata { Code = "SSIS-001", Genres = ["Drama", "Romance"] };
        var secondary = new MovieMetadata { Code = "SSIS-001", Genres = ["Comedy", "Drama"] };

        var merged = MovieMetadataMerger.Merge(primary, secondary);

        Assert.Equal(["Drama", "Romance", "Comedy"], merged.Genres);
    }

    [Fact]
    public void Merge_Genres_DedupCaseInsensitive()
    {
        var primary = new MovieMetadata { Code = "SSIS-001", Genres = ["Drama"] };
        var secondary = new MovieMetadata { Code = "SSIS-001", Genres = ["drama", "Romance"] };

        var merged = MovieMetadataMerger.Merge(primary, secondary);

        Assert.Equal(["Drama", "Romance"], merged.Genres);
    }

    [Fact]
    public void Merge_Genres_PrimaryEmpty_TakesAllSecondary()
    {
        var primary = new MovieMetadata { Code = "SSIS-001" };
        var secondary = new MovieMetadata { Code = "SSIS-001", Genres = ["Drama", "Romance"] };

        var merged = MovieMetadataMerger.Merge(primary, secondary);

        Assert.Equal(["Drama", "Romance"], merged.Genres);
    }

    // --- Actors merge by name ---

    [Fact]
    public void Merge_Actors_DedupByName_PrimaryPhotoWins()
    {
        var primary = new MovieMetadata
        {
            Code = "SSIS-001",
            Actors =
            [
                new ActorInfo { Name = "Test Actress", ImageUrl = "https://primary/photo.jpg" },
            ],
        };
        var secondary = new MovieMetadata
        {
            Code = "SSIS-001",
            Actors =
            [
                new ActorInfo { Name = "Test Actress", ImageUrl = "https://secondary/photo.jpg" },
                new ActorInfo { Name = "Other Actress", ImageUrl = "https://secondary/other.jpg" },
            ],
        };

        var merged = MovieMetadataMerger.Merge(primary, secondary);

        Assert.Equal(2, merged.Actors.Count);
        var first = merged.Actors[0];
        Assert.Equal("Test Actress", first.Name);
        Assert.Equal("https://primary/photo.jpg", first.ImageUrl);  // primary wins
        var second = merged.Actors[1];
        Assert.Equal("Other Actress", second.Name);
        Assert.Equal("https://secondary/other.jpg", second.ImageUrl);
    }

    [Fact]
    public void Merge_Actors_PrimaryMissingPhoto_SecondaryFillsIt()
    {
        var primary = new MovieMetadata
        {
            Code = "SSIS-001",
            Actors = [new ActorInfo { Name = "Test Actress", ImageUrl = null }],
        };
        var secondary = new MovieMetadata
        {
            Code = "SSIS-001",
            Actors = [new ActorInfo { Name = "Test Actress", ImageUrl = "https://secondary/photo.jpg" }],
        };

        var merged = MovieMetadataMerger.Merge(primary, secondary);

        Assert.Single(merged.Actors);
        Assert.Equal("https://secondary/photo.jpg", merged.Actors[0].ImageUrl);
    }

    [Fact]
    public void Merge_Actors_DedupCaseInsensitive()
    {
        var primary = new MovieMetadata
        {
            Code = "SSIS-001",
            Actors = [new ActorInfo { Name = "test actress", ImageUrl = "primary" }],
        };
        var secondary = new MovieMetadata
        {
            Code = "SSIS-001",
            Actors = [new ActorInfo { Name = "Test Actress", ImageUrl = "secondary" }],
        };

        var merged = MovieMetadataMerger.Merge(primary, secondary);

        Assert.Single(merged.Actors);
        Assert.Equal("primary", merged.Actors[0].ImageUrl);
    }

    [Fact]
    public void Merge_Actors_PreservesPrimaryOrderThenAppendsNewSecondary()
    {
        var primary = new MovieMetadata
        {
            Code = "SSIS-001",
            Actors =
            [
                new ActorInfo { Name = "A" },
                new ActorInfo { Name = "B" },
            ],
        };
        var secondary = new MovieMetadata
        {
            Code = "SSIS-001",
            Actors =
            [
                new ActorInfo { Name = "C" },
                new ActorInfo { Name = "B" },  // dup, ignored
                new ActorInfo { Name = "D" },
            ],
        };

        var merged = MovieMetadataMerger.Merge(primary, secondary);

        Assert.Equal(["A", "B", "C", "D"], merged.Actors.Select(a => a.Name));
    }

    // --- HasAllPrimaryFields ---

    [Fact]
    public void HasAllPrimaryFields_EmptyRecord_ReturnsFalse()
    {
        Assert.False(MovieMetadataMerger.HasAllPrimaryFields(Empty()));
    }

    [Fact]
    public void HasAllPrimaryFields_AllRequiredPresent_ReturnsTrue()
    {
        var m = new MovieMetadata
        {
            Code = "SSIS-001",
            Title = "Test",
            CoverUrl = "https://example.com/cover.jpg",
            ReleaseDate = new DateTime(2024, 1, 15),
            Genres = ["Drama"],
            Actors = [new ActorInfo { Name = "Test Actress" }],
        };

        Assert.True(MovieMetadataMerger.HasAllPrimaryFields(m));
    }

    [Fact]
    public void HasAllPrimaryFields_MissingAnyOne_ReturnsFalse()
    {
        var template = new MovieMetadata
        {
            Code = "SSIS-001",
            Title = "Test",
            CoverUrl = "https://example.com/cover.jpg",
            ReleaseDate = new DateTime(2024, 1, 15),
            Genres = ["Drama"],
            Actors = [new ActorInfo { Name = "Test Actress" }],
        };
        Assert.True(MovieMetadataMerger.HasAllPrimaryFields(template));

        // Each "missing one field" mutation should flip the predicate to false.
        var noTitle = Clone(template); noTitle.Title = null; Assert.False(MovieMetadataMerger.HasAllPrimaryFields(noTitle));
        var noCover = Clone(template); noCover.CoverUrl = null; Assert.False(MovieMetadataMerger.HasAllPrimaryFields(noCover));
        var noDate = Clone(template); noDate.ReleaseDate = null; Assert.False(MovieMetadataMerger.HasAllPrimaryFields(noDate));
        var noGenres = Clone(template); noGenres.Genres = []; Assert.False(MovieMetadataMerger.HasAllPrimaryFields(noGenres));
        var noActors = Clone(template); noActors.Actors = []; Assert.False(MovieMetadataMerger.HasAllPrimaryFields(noActors));
    }

    private static MovieMetadata Clone(MovieMetadata m) => new()
    {
        Code = m.Code,
        Title = m.Title,
        CoverUrl = m.CoverUrl,
        ReleaseDate = m.ReleaseDate,
        Genres = m.Genres.ToList(),
        Actors = m.Actors.ToList(),
    };
}
