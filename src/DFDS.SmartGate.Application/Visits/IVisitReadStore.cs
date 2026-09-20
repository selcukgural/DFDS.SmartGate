using DFDS.SmartGate.Application.Visits.Models;

namespace DFDS.SmartGate.Application.Visits;

/// <summary>
/// Read-side port for visits. Implementations return projections straight into the response models
/// (no aggregate materialisation, no change tracking) and are expected to back every criterion with an index.
/// </summary>
public interface IVisitReadStore
{
    /// <summary>Loads the full representation of one visit, regardless of terminal; the handler applies authorization.</summary>
    /// <param name="id">Identifier of the visit.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The visit, or <see langword="null"/> when it does not exist.</returns>
    Task<VisitResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns one page of visits matching <paramref name="criteria"/>, ordered by <c>createdTime</c> descending then id descending.
    /// </summary>
    /// <param name="criteria">Resolved, authorized criteria.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The page and the total match count.</returns>
    Task<PagedItems<VisitSummary>> SearchAsync(VisitSearchCriteria criteria, CancellationToken cancellationToken);
}
