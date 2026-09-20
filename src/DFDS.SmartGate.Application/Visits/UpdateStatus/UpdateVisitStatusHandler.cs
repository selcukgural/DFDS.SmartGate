using DFDS.SmartGate.Application.Abstractions;
using DFDS.SmartGate.Application.Visits.Models;
using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.Domain.Visits;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DFDS.SmartGate.Application.Visits.UpdateStatus;

/// <summary>
/// Applies a status transition to a visit the caller is entitled to, persists the appended audit row, evicts the
/// cached representation and emits one audit log event. A visit outside the caller's terminals is reported as
/// not found so that ids of other terminals' visits cannot be probed.
/// </summary>
/// <param name="repository">Write-side access to visits.</param>
/// <param name="unitOfWork">Commits the transition; reports concurrency conflicts.</param>
/// <param name="caller">Identity and terminal entitlements of the caller.</param>
/// <param name="cache">Cache holding <see cref="VisitResponse"/> entries to invalidate.</param>
/// <param name="timeProvider">Clock; injected so tests can freeze time.</param>
/// <param name="logger">Receives one audit event per status change.</param>
public sealed class UpdateVisitStatusHandler(
    IVisitRepository repository,
    IUnitOfWork unitOfWork,
    ICallerContext caller,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<UpdateVisitStatusHandler> logger) : ICommandHandler<UpdateVisitStatusCommand, VisitResponse>
{
    /// <inheritdoc/>
    /// <returns>
    /// The updated visit with its full history; <see cref="VisitErrors.NotFound"/> when the visit is missing or belongs
    /// to another terminal; <see cref="VisitErrors.AlreadyInStatus"/> / <see cref="VisitErrors.InvalidTransition"/> /
    /// <see cref="VisitErrors.ConcurrentUpdate"/> as conflicts.
    /// </returns>
    public async Task<Result<VisitResponse>> HandleAsync(UpdateVisitStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var visit = await repository.GetByIdAsync(command.VisitId, cancellationToken).ConfigureAwait(false);

        if (visit is null || !caller.CanAccess(visit.TerminalId))
        {
            return VisitErrors.NotFound(command.VisitId);
        }

        var previous = visit.CurrentStatus;
        var transitioned = visit.TransitionTo(command.Status!.Value, caller.Subject, command.Reason, timeProvider.GetUtcNow());

        if (transitioned.IsFailure)
        {
            return transitioned.Error.Value;
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (saved.IsFailure)
        {
            return saved.Error.Value;
        }

        await cache.RemoveAsync(VisitCacheKeys.ById(visit.Id), cancellationToken).ConfigureAwait(false);
        VisitAuditLog.VisitStatusChanged(logger, visit.Id, previous, visit.CurrentStatus, caller.Subject);

        return VisitMapping.ToResponse(visit);
    }
}
