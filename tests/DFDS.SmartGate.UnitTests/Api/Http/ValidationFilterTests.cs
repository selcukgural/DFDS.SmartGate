using DFDS.SmartGate.Api.Http;
using FluentValidation.Results;

namespace DFDS.SmartGate.UnitTests.Api.Http;

public sealed class ValidationFilterTests
{
    [Fact]
    public void ToErrors_GroupsMessagesByCamelCasePath()
    {
        var errors = ValidationFilter.ToErrors(
        [
            new ValidationFailure("Truck.UnitNumber", "required"),
            new ValidationFailure("Truck.UnitNumber", "too long"),
            new ValidationFailure("Movements[1].Location", "invalid"),
        ]);

        Assert.Equal(2, errors.Count);
        Assert.Equal(["required", "too long"], errors["truck.unitNumber"]);
        Assert.Equal(["invalid"], errors["movements[1].location"]);
    }

    [Fact]
    public void IndexOfParameter_FindsTheTypedParameter()
    {
        var method = typeof(ValidationFilterTests).GetMethod(nameof(SampleHandler), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        Assert.Equal(1, ValidationFilter.IndexOfParameter<string>(method));
        Assert.Throws<InvalidOperationException>(() => ValidationFilter.IndexOfParameter<double>(method));
    }

    private static void SampleHandler(int first, string second)
    {
        _ = first;
        _ = second;
    }
}
