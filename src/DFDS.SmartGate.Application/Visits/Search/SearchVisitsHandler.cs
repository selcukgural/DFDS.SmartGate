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
        var paging = Paging.From(query);

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
            return ToResponse(PagedItems.Empty<VisitSummary>(), paging, createdFrom, createdTo);
        }

        var criteria = new VisitSearchCriteria
        {
            Terminals = terminals,
            Status = query.CurrentStatus is null ? null : VisitStatusNames.Parse(query.CurrentStatus),
            MovementFrom = query.MovementFrom is null ? null : LocationFilter.Create(query.MovementFrom).Value,
            MovementTo = query.MovementTo is null ? null : LocationFilter.Create(query.MovementTo).Value,
            CreatedFrom = createdFrom,
            CreatedTo = createdTo,
            CreatedBy = query.CreatedBy,
            Page = paging.Page,
            PageSize = paging.PageSize,
        };

        var page = await readStore.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);

        return ToResponse(page, paging, createdFrom, createdTo);
    }

    /// <summary>Assembles the response with paging metadata and the effective window.</summary>
    /// <param name="page">Items and total count from the read store.</param>
    /// <param name="paging">Effective page and page size.</param>
    /// <param name="createdFrom">Effective lower bound.</param>
    /// <param name="createdTo">Effective upper bound.</param>
    /// <returns>The response.</returns>
    private static SearchVisitsResponse ToResponse(PagedItems<VisitSummary> page, Paging paging, DateTimeOffset createdFrom, DateTimeOffset createdTo) =>
        new()
        {
            Items = page.Items,
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalCount = page.TotalCount,
            TotalPages = (page.TotalCount + paging.PageSize - 1) / paging.PageSize,
            CreatedTimeFrom = createdFrom,
            CreatedTimeTo = createdTo,
        };

    /// <summary>Effective paging: the client's values or <see cref="SearchLimits"/> defaults.</summary>
    /// <param name="Page">1-based page number.</param>
    /// <param name="PageSize">Items per page.</param>
    private readonly record struct Paging(int Page, int PageSize)
    {
        /// <summary>Resolves the paging of a validated query.</summary>
        /// <param name="query">The query.</param>
        /// <returns>Client values where given, defaults otherwise.</returns>
        public static Paging From(SearchVisitsQuery query) =>
            new(query.Page ?? SearchLimits.DefaultPage, query.PageSize ?? SearchLimits.DefaultPageSize);
    }
}
