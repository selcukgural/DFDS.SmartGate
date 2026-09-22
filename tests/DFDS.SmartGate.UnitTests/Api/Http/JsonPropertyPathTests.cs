using DFDS.SmartGate.Api.Http;

namespace DFDS.SmartGate.UnitTests.Api.Http;

public sealed class JsonPropertyPathTests
{
    [Theory]
    [InlineData("TerminalId", "terminalId")]
    [InlineData("Truck.UnitNumber", "truck.unitNumber")]
    [InlineData("Movements[0].Location", "movements[0].location")]
    [InlineData("alreadyCamel.value", "alreadyCamel.value")]
    [InlineData("", "")]
    public void ToCamelCase_LowersFirstLetterOfEachSegment(string input, string expected)
    {
        Assert.Equal(expected, JsonPropertyPath.ToCamelCase(input));
    }

    [Fact]
    public void ToCamelCase_ReturnsSameInstanceWhenNothingChanges()
    {
        const string path = "truck.unitNumber";

        Assert.Same(path, JsonPropertyPath.ToCamelCase(path));
    }
}
