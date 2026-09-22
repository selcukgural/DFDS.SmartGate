using DFDS.SmartGate.Application.Visits;
using DFDS.SmartGate.Domain.Visits;

namespace DFDS.SmartGate.UnitTests.Application.Visits;

public sealed class VisitStatusNamesTests
{
    [Theory]
    [InlineData("PreRegistered", VisitStatus.PreRegistered)]
    [InlineData("atgate", VisitStatus.AtGate)]
    [InlineData("ONSITE", VisitStatus.OnSite)]
    [InlineData("  completed ", VisitStatus.Completed)]
    public void TryParse_AcceptsNamesInAnyCasing(string raw, VisitStatus expected)
    {
        Assert.True(VisitStatusNames.TryParse(raw, out var status));
        Assert.Equal(expected, status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Rejected")]
    [InlineData("2")]
    [InlineData("-1")]
    [InlineData("At Gate")]
    public void TryParse_RejectsUnknownAndNumericValues(string? raw)
    {
        Assert.False(VisitStatusNames.TryParse(raw, out _));
    }

    [Fact]
    public void Parse_ThrowsForInvalidInput()
    {
        Assert.Throws<ArgumentException>(() => VisitStatusNames.Parse("Bogus"));
    }

    [Fact]
    public void AllowedValues_ListsEveryStatus()
    {
        Assert.Equal("PreRegistered, AtGate, OnSite, Completed", VisitStatusNames.AllowedValues);
    }
}
