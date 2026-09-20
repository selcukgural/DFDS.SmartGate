using DFDS.SmartGate.Application.Validation;
using FluentValidation;

namespace DFDS.SmartGate.Application.Visits.UpdateStatus;

/// <summary>Boundary validation for <see cref="UpdateVisitStatusCommand"/>. Whether the transition is allowed is decided by the aggregate.</summary>
public sealed class UpdateVisitStatusCommandValidator : AbstractValidator<UpdateVisitStatusCommand>
{
    /// <summary>Initialises the rule set.</summary>
    public UpdateVisitStatusCommandValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.Status).NotNull().IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(FieldLimits.ReasonMaxLength);
    }
}
