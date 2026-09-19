using DFDS.SmartGate.Domain.Identifiers;
using Xunit;

namespace DFDS.SmartGate.UnitTests.Domain.Identifiers;

public sealed class IdentifierNormalizerTests
{
    [Theory]
    [InlineData("34 ABC 123", "34ABC123")]
    [InlineData("  tr-34-abc  ", "TR-34-ABC")]
    [InlineData("msku 123 4567", "MSKU1234567")]
    [InlineData("ab\tcd\r\nef", "ABCDEF")]
    public void Normalize_RemovesAllWhitespaceAndUpperCases(string raw, string expected)
    {
        Assert.Equal(expected, IdentifierNormalizer.Normalize(raw));
    }

    [Fact]
    public void Normalize_RemovesIdeographicSpace()
    {
        // U+3000 is the full-width space commonly typed on Japanese keyboards.
        Assert.Equal("品川500", IdentifierNormalizer.Normalize("品川　500"));
    }

    [Fact]
    public void Normalize_FoldsFullWidthLatinToAscii()
    {
        Assert.Equal("ABC123", IdentifierNormalizer.Normalize("ＡＢＣ１２３"));
    }

    [Fact]
    public void Normalize_PreservesNonLatinLetters()
    {
        Assert.Equal("京A12345", IdentifierNormalizer.Normalize("京A 12345"));
    }

    [Fact]
    public void Normalize_UsesInvariantCasing_ForTurkishDotlessI()
    {
        // In tr-TR culture "i".ToUpper() is "İ"; the invariant culture must be used so lookups are stable.
        Assert.Equal("ISTANBUL", IdentifierNormalizer.Normalize("istanbul"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("　")]
    public void Normalize_ReturnsEmpty_ForBlankInput(string? raw)
    {
        Assert.Equal(string.Empty, IdentifierNormalizer.Normalize(raw));
    }

    [Fact]
    public void Normalize_ReturnsSameInstance_WhenAlreadyCanonical()
    {
        const string canonical = "MSKU1234567";
        Assert.Same(canonical, IdentifierNormalizer.Normalize(canonical));
    }

    [Theory]
    [InlineData("MSKU1234567", true)]
    [InlineData("TR-34-ABC", true)]
    [InlineData("京A12345", true)]
    [InlineData("", false)]
    [InlineData("AB C", false)]
    [InlineData("AB/C", false)]
    [InlineData("AB.C", false)]
    [InlineData("AB😀", false)]
    public void IsValid_AllowsOnlyLettersDigitsAndHyphen(string normalized, bool expected)
    {
        Assert.Equal(expected, IdentifierNormalizer.IsValid(normalized));
    }
}
