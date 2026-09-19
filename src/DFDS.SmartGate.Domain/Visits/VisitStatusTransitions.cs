using System.Collections.Frozen;

namespace DFDS.SmartGate.Domain.Visits;

/// <summary>
/// Allowed status transitions, expressed as data so the flow can be extended (e.g. a future
/// <c>AtGate → Rejected</c>) by adding a row rather than editing branching logic.
/// The flow is strictly forward: no skipping, no going back; <see cref="VisitStatus.Completed"/> is terminal.
/// </summary>
public static class VisitStatusTransitions
{
    /// <summary>
    /// The status assigned to a visit when the workflow begins.
    /// </summary>
    public const VisitStatus Initial = VisitStatus.PreRegistered;

    /// Maps each non-terminal status to the single status that may follow it in the workflow.
    private static readonly FrozenDictionary<VisitStatus, VisitStatus> Next =
        new Dictionary<VisitStatus, VisitStatus>
        {
            [VisitStatus.PreRegistered] = VisitStatus.AtGate,
            [VisitStatus.AtGate] = VisitStatus.OnSite,
            [VisitStatus.OnSite] = VisitStatus.Completed,
        }.ToFrozenDictionary();

    /// <summary>
    /// Determines whether moving from <paramref name="from"/> to <paramref name="to"/> is allowed.
    /// </summary>
    /// <param name="from">The current status.</param>
    /// <param name="to">The candidate next status.</param>
    /// <returns><see langword="true"/> when the transition is explicitly allowed; otherwise, <see langword="false"/>.</returns>
    public static bool CanTransition(VisitStatus from, VisitStatus to) =>
        Next.TryGetValue(from, out var next) && next == to;

    /// <summary>
    /// Gets the single status reachable from <paramref name="from"/>, or <see langword="null"/> when the status is terminal.
    /// </summary>
    /// <param name="from">The current status.</param>
    /// <returns>
    /// The next status in the workflow, or <see langword="null"/> if no forward transition exists.
    /// </returns>
    public static VisitStatus? NextOf(VisitStatus from) =>
        Next.TryGetValue(from, out var next) ? next : null;

    /// <summary>
    /// Checks whether a status cannot transition to any other status in the workflow.
    /// </summary>
    /// <param name="status">The status to evaluate.</param>
    /// <returns><see langword="true"/> when the status is terminal; otherwise, <see langword="false"/>.</returns>
    public static bool IsTerminal(VisitStatus status) => !Next.ContainsKey(status);
}
