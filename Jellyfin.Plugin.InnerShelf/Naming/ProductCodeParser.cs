using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.InnerShelf.Naming;

/// <summary>
/// Parses JAV product codes from filenames.
/// </summary>
public static partial class ProductCodeParser
{
    // Noise patterns to strip before matching
    private static readonly Regex NoisePattern = NoiseRegex();

    // Chinese subtitle suffix detection
    private static readonly Regex ChineseSubPattern = ChineseSubRegex();

    // Multi-disc suffix detection
    private static readonly Regex DiscPattern = DiscRegex();

    // Ordered matching patterns (most specific first)
    private static readonly (Regex Pattern, CodeCategory Category)[] Matchers =
    [
        (HeyzoRegex(), CodeCategory.Heyzo),
        (HeydougaRegex(), CodeCategory.Heydouga),
        (Fc2Regex(), CodeCategory.FC2),
        (AmateurRegex(), CodeCategory.Amateur),
        (CensoredRegex(), CodeCategory.Censored),
        (UncensoredRegex(), CodeCategory.Uncensored),
    ];

    /// <summary>
    /// Attempts to parse a product code from a filename.
    /// </summary>
    /// <param name="filename">The filename (with or without extension).</param>
    /// <returns>A parsed <see cref="ProductCode"/>, or null if no code was found.</returns>
    public static ProductCode? Parse(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return null;
        }

        // Strip file extension
        var name = Path.GetFileNameWithoutExtension(filename);

        // Detect and strip Chinese subtitle suffix
        var chineseSubMatch = ChineseSubPattern.Match(name);
        bool hasChineseSub = chineseSubMatch.Success;
        if (hasChineseSub)
        {
            name = name[..chineseSubMatch.Index] + name[(chineseSubMatch.Index + chineseSubMatch.Length)..];
        }

        // Detect and strip multi-disc suffix
        var discMatch = DiscPattern.Match(name);
        int? discNumber = null;
        if (discMatch.Success)
        {
            discNumber = int.Parse(discMatch.Groups[1].Value);
            name = name[..discMatch.Index] + name[(discMatch.Index + discMatch.Length)..];
        }

        // Strip noise (resolution, codec, release group, etc.)
        name = NoisePattern.Replace(name, " ");

        // Try each pattern in priority order
        foreach (var (pattern, category) in Matchers)
        {
            var match = pattern.Match(name);
            if (match.Success)
            {
                var raw = match.Value;
                var normalized = Normalize(raw, category);
                return new ProductCode(raw, normalized, category, hasChineseSub, discNumber);
            }
        }

        return null;
    }

    private static string Normalize(string raw, CodeCategory category)
    {
        var upper = raw.ToUpperInvariant();

        return category switch
        {
            CodeCategory.FC2 => NormalizeFc2(upper),
            CodeCategory.Heydouga => upper.Replace("_", "-"),
            CodeCategory.Uncensored => upper.Replace("_", "-"),
            _ => NormalizeDash(upper),
        };
    }

    private static string NormalizeFc2(string code)
    {
        // Normalize all FC2 variants to "FC2-PPV-NNNNNNN"
        var digits = Fc2DigitsRegex().Match(code);
        return digits.Success ? $"FC2-PPV-{digits.Groups[1].Value}" : code;
    }

    private static string NormalizeDash(string code)
    {
        // Ensure there's a dash between prefix and number for standard codes
        // e.g., "SSIS001" -> "SSIS-001"
        var noDash = NoDashRegex().Match(code);
        if (noDash.Success)
        {
            return $"{noDash.Groups[1].Value}-{noDash.Groups[2].Value}";
        }

        return code;
    }

    // --- Generated Regex patterns ---

    [GeneratedRegex(@"[\[\(].*?[\]\)]|1080[pi]|720[pi]|480[pi]|4K|2160[pi]|[xXhH]\.?26[45]|HEVC|AVC|AAC|MP4|MKV|AVI|WMV", RegexOptions.IgnoreCase)]
    private static partial Regex NoiseRegex();

    [GeneratedRegex(@"[-_](?:C|ch|CH|chinese|中文字幕)(?=[-_.\s]|$)", RegexOptions.IgnoreCase)]
    private static partial Regex ChineseSubRegex();

    [GeneratedRegex(@"[-_]cd(\d)(?=[-_.\s]|$)", RegexOptions.IgnoreCase)]
    private static partial Regex DiscRegex();

    [GeneratedRegex(@"HEYZO[-_](\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex HeyzoRegex();

    [GeneratedRegex(@"heydouga[-_]\d{4}[-_]\d{3,4}", RegexOptions.IgnoreCase)]
    private static partial Regex HeydougaRegex();

    [GeneratedRegex(@"FC2[-_]?PPV[-_]?(\d{5,7})", RegexOptions.IgnoreCase)]
    private static partial Regex Fc2Regex();

    [GeneratedRegex(@"\d{3}[A-Z]{2,5}[-_]\d{3,5}", RegexOptions.IgnoreCase)]
    private static partial Regex AmateurRegex();

    [GeneratedRegex(@"[A-Z]{2,10}[-_]?\d{3,5}", RegexOptions.IgnoreCase)]
    private static partial Regex CensoredRegex();

    [GeneratedRegex(@"\d{6}[-_]\d{2,3}", RegexOptions.None)]
    private static partial Regex UncensoredRegex();

    [GeneratedRegex(@"(\d+)$")]
    private static partial Regex Fc2DigitsRegex();

    [GeneratedRegex(@"^([A-Z]{2,10})(\d{3,5})$")]
    private static partial Regex NoDashRegex();
}
