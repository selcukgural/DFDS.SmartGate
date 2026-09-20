using DFDS.SmartGate.Application.Validation;
using DFDS.SmartGate.Domain.Locations;
using FluentValidation;

namespace DFDS.SmartGate.Application.Visits.Search;

/// <summary>Boundary validation for <see cref="SearchVisitsQuery"/>; optional filters are validated only when present.</summary>
public sealed class SearchVisitsQueryValidator : AbstractValidator<SearchVisitsQuery>
{
    /// <summary>Initialises the rule set.</summary>
    public SearchVisitsQueryValidator()
    {
        RuleFor(x => x.TerminalId).MustCreate(LocationCode.Create).When(x => x.TerminalId is not null);
        RuleFor(x => x.CurrentStatus).IsInEnum().When(x => x.CurrentStatus is not null);
        RuleFor(x => x.MovementFrom).MustCreate(LocationFilter.Create).When(x => x.MovementFrom is not null);
        RuleFor(x => x.MovementTo).MustCreate(LocationFilter.Create).When(x => x.MovementTo is not null);
        RuleFor(x => x.CreatedBy).NotEmpty().MaximumLength(FieldLimits.CreatedByMaxLength).When(x => x.CreatedBy is not null);

        RuleFor(x => x.CreatedTimeFrom)
            .LessThanOrEqualTo(x => x.CreatedTimeTo!.Value)
            .When(x => x.CreatedTimeFrom is not null && x.CreatedTimeTo is not null)
            .WithMessage("'createdTimeFrom' must not be later than 'createdTimeTo'.");

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, SearchLimits.MaxPageSize);
    }
}
