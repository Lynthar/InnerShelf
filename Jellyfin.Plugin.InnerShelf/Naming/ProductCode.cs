namespace Jellyfin.Plugin.InnerShelf.Naming;

/// <summary>
/// Category of JAV product code.
/// </summary>
public enum CodeCategory
{
    /// <summary>Standard censored content (e.g. SSIS-001).</summary>
    Censored,

    /// <summary>Amateur/素人 content (e.g. 390JAC-132).</summary>
    Amateur,

    /// <summary>FC2 PPV content (e.g. FC2-PPV-1234567).</summary>
    FC2,

    /// <summary>Uncensored content with date-based codes (e.g. 010120-001).</summary>
    Uncensored,

    /// <summary>HEYZO content (e.g. HEYZO-1234).</summary>
    Heyzo,

    /// <summary>Heydouga content (e.g. heydouga-1234-321).</summary>
    Heydouga,
}

/// <summary>
/// Version markers extracted from the filename (post-production edits,
/// leaked variants, remasters, revisions, etc). Multiple flags can be set.
/// </summary>
[System.Flags]
public enum VersionFlags
{
    /// <summary>No version markers detected.</summary>
    None = 0,

    /// <summary>Chinese subtitle (hardcoded), e.g. `-C`, `-ch`, `-中文字幕`.</summary>
    ChineseSub = 1 << 0,

    /// <summary>Uncensored leak / decrypted, e.g. `-U`, `-UC`, `-uncen`.</summary>
    Uncensored = 1 << 1,

    /// <summary>Decrypted / hack edition, e.g. `-HACK`, `-hacked`.</summary>
    Hack = 1 << 2,

    /// <summary>HD remaster edition, e.g. `-HD` (dash-prefixed only).</summary>
    HdRemaster = 1 << 3,

    /// <summary>4K remaster edition, e.g. `-4K` (dash-prefixed only).</summary>
    FourK = 1 << 4,

    /// <summary>Revised version (v2, fix, -2, etc).</summary>
    Revision = 1 << 5,
}

/// <summary>
/// Parsed JAV product code extracted from a filename.
/// </summary>
/// <param name="Raw">The raw matched string from the filename.</param>
/// <param name="Normalized">Uppercased, canonical form of the code.</param>
/// <param name="Category">The category of this product code.</param>
/// <param name="Versions">Version markers detected in the filename.</param>
/// <param name="DiscNumber">Multi-disc indicator, if detected.</param>
public record ProductCode(
    string Raw,
    string Normalized,
    CodeCategory Category,
    VersionFlags Versions,
    int? DiscNumber)
{
    /// <summary>Gets a value indicating whether this is a Chinese subtitle edition.</summary>
    public bool HasChineseSub => Versions.HasFlag(VersionFlags.ChineseSub);
}
