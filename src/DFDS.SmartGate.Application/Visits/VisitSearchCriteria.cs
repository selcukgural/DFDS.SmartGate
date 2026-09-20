using DFDS.SmartGate.Domain.Locations;
using DFDS.SmartGate.Domain.Visits;

namespace DFDS.SmartGate.Application.Visits;

/// <summary>
/// Fully resolved, already-authorized search criteria handed to <see cref="IVisitReadStore.SearchAsync"/>.
/// Every filter maps to an indexed column; the read store must not apply any authorization of its own.
/// </summary>
public sealed record VisitSearchCriteria
{
    /// <summary>Terminals to search in. Never empty: the handler short-circuits when the caller has no terminals.</summary>
    public required IReadOnlyList<LocationCode> Terminals { get; init; }

    /// <summary>Restricts to visits currently in this status, when set.</summary>
    public VisitStatus? Status { get; init; }

    /// <summary>Restricts to visits with a movement whose origin matches this country or location, when set.</summary>
    public LocationFilter? MovementFrom { get; init; }

    /// <summary>
    /// Restricts to visits with a movement whose destination matches this country or location, when set.
    /// When both <see cref="MovementFrom"/> and <see cref="MovementTo"/> are set they must match the <em>same</em> movement.
    /// </summary>
    public LocationFilter? MovementTo { get; init; }

    /// <summary>Inclusive lower bound of <c>createdTime</c> (UTC). Always set: the handler applies the default window.</summary>
    public required DateTimeOffset CreatedFrom { get; init; }

    /// <summary>Inclusive upper bound of <c>createdTime</c> (UTC). Always set.</summary>
    public required DateTimeOffset CreatedTo { get; init; }

    /// <summary>Exact-match filter on the creator's token subject, when set.</summary>
    public string? CreatedBy { get; init; }

    /// <summary>1-based page number.</summary>
    public required int Page { get; init; }

    /// <summary>Number of items per page; already clamped to the allowed range.</summary>
    public required int PageSize { get; init; }

    /// <summary>Number of items to skip for <see cref="Page"/>.</summary>
    public int Skip => (Page - 1) * PageSize;
}
