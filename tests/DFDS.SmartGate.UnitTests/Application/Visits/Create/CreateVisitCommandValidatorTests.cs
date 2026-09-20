using DFDS.SmartGate.Application.Visits.Create;
using DFDS.SmartGate.Domain.Visits;
using FluentValidation.TestHelper;
using Xunit;
using static DFDS.SmartGate.UnitTests.Application.ApplicationTestData;

namespace DFDS.SmartGate.UnitTests.Application.Visits.Create;

public sealed class CreateVisitCommandValidatorTests
{
    private readonly CreateVisitCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(ValidCreateCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("DK")]
    [InlineData("DKCP1")]
    public void InvalidTerminal_IsReported_WithDomainMessage(string? terminalId)
    {
        var result = _validator.TestValidate(ValidCreateCommand() with { TerminalId = terminalId });

        result.ShouldHaveValidationErrorFor(x => x.TerminalId);
    }

    [Fact]
    public void MissingTruckAndDriver_AreReported()
    {
        var result = _validator.TestValidate(ValidCreateCommand() with { Truck = null, Driver = null });

        result.ShouldHaveValidationErrorFor(x => x.Truck);
        result.ShouldHaveValidationErrorFor(x => x.Driver);
    }

    [Fact]
    public void TruckIdentifiers_UseValueObjectRules()
    {
        var command = ValidCreateCommand() with
        {
            Truck = new TruckInput { UnitNumber = "  ", LicensePlate = "AB!C", Carrier = new string('x', 201) },
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Truck.UnitNumber").WithErrorMessage("UnitNumber is required.");
        result.ShouldHaveValidationErrorFor("Truck.LicensePlate").WithErrorMessage("LicensePlate may only contain letters, digits and hyphens.");
        result.ShouldHaveValidationErrorFor("Truck.Carrier");
    }

    [Theory]
    [InlineData("+4512345678", true)]
    [InlineData("+905321234567", true)]
    [InlineData("4512345678", false)]
    [InlineData("+0123", false)]
    [InlineData("+45 12 34 56 78", false)]
    public void DriverPhone_MustBeE164_WhenPresent(string phone, bool valid)
    {
        var command = ValidCreateCommand() with { Driver = new DriverInput { Name = "A", LicenseNumber = "DL1", Phone = phone } };

        var result = _validator.TestValidate(command);

        if (valid)
        {
            result.ShouldNotHaveValidationErrorFor("Driver.Phone");
        }
        else
        {
            result.ShouldHaveValidationErrorFor("Driver.Phone");
        }
    }

    [Fact]
    public void DriverName_IsRequired()
    {
        var command = ValidCreateCommand() with { Driver = new DriverInput { Name = " ", LicenseNumber = "DL1" } };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Driver.Name");
    }

    [Fact]
    public void Movements_MustContainAtLeastOne()
    {
        var result = _validator.TestValidate(ValidCreateCommand() with { Movements = [] });

        result.ShouldHaveValidationErrorFor(x => x.Movements).WithErrorMessage("At least one movement is required.");
    }

    [Fact]
    public void MovementFields_AreValidatedPerItem()
    {
        var command = ValidCreateCommand() with
        {
            Movements =
            [
                new MovementInput { Type = MovementType.Delivery, UnitNumber = "MSKU1", Location = "TRIST" },
                new MovementInput { Type = null, UnitNumber = "", Location = "XX", Reference = new string('r', 65) },
                null,
            ],
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("Movements[0].Type");
        result.ShouldHaveValidationErrorFor("Movements[1].Type");
        result.ShouldHaveValidationErrorFor("Movements[1].UnitNumber");
        result.ShouldHaveValidationErrorFor("Movements[1].Location");
        result.ShouldHaveValidationErrorFor("Movements[1].Reference");
        result.ShouldHaveValidationErrorFor("Movements[2]");
    }

    [Fact]
    public void UnknownMovementType_IsRejected()
    {
        var command = ValidCreateCommand() with
        {
            Movements = [new MovementInput { Type = (MovementType)99, UnitNumber = "MSKU1", Location = "TRIST" }],
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Movements[0].Type");
    }
}
