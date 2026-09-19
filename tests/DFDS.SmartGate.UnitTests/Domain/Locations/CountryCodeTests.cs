using System;
using DFDS.SmartGate.Domain.Locations;
using Xunit;

namespace DFDS.SmartGate.UnitTests.Domain.Locations;

public sealed class CountryCodeTests
{
    [Theory]
    [InlineData("dk", "DK")]
    [InlineData(" TR ", "TR")]
    public void Create_AcceptsTwoAsciiLetters(string raw, string expected)
    {
        var result = CountryCode.Create(raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("D")]
    [InlineData("DKK")]
    [InlineData("D1")]
    [InlineData("İT")]
    public void Create_FailsWithInvalid_ForBadFormat(string? raw)
    {
        var result = CountryCode.Create(raw, "movementFrom");

        Assert.True(result.IsFailure);
        Assert.Equal("CountryCode.Invalid", result.Error.Value.Code);
        Assert.Contains("movementFrom", result.Error.Value.Message, StringComparison.Ordinal);
    }
}
