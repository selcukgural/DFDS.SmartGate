using DFDS.SmartGate.Domain.Visits;
using DFDS.SmartGate.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DFDS.SmartGate.Infrastructure.Persistence;

/// <summary>
/// Write-side access to the <see cref="Visit"/> aggregate. Loads with change tracking so that
/// <see cref="Visit.TransitionTo"/> is persisted by the unit of work; child collections are loaded in their
/// canonical order with a split query (three primary-key lookups, no cartesian product).
/// </summary>
/// <param name="context">The request-scoped context shared with the unit of work.</param>
internal sealed class EfVisitRepository(VisitDbContext context) : IVisitRepository
{
    /// <inheritdoc/>
    public Task<Visit?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Visits
            .Include(v => v.Movements.OrderBy(m => EF.Property<int>(m, MovementConfiguration.SequenceProperty)))
            .Include(v => v.StatusHistory.OrderBy(h => h.ChangedAt).ThenBy(h => h.Id))
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>Also stamps each movement with its request position so <see cref="Visit.Movements"/> keeps its order on reload.</remarks>
    public void Add(Visit visit)
    {
        ArgumentNullException.ThrowIfNull(visit);

        context.Visits.Add(visit);

        for (var i = 0; i < visit.Movements.Count; i++)
        {
            context.Entry(visit.Movements[i]).Property<int>(MovementConfiguration.SequenceProperty).CurrentValue = i;
        }
    }
}
