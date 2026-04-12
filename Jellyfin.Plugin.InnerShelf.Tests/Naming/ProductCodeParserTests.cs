using Jellyfin.Plugin.InnerShelf.Naming;
using Xunit;

namespace Jellyfin.Plugin.InnerShelf.Tests.Naming;

public class ProductCodeParserTests
{
    [Theory]
    [InlineData("SSIS-001.mp4", "SSIS-001", CodeCategory.Censored)]
    [InlineData("ABP-100 Title Here.mkv", "ABP-100", CodeCategory.Censored)]
    [InlineData("PPPC-326.1080p.mp4", "PPPC-326", CodeCategory.Censored)]
    [InlineData("[Group] STARS-123 Some Title [1080p].mp4", "STARS-123", CodeCategory.Censored)]
    [InlineData("MIDV-001 4K x265.mkv", "MIDV-001", CodeCategory.Censored)]
    public void Parse_StandardCensored_ReturnsCorrectCode(string filename, string expectedCode, CodeCategory expectedCategory)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Normalized);
        Assert.Equal(expectedCategory, result.Category);
    }

    [Theory]
    [InlineData("390JAC-132.mp4", "390JAC-132", CodeCategory.Amateur)]
    [InlineData("300MAAN-783 Title.mkv", "300MAAN-783", CodeCategory.Amateur)]
    [InlineData("200GANA-2345.mp4", "200GANA-2345", CodeCategory.Amateur)]
    public void Parse_Amateur_ReturnsCorrectCode(string filename, string expectedCode, CodeCategory expectedCategory)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Normalized);
        Assert.Equal(expectedCategory, result.Category);
    }

    [Theory]
    [InlineData("FC2-PPV-1234567.mp4", "FC2-PPV-1234567", CodeCategory.FC2)]
    [InlineData("FC2PPV1234567.mkv", "FC2-PPV-1234567", CodeCategory.FC2)]
    [InlineData("FC2_PPV_1234567.mp4", "FC2-PPV-1234567", CodeCategory.FC2)]
    [InlineData("fc2-ppv-9876543.avi", "FC2-PPV-9876543", CodeCategory.FC2)]
    public void Parse_FC2_ReturnsNormalizedCode(string filename, string expectedCode, CodeCategory expectedCategory)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Normalized);
        Assert.Equal(expectedCategory, result.Category);
    }

    [Theory]
    [InlineData("HEYZO-1234.mp4", "HEYZO-1234", CodeCategory.Heyzo)]
    [InlineData("HEYZO_5678.mkv", "HEYZO-5678", CodeCategory.Heyzo)]
    public void Parse_Heyzo_ReturnsCorrectCode(string filename, string expectedCode, CodeCategory expectedCategory)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Normalized);
        Assert.Equal(expectedCategory, result.Category);
    }

    [Theory]
    [InlineData("heydouga-1234-321.mp4", "HEYDOUGA-1234-321", CodeCategory.Heydouga)]
    [InlineData("heydouga_4567_890.mkv", "HEYDOUGA-4567-890", CodeCategory.Heydouga)]
    public void Parse_Heydouga_ReturnsCorrectCode(string filename, string expectedCode, CodeCategory expectedCategory)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Normalized);
        Assert.Equal(expectedCategory, result.Category);
    }

    [Theory]
    [InlineData("010120-001.mp4", "010120-001", CodeCategory.Uncensored)]
    [InlineData("123456_999.mkv", "123456-999", CodeCategory.Uncensored)]
    public void Parse_Uncensored_ReturnsCorrectCode(string filename, string expectedCode, CodeCategory expectedCategory)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Normalized);
        Assert.Equal(expectedCategory, result.Category);
    }

    [Theory]
    [InlineData("SSIS-001-C.mp4", "SSIS-001", VersionFlags.ChineseSub)]
    [InlineData("ABP-100-ch.mkv", "ABP-100", VersionFlags.ChineseSub)]
    [InlineData("STARS-456-chinese.mp4", "STARS-456", VersionFlags.ChineseSub)]
    [InlineData("ATID-576-C.mp4", "ATID-576", VersionFlags.ChineseSub)]
    [InlineData("ATID-576-ch.mkv", "ATID-576", VersionFlags.ChineseSub)]
    [InlineData("MIDV-001.mp4", "MIDV-001", VersionFlags.None)]
    [InlineData("SSIS-001-C2.mp4", "SSIS-001", VersionFlags.ChineseSub | VersionFlags.Revision)]
    public void Parse_DetectsChineseSubtitleSuffix(string filename, string expectedCode, VersionFlags expectedFlags)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Normalized);
        Assert.Equal(expectedFlags, result.Versions);
    }

    [Theory]
    [InlineData("SSIS-001-U.mp4", "SSIS-001", VersionFlags.Uncensored)]
    [InlineData("SSIS-001-UC.mp4", "SSIS-001", VersionFlags.Uncensored)]
    [InlineData("SSIS-001-uncen.mp4", "SSIS-001", VersionFlags.Uncensored)]
    [InlineData("SSIS-001-uncensored.mp4", "SSIS-001", VersionFlags.Uncensored)]
    public void Parse_DetectsUncensoredLeakSuffix(string filename, string expectedCode, VersionFlags expectedFlags)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Normalized);
        Assert.Equal(expectedFlags, result.Versions);
    }

    [Theory]
    [InlineData("SSIS-001-HACK.mp4", "SSIS-001", VersionFlags.Hack)]
    [InlineData("SSIS-001-hacked.mp4", "SSIS-001", VersionFlags.Hack)]
    [InlineData("SSIS-001-decrypted.mp4", "SSIS-001", VersionFlags.Hack)]
    public void Parse_DetectsHackSuffix(string filename, string expectedCode, VersionFlags expectedFlags)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Normalized);
        Assert.Equal(expectedFlags, result.Versions);
    }

    [Theory]
    [InlineData("SSIS-001-HD.mp4", "SSIS-001", VersionFlags.HdRemaster)]
    [InlineData("SSIS-001-hd.mkv", "SSIS-001", VersionFlags.HdRemaster)]
    public void Parse_DetectsHdRemasterSuffix(string filename, string expectedCode, VersionFlags expectedFlags)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Normalized);
        Assert.Equal(expectedFlags, result.Versions);
    }

    [Theory]
    [InlineData("SSIS-001-4K.mp4", "SSIS-001", VersionFlags.FourK)]
    [InlineData("SSIS-001-4k.mkv", "SSIS-001", VersionFlags.FourK)]
    public void Parse_Detects4KSuffix(string filename, string expectedCode, VersionFlags expectedFlags)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Normalized);
        Assert.Equal(expectedFlags, result.Versions);
    }

    [Theory]
    [InlineData("SSIS-001-v2.mp4", "SSIS-001", VersionFlags.Revision)]
    [InlineData("SSIS-001-fix.mp4", "SSIS-001", VersionFlags.Revision)]
    [InlineData("SSIS-001-2.mp4", "SSIS-001", VersionFlags.Revision)]
    public void Parse_DetectsRevisionSuffix(string filename, string expectedCode, VersionFlags expectedFlags)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Normalized);
        Assert.Equal(expectedFlags, result.Versions);
    }

    [Theory]
    [InlineData("SSIS-001-C-HD.mp4", VersionFlags.ChineseSub | VersionFlags.HdRemaster)]
    [InlineData("ATID-576-HD-C.mp4", VersionFlags.ChineseSub | VersionFlags.HdRemaster)]
    [InlineData("SSIS-001-U-HACK.mp4", VersionFlags.Uncensored | VersionFlags.Hack)]
    [InlineData("SSIS-001-C-4K.mp4", VersionFlags.ChineseSub | VersionFlags.FourK)]
    public void Parse_DetectsCombinedVersionMarkers(string filename, VersionFlags expectedFlags)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedFlags, result.Versions);
    }

    [Theory]
    [InlineData("SSIS-001-cd1.mp4", 1)]
    [InlineData("ABP-100-cd2.mkv", 2)]
    [InlineData("MIDV-001.mp4", null)]
    public void Parse_DetectsDiscNumber(string filename, int? expectedDisc)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedDisc, result.DiscNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("random-text.txt")]
    [InlineData("photo_001.jpg")]
    public void Parse_NoMatch_ReturnsNull(string filename)
    {
        var result = ProductCodeParser.Parse(filename);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_NullInput_ReturnsNull()
    {
        var result = ProductCodeParser.Parse(null!);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("SSIS001.mp4", "SSIS-001")]
    public void Parse_NoDash_NormalizesWithDash(string filename, string expectedCode)
    {
        var result = ProductCodeParser.Parse(filename);

        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Normalized);
    }
}
