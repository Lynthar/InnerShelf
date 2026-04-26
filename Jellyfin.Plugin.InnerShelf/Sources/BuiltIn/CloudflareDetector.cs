namespace Jellyfin.Plugin.InnerShelf.Sources.BuiltIn;

/// <summary>
/// Detects whether an HTTP 200 response body is actually a Cloudflare
/// interstitial / challenge page rather than the upstream content the caller
/// expected. Without this check, scrapers see a 200 OK with an HTML body and
/// happily try to parse it — finding none of the expected selectors and
/// silently returning empty results, indistinguishable from a legitimate
/// "no match" answer. Pure (no I/O, no statics) so it can be unit-tested
/// against fixture HTML.
/// </summary>
public static class CloudflareDetector
{
    // Markers chosen to be highly specific to Cloudflare's challenge platform.
    // Any match is treated as a hit (OR semantics): false positives on real
    // upstream content are extremely unlikely because these strings reference
    // CF-internal constructs, while false negatives (CF restructures their
    // page) only degrade us back to current "silent empty" behaviour, not worse.
    private static readonly string[] InterstitialMarkers =
    [
        "<title>Just a moment...</title>",
        "challenge-platform",
        "cf-mitigated",
        "cf-browser-verification",
        "__cf_chl_",
    ];

    /// <summary>
    /// Returns true if <paramref name="html"/> looks like a Cloudflare
    /// interstitial / challenge page. Empty or null input returns false.
    /// </summary>
    public static bool IsCloudflareInterstitial(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return false;
        }

        foreach (var marker in InterstitialMarkers)
        {
            if (html.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
