namespace DFDS.SmartGate.Domain.Visits;

/// <summary>
/// One immutable row of the visit's audit trail. Rows are only ever appended, never updated or deleted.
/// </summary>
public sealed class StatusHistoryEntry
{
    /// <summary>
    /// Initializes a new immutable status history entry.
    /// </summary>
    /// <param name="id">The unique identifier for this audit-row entry.</param>
    /// <param name="status">The visit status recorded for this transition.</param>
    /// <param name="changedAt">The timestamp when the status change occurred.</param>
    /// <param name="changedBy">The subject of the caller token that initiated the change.</param>
    /// <param name="reason">An optional explanation for the status change.</param>
    private StatusHistoryEntry(Guid id, VisitStatus status, DateTimeOffset changedAt, string changedBy, string? reason)
    {
        Id = id;
        Status = status;
        ChangedAt = changedAt;
        ChangedBy = changedBy;
        Reason = reason;
    }
    
    /// <summary>Materialisation constructor for the persistence layer; state is written to the backing fields.</summary>
    private StatusHistoryEntry()
    {
        ChangedBy = null!;
    }

    /// <summary>
    /// Gets the unique identifier of this audit-row entry.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the visit status captured by this history entry.
    /// </summary>
    public VisitStatus Status { get; }

    /// <summary>
    /// Gets the date and time when the visit status was changed.
    /// </summary>
    public DateTimeOffset ChangedAt { get; }

    /// <summary>Subject of the caller's token (never personal data).</summary>
    public string ChangedBy { get; }

    /// <summary>
    /// Gets the optional reason associated with the status change.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Creates a new status history entry for a visit transition.
    /// </summary>
    /// <param name="status">The new visit status.</param>
    /// <param name="changedAt">The timestamp for the status change.</param>
    /// <param name="changedBy">The subject of the caller token that triggered the change.</param>
    /// <param name="reason">An optional reason supplied for the transition.</param>
    /// <returns>A new immutable audit trail record.</returns>
    internal static StatusHistoryEntry Create(VisitStatus status, DateTimeOffset changedAt, string changedBy, string? reason) =>
        new(Guid.CreateVersion7(changedAt), status, changedAt, changedBy, reason);
}
