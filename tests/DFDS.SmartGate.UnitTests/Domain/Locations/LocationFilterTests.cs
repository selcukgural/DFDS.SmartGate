using System;
using DFDS.SmartGate.Domain.Locations;
using Xunit;

namespace DFDS.SmartGate.UnitTests.Domain.Locations;

public sealed class LocationFilterTests
{
    [Fact]
    public void Create_WithTwoLetters_ProducesCountryFilter()
    {
        var result = LocationFilter.Create("tr");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsCountry);
        Assert.Equal("TR", result.Value.Country!.Value.Value);
        Assert.Null(result.Value.Location);
    }

    [Fact]
    public void Create_WithFiveCharacters_ProducesLocationFilter()
    {
        var result = LocationFilter.Create(" trist ");

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsCountry);
        Assert.Equal("TRIST", result.Value.Location!.Value.Value);
        Assert.Null(result.Value.Country);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("TRI")]
    [InlineData("TRISTX")]
    [InlineData("T1")]
    [InlineData("TRIS1")]
    public void Create_FailsWithSingleFilterError_ForAnythingElse(string? raw)
    {
        var result = LocationFilter.Create(raw, "movementTo");

        Assert.True(result.IsFailure);
        Assert.Equal("LocationFilter.Invalid", result.Error.Value.Code);
        Assert.Contains("movementTo", result.Error.Value.Message, StringComparison.Ordinal);
    }
}
