using DFDS.SmartGate.Domain.Visits;

namespace DFDS.SmartGate.Application.Visits.Search;

/// <summary>
/// Query-string parameters of the search endpoint, as received; every member is optional on the wire, which is why the
/// paging values are nullable rather than defaulted here. Validated once by <see cref="SearchVisitsQueryValidator"/>;
/// the handler resolves defaults (time window, terminals, paging) and builds a <see cref="VisitSearchCriteria"/>.
/// </summary>
public sealed record SearchVisitsQuery
{
    /// <summary>UN/LOCODE of a terminal the caller is entitled to. When omitted, all of the caller's terminals are searched.</summary>
    public string? TerminalId { get; init; }

    /// <summary>Restricts to visits currently in this status; a <see cref="VisitStatus"/> name in any casing (see <see cref="VisitStatusNames"/>).</summary>
    public string? CurrentStatus { get; init; }

    /// <summary>Origin filter: a 2-letter country code or a 5-character UN/LOCODE matched against movement origins.</summary>
    public string? MovementFrom { get; init; }

    /// <summary>Destination filter: a 2-letter country code or a 5-character UN/LOCODE matched against movement destinations.</summary>
    public string? MovementTo { get; init; }

    /// <summary>Inclusive lower bound of <c>createdTime</c>. Defaults to <see cref="CreatedTimeTo"/> minus <see cref="SearchLimits.DefaultWindow"/>.</summary>
    public DateTimeOffset? CreatedTimeFrom { get; init; }

    /// <summary>Inclusive upper bound of <c>createdTime</c>. Defaults to now.</summary>
    public DateTimeOffset? CreatedTimeTo { get; init; }

    /// <summary>Exact match on the creator's token subject.</summary>
    public string? CreatedBy { get; init; }

    /// <summary>1-based page number; defaults to <see cref="SearchLimits.DefaultPage"/> when omitted.</summary>
    public int? Page { get; init; }

    /// <summary>Items per page, at most <see cref="SearchLimits.MaxPageSize"/>; defaults to <see cref="SearchLimits.DefaultPageSize"/> when omitted.</summary>
    public int? PageSize { get; init; }
}
