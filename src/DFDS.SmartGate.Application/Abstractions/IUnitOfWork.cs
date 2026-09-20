using DFDS.SmartGate.Domain.Common;

namespace DFDS.SmartGate.Application.Abstractions;

/// <summary>
/// Commits the changes tracked during the current request as one transaction.
/// Expected persistence failures (optimistic-concurrency conflicts) are reported as a <see cref="Result"/>,
/// never as exceptions, so handlers can map them like any other domain outcome.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>
    /// Success, or <see cref="Domain.Visits.VisitErrors.ConcurrentUpdate"/> when another request modified one of the
    /// tracked aggregates in the meantime.
    /// </returns>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
