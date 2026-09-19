using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.Domain.Locations;

namespace DFDS.SmartGate.Domain.Visits;

/// <summary>
/// Aggregate root for a truck's visit to a terminal. Exposes exactly two ways to change state:
/// <see cref="Create"/> and <see cref="TransitionTo"/>. Truck, driver and movements are fixed at creation;
/// every status change is appended to <see cref="StatusHistory"/> which is never rewritten.
/// </summary>
public sealed class Visit
{
    private readonly List<Movement> _movements;
    private readonly List<StatusHistoryEntry> _statusHistory;

    /// <summary>
    /// Initializes a new visit instance with the immutable data supplied by the draft and the initial status entry.
    /// </summary>
    /// <param name="id">The unique identifier of the visit.</param>
    /// <param name="draft">The initial data used to create the visit.</param>
    /// <param name="createdAt">The timestamp at which the visit was created.</param>
    private Visit(Guid id, VisitDraft draft, DateTimeOffset createdAt)
    {
        Id = id;
        TerminalId = draft.TerminalId;
        Truck = draft.Truck;
        Driver = draft.Driver;
        CreatedAt = createdAt;
        CreatedBy = draft.CreatedBy;
        CurrentStatus = VisitStatusTransitions.Initial;
        _movements = new List<Movement>(draft.Movements.Count);
        _statusHistory = [StatusHistoryEntry.Create(CurrentStatus, createdAt, draft.CreatedBy, reason: null)];
    }

    /// <summary>
    /// Gets the unique identifier of the visit.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the terminal where the visit is registered.
    /// </summary>
    public LocationCode TerminalId { get; }

    /// <summary>
    /// Gets the current lifecycle status of the visit.
    /// </summary>
    public VisitStatus CurrentStatus { get; private set; }

    /// <summary>
    /// Gets the truck associated with the visit.
    /// </summary>
    public Truck Truck { get; }

    /// <summary>
    /// Gets the driver associated with the visit.
    /// </summary>
    public Driver Driver { get; }

    /// <summary>
    /// Gets the movements associated with this visit.
    /// </summary>
    public IReadOnlyList<Movement> Movements => _movements;

    /// <summary>
    /// Gets the append-only audit trail, ordered from oldest to newest. The first entry is always the creation record.
    /// </summary>
    public IReadOnlyList<StatusHistoryEntry> StatusHistory => _statusHistory;

    /// <summary>
    /// Gets the UTC timestamp when the visit was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the identity of the caller that created the visit.
    /// </summary>
    public string CreatedBy { get; }

    /// <summary>
    /// Creates a visit in the <see cref="VisitStatus.PreRegistered"/> state and records the initial audit entry.
    /// The terminal side of each movement is derived from the movement type.
    /// </summary>
    /// <param name="draft">The draft containing the visit details and movements.</param>
    /// <param name="now">The timestamp used for creation and movement validation.</param>
    /// <returns>A success result containing the new visit, or a validation failure when the draft is not valid.</returns>
    public static Result<Visit> Create(VisitDraft draft, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentException.ThrowIfNullOrWhiteSpace(draft.CreatedBy);

        if (draft.Movements.Count == 0)
        {
            return VisitErrors.NoMovements;
        }

        for (var i = 0; i < draft.Movements.Count; i++)
        {
            if (draft.Movements[i].Location == draft.TerminalId)
            {
                return VisitErrors.MovementLocationIsTerminal(i);
            }
        }

        var visit = new Visit(Guid.CreateVersion7(now), draft, now);

        foreach (var movement in draft.Movements)
        {
            visit._movements.Add(Movement.Create(movement, draft.TerminalId, now));
        }

        return visit;
    }

    /// <summary>
    /// Moves the visit to <paramref name="target"/> when the configured transition rules allow it and appends a status audit entry.
    /// Returns a conflict error when the transition is not permitted or the visit is already in the target state.
    /// </summary>
    /// <param name="target">The status to transition the visit to.</param>
    /// <param name="changedBy">The actor responsible for the status change.</param>
    /// <param name="reason">An optional reason for the status change.</param>
    /// <param name="now">The timestamp at which the transition occurred.</param>
    /// <returns>A success result when the transition is recorded; otherwise a validation result describing the conflict.</returns>
    public Result TransitionTo(VisitStatus target, string changedBy, string? reason, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(changedBy);

        if (target == CurrentStatus)
        {
            return VisitErrors.AlreadyInStatus(CurrentStatus);
        }

        if (!VisitStatusTransitions.CanTransition(CurrentStatus, target))
        {
            return VisitErrors.InvalidTransition(CurrentStatus, target);
        }

        _statusHistory.Add(StatusHistoryEntry.Create(target, now, changedBy, reason));
        CurrentStatus = target;

        return Result.Success();
    }
}
