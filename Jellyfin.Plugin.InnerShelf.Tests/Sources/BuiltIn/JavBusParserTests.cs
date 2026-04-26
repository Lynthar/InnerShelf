using Jellyfin.Plugin.InnerShelf.Sources.BuiltIn;
using Xunit;

namespace Jellyfin.Plugin.InnerShelf.Tests.Sources.BuiltIn;

public class JavBusParserTests
{
    private const string BaseUrl = "https://www.javbus.com";

    private static string LoadFixture(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Sources", "BuiltIn", "Fixtures", name);
        return File.ReadAllText(path);
    }

    // --- Search results ---

    [Fact]
    public async Task ParseSearchResults_ExtractsAllItems()
    {
        var html = LoadFixture("javbus_search.html");

        var results = await JavBusParser.ParseSearchResultsAsync(html, BaseUrl, "JavBus", default);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task ParseSearchResults_ExtractsTitleAndCode()
    {
        var html = LoadFixture("javbus_search.html");

        var results = await JavBusParser.ParseSearchResultsAsync(html, BaseUrl, "JavBus", default);

        Assert.Equal("SSIS-001", results[0].SourceId);
        Assert.Equal("SSIS-001", results[0].Code);
        Assert.Equal("The Test Movie SSIS-001", results[0].Title);
        Assert.Equal("JavBus", results[0].SourceName);
    }

    [Fact]
    public async Task ParseSearchResults_ExtractsReleaseDateFromSecondDateElement()
    {
        var html = LoadFixture("javbus_search.html");

        var results = await JavBusParser.ParseSearchResultsAsync(html, BaseUrl, "JavBus", default);

        Assert.Equal(new DateTime(2024, 1, 15), results[0].ReleaseDate);
        Assert.Equal(new DateTime(2024, 2, 20), results[1].ReleaseDate);
    }

    [Fact]
    public async Task ParseSearchResults_HandlesMissingDateGracefully()
    {
        // Third item has only one <date> — release date should be null, not a parse error.
        var html = LoadFixture("javbus_search.html");

        var results = await JavBusParser.ParseSearchResultsAsync(html, BaseUrl, "JavBus", default);

        Assert.Null(results[2].ReleaseDate);
    }

    [Fact]
    public async Task ParseSearchResults_ResolvesAbsoluteThumbnailUrl()
    {
        var html = LoadFixture("javbus_search.html");

        var results = await JavBusParser.ParseSearchResultsAsync(html, BaseUrl, "JavBus", default);

        // Root-relative
        Assert.Equal("https://www.javbus.com/pics/cover/ssis-001_b.jpg", results[0].ThumbnailUrl);
        // Protocol-relative
        Assert.Equal("https://cdn.javbus.com/pics/cover/ssis-002_b.jpg", results[1].ThumbnailUrl);
        // Bare path
        Assert.Equal("https://www.javbus.com/pics/cover/ssis-003_b.jpg", results[2].ThumbnailUrl);
    }

    [Fact]
    public async Task ParseSearchResults_TrailingSlashOnHrefDoesNotEatSourceId()
    {
        // The second <a> in the fixture ends in "/" — split on "/" alone would yield "".
        var html = LoadFixture("javbus_search.html");

        var results = await JavBusParser.ParseSearchResultsAsync(html, BaseUrl, "JavBus", default);

        Assert.Equal("SSIS-002", results[1].SourceId);
    }

    [Fact]
    public async Task ParseSearchResults_EmptyPage_ReturnsEmptyList()
    {
        var results = await JavBusParser.ParseSearchResultsAsync(
            "<html><body></body></html>", BaseUrl, "JavBus", default);

        Assert.Empty(results);
    }

    // --- Movie page ---

    [Fact]
    public async Task ParseMoviePage_ExtractsCoreFields()
    {
        var html = LoadFixture("javbus_movie.html");

        var movie = await JavBusParser.ParseMoviePageAsync(html, BaseUrl, "SSIS-001", "JavBus", default);

        Assert.NotNull(movie);
        Assert.Equal("SSIS-001", movie!.Code);
        Assert.Equal("The Test Title 日本語タイトル", movie.Title);
        // OriginalTitle and Title are the same on JavBus.
        Assert.Equal(movie.Title, movie.OriginalTitle);
        Assert.Equal("JavBus", movie.SourceName);
        Assert.Equal("SSIS-001", movie.SourceId);
    }

    [Fact]
    public async Task ParseMoviePage_StripsCodePrefixFromTitle()
    {
        var html = LoadFixture("javbus_movie.html");

        var movie = await JavBusParser.ParseMoviePageAsync(html, BaseUrl, "SSIS-001", "JavBus", default);

        Assert.NotNull(movie);
        Assert.DoesNotContain("SSIS-001", movie!.Title);
    }

    [Fact]
    public async Task ParseMoviePage_ExtractsInfoFields()
    {
        var html = LoadFixture("javbus_movie.html");

        var movie = await JavBusParser.ParseMoviePageAsync(html, BaseUrl, "SSIS-001", "JavBus", default);

        Assert.NotNull(movie);
        Assert.Equal(new DateTime(2024, 1, 15), movie!.ReleaseDate);
        Assert.Equal(120, movie.RuntimeMinutes);
        Assert.Equal("Test Director", movie.Director);
        Assert.Equal("S1 NO.1 STYLE", movie.Studio);
        Assert.Equal("S1", movie.Label);
        Assert.Equal("Test Series", movie.Series);
    }

    [Fact]
    public async Task ParseMoviePage_ExtractsGenres()
    {
        var html = LoadFixture("javbus_movie.html");

        var movie = await JavBusParser.ParseMoviePageAsync(html, BaseUrl, "SSIS-001", "JavBus", default);

        Assert.NotNull(movie);
        Assert.Equal(["Drama", "Romance"], movie!.Genres);
    }

    [Fact]
    public async Task ParseMoviePage_ExtractsActors()
    {
        var html = LoadFixture("javbus_movie.html");

        var movie = await JavBusParser.ParseMoviePageAsync(html, BaseUrl, "SSIS-001", "JavBus", default);

        Assert.NotNull(movie);
        Assert.Single(movie!.Actors);
        Assert.Equal("Test Actress 日本語名", movie.Actors[0].Name);
        Assert.Equal("https://cdn.javbus.com/pics/actress/abc_b.jpg", movie.Actors[0].ImageUrl);
    }

    [Fact]
    public async Task ParseMoviePage_BackdropMirrorsCoverUrl()
    {
        // JavBus only ships one cover image per movie; we reuse it for the
        // backdrop so detail pages aren't blank.
        var html = LoadFixture("javbus_movie.html");

        var movie = await JavBusParser.ParseMoviePageAsync(html, BaseUrl, "SSIS-001", "JavBus", default);

        Assert.NotNull(movie);
        Assert.Equal(movie!.CoverUrl, movie.BackdropUrl);
        Assert.Equal("https://www.javbus.com/pics/cover/ssis-001_b.jpg", movie.CoverUrl);
    }

    [Fact]
    public async Task ParseMoviePage_LowercaseSourceIdNormalizedToUpperCode()
    {
        var html = LoadFixture("javbus_movie.html");

        var movie = await JavBusParser.ParseMoviePageAsync(html, BaseUrl, "ssis-001", "JavBus", default);

        Assert.NotNull(movie);
        Assert.Equal("SSIS-001", movie!.Code);
    }

    [Fact]
    public async Task ParseMoviePage_NoContainer_ReturnsNull()
    {
        // Pages that don't have .container are 404 redirects, Cloudflare interstitials, etc.
        var html = "<html><body><div>nothing here</div></body></html>";

        var movie = await JavBusParser.ParseMoviePageAsync(html, BaseUrl, "SSIS-001", "JavBus", default);

        Assert.Null(movie);
    }

    // --- Actor search ---

    [Fact]
    public async Task ParseActorSearchResult_ReturnsFirstMatch()
    {
        var html = LoadFixture("javbus_actor_search.html");

        var actor = await JavBusParser.ParseActorSearchResultAsync(html, BaseUrl, "fallback", default);

        Assert.NotNull(actor);
        Assert.Equal("Test Actress", actor!.Name);
        Assert.Equal("https://www.javbus.com/pics/actress/test_b.jpg", actor.ImageUrl);
    }

    [Fact]
    public async Task ParseActorSearchResult_NoResults_ReturnsNull()
    {
        var html = "<html><body></body></html>";

        var actor = await JavBusParser.ParseActorSearchResultAsync(html, BaseUrl, "fallback", default);

        Assert.Null(actor);
    }

    // --- MakeAbsolute (covered indirectly above; one explicit case for the auth scheme) ---

    [Fact]
    public async Task ParseSearchResults_AbsoluteHttpsUrlPreserved()
    {
        var html = """
            <html><body><div id="waterfall">
              <div class="item">
                <a href="https://www.javbus.com/ABS-001">
                  <div class="photo-frame">
                    <img src="https://example.com/already-absolute.jpg" title="Absolute test" />
                  </div>
                </a>
                <date>ABS-001</date>
                <date>2024-03-01</date>
              </div>
            </div></body></html>
            """;

        var results = await JavBusParser.ParseSearchResultsAsync(html, BaseUrl, "JavBus", default);

        Assert.Single(results);
        Assert.Equal("https://example.com/already-absolute.jpg", results[0].ThumbnailUrl);
    }
}
