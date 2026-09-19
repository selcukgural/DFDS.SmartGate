namespace DFDS.SmartGate.Domain.Visits;

/// <summary>
/// Persistence port for the <see cref="Visit"/> aggregate. Search/read-model queries live in the
/// Application layer because they return projections, not aggregates.
/// </summary>
public interface IVisitRepository
{
    /// <summary>
    /// Loads the aggregate including its movements and event history.
    /// </summary>
    /// <param name="id">The unique identifier of the visit to retrieve.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// The matching <see cref="Visit"/> aggregate, or <see langword="null"/> when no visit exists for the specified id.
    /// </returns>
    Task<Visit?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new visit aggregate to the repository so it can be persisted.
    /// </summary>
    /// <param name="visit">The visit aggregate to store.</param>
    void Add(Visit visit);
}
