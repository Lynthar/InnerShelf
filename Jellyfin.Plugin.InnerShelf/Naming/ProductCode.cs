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
/// Parsed JAV product code extracted from a filename.
/// </summary>
/// <param name="Raw">The raw matched string from the filename.</param>
/// <param name="Normalized">Uppercased, canonical form of the code.</param>
/// <param name="Category">The category of this product code.</param>
/// <param name="HasChineseSub">Whether a Chinese subtitle suffix was detected.</param>
/// <param name="DiscNumber">Multi-disc indicator, if detected.</param>
public record ProductCode(
    string Raw,
    string Normalized,
    CodeCategory Category,
    bool HasChineseSub,
    int? DiscNumber);
