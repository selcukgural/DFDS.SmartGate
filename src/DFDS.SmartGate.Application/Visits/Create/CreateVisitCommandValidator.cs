using DFDS.SmartGate.Application.Validation;
using DFDS.SmartGate.Domain.Identifiers;
using DFDS.SmartGate.Domain.Locations;
using FluentValidation;

namespace DFDS.SmartGate.Application.Visits.Create;

/// <summary>
/// Boundary validation for <see cref="CreateVisitCommand"/>. Format rules delegate to the domain value objects;
/// only presence, lengths and cross-field rules are expressed here.
/// </summary>
public sealed class CreateVisitCommandValidator : AbstractValidator<CreateVisitCommand>
{
    /// <summary>Initialises the rule set.</summary>
    public CreateVisitCommandValidator()
    {
        RuleFor(x => x.TerminalId).MustCreate(LocationCode.Create);

        RuleFor(x => x.Truck).NotNull();
        RuleFor(x => x.Truck!).SetValidator(new TruckInputValidator()).When(x => x.Truck is not null);

        RuleFor(x => x.Driver).NotNull();
        RuleFor(x => x.Driver!).SetValidator(new DriverInputValidator()).When(x => x.Driver is not null);

        RuleFor(x => x.Movements).NotEmpty().WithMessage("At least one movement is required.");
        RuleForEach(x => x.Movements).NotNull().SetValidator(new MovementInputValidator()!);
    }
}

/// <summary>Rules for <see cref="TruckInput"/>.</summary>
public sealed class TruckInputValidator : AbstractValidator<TruckInput>
{
    /// <summary>Initialises the rule set.</summary>
    public TruckInputValidator()
    {
        RuleFor(x => x.UnitNumber).MustCreate(UnitNumber.Create);
        RuleFor(x => x.LicensePlate).MustCreate(LicensePlate.Create);
        RuleFor(x => x.Carrier).MaximumLength(FieldLimits.CarrierMaxLength);
    }
}

/// <summary>Rules for <see cref="DriverInput"/>.</summary>
public sealed class DriverInputValidator : AbstractValidator<DriverInput>
{
    /// <summary>Initialises the rule set.</summary>
    public DriverInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(FieldLimits.DriverNameMaxLength);
        RuleFor(x => x.LicenseNumber).MustCreate(DriverLicenseNumber.Create);
        RuleFor(x => x.Phone).Matches(FieldLimits.PhonePattern).When(x => x.Phone is not null)
            .WithMessage("Phone must be in E.164 format, e.g. '+4512345678'.");
    }
}

/// <summary>Rules for <see cref="MovementInput"/>.</summary>
public sealed class MovementInputValidator : AbstractValidator<MovementInput>
{
    /// <summary>Initialises the rule set.</summary>
    public MovementInputValidator()
    {
        RuleFor(x => x.Type).NotNull().IsInEnum();
        RuleFor(x => x.UnitNumber).MustCreate(UnitNumber.Create);
        RuleFor(x => x.Location).MustCreate(LocationCode.Create);
        RuleFor(x => x.Reference).MaximumLength(FieldLimits.ReferenceMaxLength);
    }
}
