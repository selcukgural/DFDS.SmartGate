using System.Linq.Expressions;
using DFDS.SmartGate.Application.Visits;
using DFDS.SmartGate.Application.Visits.Models;
using DFDS.SmartGate.Domain.Visits;
using DFDS.SmartGate.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DFDS.SmartGate.Infrastructure.Persistence;

/// <summary>
/// Read-side projections straight into the response models: no aggregate materialisation, no change tracking.
/// Get-by-id is a compiled query (the hottest read, cached by the handler). Search composes only the filters that
/// are set, each backed by an index declared in the entity configurations, and issues one count and one page query.
/// </summary>
/// <param name="context">The request-scoped context.</param>
internal sealed class EfVisitReadStore(VisitDbContext context) : IVisitReadStore
{
    private static readonly Func<VisitDbContext, Guid, CancellationToken, Task<VisitResponse?>> GetByIdQuery =
        EF.CompileAsyncQuery((VisitDbContext db, Guid id, CancellationToken ct) =>
            db.Visits
                .AsNoTracking()
                // One round-trip: the movements x history product of a single visit is a handful of rows.
                .AsSingleQuery()
                .Where(v => v.Id == id)
                .Select(v => new VisitResponse(
                    v.Id,
                    v.TerminalId.Value,
                    v.CurrentStatus,
                    new TruckDto(v.Truck.UnitNumber.Value, v.Truck.LicensePlate.Value, v.Truck.Carrier),
                    new DriverDto(v.Driver.Name, v.Driver.LicenseNumber.Value, v.Driver.Phone),
                    v.Movements
                        .OrderBy(m => EF.Property<int>(m, MovementConfiguration.SequenceProperty))
                        .Select(m => new MovementDto(m.Id, m.Type, m.UnitNumber.Value, m.From.Value, m.To.Value, m.Reference))
                        .ToList(),
                    v.StatusHistory
                        .OrderBy(h => h.ChangedAt)
                        .ThenBy(h => h.Id)
                        .Select(h => new StatusHistoryEntryDto(h.Status, h.ChangedAt, h.ChangedBy, h.Reason))
                        .ToList(),
                    v.CreatedAt,
                    v.CreatedBy))
                .FirstOrDefault());

    /// <inheritdoc/>
    public Task<VisitResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        GetByIdQuery(context, id, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedItems<VisitSummary>> SearchAsync(VisitSearchCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var query = ApplyFilters(context.Visits.AsNoTracking(), criteria);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        if (totalCount == 0 || criteria.Skip >= totalCount)
        {
            return new PagedItems<VisitSummary>([], totalCount);
        }

        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .ThenByDescending(v => v.Id)
            .Skip(criteria.Skip)
            .Take(criteria.PageSize)
            .Select(v => new VisitSummary(
                v.Id,
                v.TerminalId.Value,
                v.CurrentStatus,
                new TruckDto(v.Truck.UnitNumber.Value, v.Truck.LicensePlate.Value, v.Truck.Carrier),
                v.Driver.Name,
                v.Movements
                    .OrderBy(m => EF.Property<int>(m, MovementConfiguration.SequenceProperty))
                    .Select(m => new MovementDto(m.Id, m.Type, m.UnitNumber.Value, m.From.Value, m.To.Value, m.Reference))
                    .ToList(),
                v.CreatedAt,
                v.CreatedBy))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedItems<VisitSummary>(items, totalCount);
    }

    /// <summary>
    /// Adds one <c>WHERE</c> clause per set criterion. The terminal and time-window filters are always present
    /// (the handler guarantees both), so the planner can always start from <c>ix_visits_terminal_created</c>.
    /// </summary>
    /// <param name="visits">The base query.</param>
    /// <param name="criteria">The resolved criteria.</param>
    /// <returns>The filtered, unordered query.</returns>
    private static IQueryable<Visit> ApplyFilters(IQueryable<Visit> visits, VisitSearchCriteria criteria)
    {
        var terminals = criteria.Terminals;

        if (terminals.Count == 1)
        {
            var terminal = terminals[0];
            visits = visits.Where(v => v.TerminalId == terminal);
        }
        else
        {
            visits = visits.Where(v => terminals.Contains(v.TerminalId));
        }

        visits = visits.Where(v => v.CreatedAt >= criteria.CreatedFrom && v.CreatedAt <= criteria.CreatedTo);

        if (criteria.Status is { } status)
        {
            visits = visits.Where(v => v.CurrentStatus == status);
        }

        if (criteria.CreatedBy is { } createdBy)
        {
            visits = visits.Where(v => v.CreatedBy == createdBy);
        }

        var movement = MovementFilter(criteria);

        if (movement is not null)
        {
            visits = visits.Where(MovementPredicates.AnyMovement(movement));
        }

        return visits;
    }

    /// <summary>
    /// Combines the <c>movementFrom</c> / <c>movementTo</c> criteria into one movement predicate. When both are set
    /// they apply to the same movement (a delivery from X that is also a delivery to Y), per the search contract.
    /// </summary>
    /// <param name="criteria">The resolved criteria.</param>
    /// <returns>The predicate, or <see langword="null"/> when neither criterion is set.</returns>
    private static Expression<Func<Movement, bool>>? MovementFilter(VisitSearchCriteria criteria)
    {
        var from = criteria.MovementFrom is { } f ? MovementPredicates.FromMatches(f) : null;
        var to = criteria.MovementTo is { } t ? MovementPredicates.ToMatches(t) : null;

        if (from is null)
        {
            return to;
        }

        return to is null ? from : MovementPredicates.And(from, to);
    }
}
