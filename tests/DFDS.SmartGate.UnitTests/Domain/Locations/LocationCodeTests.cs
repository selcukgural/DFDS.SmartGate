using System;
using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.Domain.Locations;
using Xunit;

namespace DFDS.SmartGate.UnitTests.Domain.Locations;

public sealed class LocationCodeTests
{
    [Theory]
    [InlineData("DKCPH", "DKCPH", "DK")]
    [InlineData("trist", "TRIST", "TR")]
    [InlineData("  segot ", "SEGOT", "SE")]
    [InlineData("US2NY", "US2NY", "US")]
    public void Create_AcceptsValidUnLocode_AndDerivesCountry(string raw, string expectedCode, string expectedCountry)
    {
        var result = LocationCode.Create(raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedCode, result.Value.Value);
        Assert.Equal(expectedCountry, result.Value.Country.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_FailsWithEmpty_ForBlankInput(string? raw)
    {
        var result = LocationCode.Create(raw, "terminalId");

        Assert.True(result.IsFailure);
        Assert.Equal("LocationCode.Empty", result.Error.Value.Code);
        Assert.Contains("terminalId", result.Error.Value.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("DK")]        // too short
    [InlineData("DKCPHX")]    // too long
    [InlineData("D1CPH")]     // digit in country part
    [InlineData("DKCP1")]     // 0/1 are not allowed in the location part
    [InlineData("DKC-H")]
    [InlineData("İSTAN")]     // non-ASCII letter
    [InlineData("DK CP")]
    public void Create_FailsWithInvalid_ForBadFormat(string raw)
    {
        var result = LocationCode.Create(raw);

        Assert.True(result.IsFailure);
        Assert.Equal("LocationCode.Invalid", result.Error.Value.Code);
        Assert.Equal(ErrorKind.Validation, result.Error.Value.Kind);
    }

    [Fact]
    public void Codes_WithSameValue_AreEqual()
    {
        Assert.Equal(LocationCode.Create("dkcph").Value, LocationCode.Create("DKCPH").Value);
    }
}
