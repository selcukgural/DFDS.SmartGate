using DFDS.SmartGate.Application.Abstractions;
using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.Domain.Visits;
using Microsoft.EntityFrameworkCore;

namespace DFDS.SmartGate.Infrastructure.Persistence;

/// <summary>
/// Commits the tracked changes of the request in one transaction and translates the one expected persistence
/// failure, an optimistic-concurrency conflict on <c>xmin</c>, into <see cref="VisitErrors.ConcurrentUpdate"/>.
/// Any other database failure is unexpected and propagates as an exception.
/// </summary>
/// <param name="context">The request-scoped context shared with the repository.</param>
internal sealed class EfUnitOfWork(VisitDbContext context) : IUnitOfWork
{
    /// <inheritdoc/>
    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            // The row is left untouched in the database; the caller re-reads and retries.
            return VisitErrors.ConcurrentUpdate;
        }
    }
}
