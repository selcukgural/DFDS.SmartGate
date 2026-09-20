using DFDS.SmartGate.Application.Visits.Models;

namespace DFDS.SmartGate.Application.Visits.Search;

/// <summary>
/// One page of search results with paging metadata and the effective time window (so clients can see the
/// default window that was applied when they gave none).
/// </summary>
public sealed record SearchVisitsResponse
{
    /// <summary>Visits of the requested page, newest first.</summary>
    public required IReadOnlyList<VisitSummary> Items { get; init; }

    /// <summary>1-based page number that was returned.</summary>
    public required int Page { get; init; }

    /// <summary>Items per page.</summary>
    public required int PageSize { get; init; }

    /// <summary>Total number of matches across all pages.</summary>
    public required int TotalCount { get; init; }

    /// <summary>Number of pages for <see cref="TotalCount"/> and <see cref="PageSize"/>; zero when there are no matches.</summary>
    public required int TotalPages { get; init; }

    /// <summary>Effective inclusive lower bound of <c>createdTime</c> (UTC).</summary>
    public required DateTimeOffset CreatedTimeFrom { get; init; }

    /// <summary>Effective inclusive upper bound of <c>createdTime</c> (UTC).</summary>
    public required DateTimeOffset CreatedTimeTo { get; init; }
}
