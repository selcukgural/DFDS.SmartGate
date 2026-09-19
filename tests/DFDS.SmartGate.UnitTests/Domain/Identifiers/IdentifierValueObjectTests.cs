using System.Linq;
using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.Domain.Identifiers;
using Xunit;

namespace DFDS.SmartGate.UnitTests.Domain.Identifiers;

public sealed class IdentifierValueObjectTests
{
    [Fact]
    public void LicensePlate_Create_NormalisesInput()
    {
        var result = LicensePlate.Create(" 34 abc 123 ");

        Assert.True(result.IsSuccess);
        Assert.Equal("34ABC123", result.Value.Value);
    }

    [Fact]
    public void UnitNumber_Create_NormalisesInput()
    {
        var result = UnitNumber.Create("msku 123 4567");

        Assert.True(result.IsSuccess);
        Assert.Equal("MSKU1234567", result.Value.Value);
    }

    [Fact]
    public void DriverLicenseNumber_Create_NormalisesInput()
    {
        var result = DriverLicenseNumber.Create("dl 99 88");

        Assert.True(result.IsSuccess);
        Assert.Equal("DL9988", result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_FailsWithEmptyError_ForBlankInput(string? raw)
    {
        AssertFailure(LicensePlate.Create(raw), "LicensePlate.Empty");
        AssertFailure(UnitNumber.Create(raw), "UnitNumber.Empty");
        AssertFailure(DriverLicenseNumber.Create(raw), "DriverLicenseNumber.Empty");
    }

    [Fact]
    public void Create_FailsWithTooLongError_WhenNormalisedValueExceedsMax()
    {
        var tooLong = new string('A', LicensePlate.MaxLength + 1);

        AssertFailure(LicensePlate.Create(tooLong), "LicensePlate.TooLong");
        AssertFailure(UnitNumber.Create(new string('A', UnitNumber.MaxLength + 1)), "UnitNumber.TooLong");
        AssertFailure(DriverLicenseNumber.Create(new string('A', DriverLicenseNumber.MaxLength + 1)), "DriverLicenseNumber.TooLong");
    }

    [Fact]
    public void Create_MeasuresLengthAfterNormalisation()
    {
        // 20 letters plus interior spaces: the spaces are removed before the length check.
        var raw = string.Join(' ', Enumerable.Repeat("AB", LicensePlate.MaxLength / 2));

        Assert.True(LicensePlate.Create(raw).IsSuccess);
    }

    [Theory]
    [InlineData("34/ABC")]
    [InlineData("34.ABC")]
    [InlineData("34ABC!")]
    public void Create_FailsWithInvalidCharactersError(string raw)
    {
        AssertFailure(LicensePlate.Create(raw), "LicensePlate.InvalidCharacters");
        AssertFailure(UnitNumber.Create(raw), "UnitNumber.InvalidCharacters");
    }

    [Fact]
    public void ValueObjects_WithSameNormalisedValue_AreEqual()
    {
        Assert.Equal(LicensePlate.Create("34 abc 123").Value, LicensePlate.Create("34ABC123").Value);
    }

    private static void AssertFailure<T>(Result<T> result, string expectedCode)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Value.Code);
        Assert.Equal(ErrorKind.Validation, result.Error.Value.Kind);
    }
}
