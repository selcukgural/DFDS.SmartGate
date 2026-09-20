using DFDS.SmartGate.Application.Abstractions;
using DFDS.SmartGate.Application.Visits.Models;
using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.Domain.Locations;

namespace DFDS.SmartGate.Application.Visits.Search;

/// <summary>
/// Searches visits within the caller's terminals. Resolves the effective time window (default: last
/// <see cref="SearchLimits.DefaultWindow"/>), restricts the terminal set to the caller's entitlements and delegates
/// the indexed query to <see cref="IVisitReadStore"/>.
/// </summary>
/// <param name="readStore">Read-side projection of visits.</param>
/// <param name="caller">Terminal entitlements of the caller.</param>
/// <param name="timeProvider">Clock used for the default window; injected so tests can freeze time.</param>
public sealed class SearchVisitsHandler(IVisitReadStore readStore, ICallerContext caller, TimeProvider timeProvider)
    : IQueryHandler<SearchVisitsQuery, SearchVisitsResponse>
{
    /// <inheritdoc/>
    /// <returns>
    /// The page; <see cref="AuthorizationErrors.TerminalAccessDenied"/> when an explicit <c>terminalId</c> is outside
    /// the caller's terminals. A caller with no terminals gets an empty page without a database round-trip.
    /// </returns>
    public async Task<Result<SearchVisitsResponse>> HandleAsync(SearchVisitsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var createdTo = query.CreatedTimeTo ?? timeProvider.GetUtcNow();
        var createdFrom = query.CreatedTimeFrom ?? createdTo - SearchLimits.DefaultWindow;
        
        IReadOnlyList<LocationCode> terminals;

        if (query.TerminalId is not null)
        {
            var terminal = LocationCode.Create(query.TerminalId).Value;

            if (!caller.CanAccess(terminal))
            {
                return AuthorizationErrors.TerminalAccessDenied(terminal);
            }

            terminals = [terminal];
        }
        else
        {
            terminals = caller.Terminals;
        }

        if (terminals.Count == 0)
        {
            return ToResponse(PagedItems.Empty<VisitSummary>(), query, createdFrom, createdTo);
        }

        var criteria = new VisitSearchCriteria
        {
            Terminals = terminals,
            Status = query.CurrentStatus,
            MovementFrom = query.MovementFrom is null ? null : LocationFilter.Create(query.MovementFrom).Value,
            MovementTo = query.MovementTo is null ? null : LocationFilter.Create(query.MovementTo).Value,
            CreatedFrom = createdFrom,
            CreatedTo = createdTo,
            CreatedBy = query.CreatedBy,
            Page = query.Page,
            PageSize = query.PageSize,
        };

        var page = await readStore.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);

        return ToResponse(page, query, createdFrom, createdTo);
    }

    /// <summary>Assembles the response with paging metadata and the effective window.</summary>
    /// <param name="page">Items and total count from the read store.</param>
    /// <param name="query">The original query, for page and page size.</param>
    /// <param name="createdFrom">Effective lower bound.</param>
    /// <param name="createdTo">Effective upper bound.</param>
    /// <returns>The response.</returns>
    private static SearchVisitsResponse ToResponse(PagedItems<VisitSummary> page, SearchVisitsQuery query, DateTimeOffset createdFrom, DateTimeOffset createdTo) =>
        new()
        {
            Items = page.Items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = page.TotalCount,
            TotalPages = (page.TotalCount + query.PageSize - 1) / query.PageSize,
            CreatedTimeFrom = createdFrom,
            CreatedTimeTo = createdTo,
        };
}
