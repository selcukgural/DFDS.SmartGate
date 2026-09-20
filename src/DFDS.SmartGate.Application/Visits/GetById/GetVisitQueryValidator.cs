using FluentValidation;

namespace DFDS.SmartGate.Application.Visits.GetById;

/// <summary>Boundary validation for <see cref="GetVisitQuery"/>.</summary>
public sealed class GetVisitQueryValidator : AbstractValidator<GetVisitQuery>
{
    /// <summary>Initialises the rule set.</summary>
    public GetVisitQueryValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
    }
}
