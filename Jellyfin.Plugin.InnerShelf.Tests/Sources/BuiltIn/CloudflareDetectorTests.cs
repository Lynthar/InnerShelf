using Jellyfin.Plugin.InnerShelf.Sources.BuiltIn;
using Xunit;

namespace Jellyfin.Plugin.InnerShelf.Tests.Sources.BuiltIn;

public class CloudflareDetectorTests
{
    private static string LoadFixture(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Sources", "BuiltIn", "Fixtures", name);
        return File.ReadAllText(path);
    }

    [Fact]
    public void NullInput_ReturnsFalse()
    {
        Assert.False(CloudflareDetector.IsCloudflareInterstitial(null));
    }

    [Fact]
    public void EmptyInput_ReturnsFalse()
    {
        Assert.False(CloudflareDetector.IsCloudflareInterstitial(string.Empty));
    }

    [Fact]
    public void RealJavBusSearchFixture_ReturnsFalse()
    {
        // Reuses the existing parser fixture — verifies we don't false-positive on real JavBus.
        var html = LoadFixture("javbus_search.html");

        Assert.False(CloudflareDetector.IsCloudflareInterstitial(html));
    }

    [Fact]
    public void RealJavBusMovieFixture_ReturnsFalse()
    {
        var html = LoadFixture("javbus_movie.html");

        Assert.False(CloudflareDetector.IsCloudflareInterstitial(html));
    }

    [Theory]
    [InlineData("<html><head><title>Just a moment...</title></head><body>...</body></html>")]
    [InlineData("<!doctype html><html><body><script src='/cdn-cgi/challenge-platform/h/b/orchestrate/jsch/v1'></script></body></html>")]
    [InlineData("<html><head><meta name='cf-mitigated' content='challenge'></head></html>")]
    [InlineData("<html><body><div class='cf-browser-verification'></div></body></html>")]
    [InlineData("<html><body>__cf_chl_opt_xxx</body></html>")]
    public void CloudflareMarkers_ReturnTrue(string html)
    {
        Assert.True(CloudflareDetector.IsCloudflareInterstitial(html));
    }

    [Fact]
    public void MarkerMatchIsCaseInsensitive()
    {
        var html = "<html><head><title>JUST A MOMENT...</title></head></html>";

        Assert.True(CloudflareDetector.IsCloudflareInterstitial(html));
    }

    [Fact]
    public void BenignHtmlWithoutMarkers_ReturnsFalse()
    {
        var html = "<html><body><h1>Hello</h1><p>This page mentions challenges and platforms but isn't Cloudflare.</p></body></html>";

        Assert.False(CloudflareDetector.IsCloudflareInterstitial(html));
    }
}
