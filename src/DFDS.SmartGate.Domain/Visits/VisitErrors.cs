using DFDS.SmartGate.Domain.Common;

namespace DFDS.SmartGate.Domain.Visits;

/// <summary>
/// Provides factory methods and predefined domain errors for validating and managing visit state transitions.
/// </summary>
public static class VisitErrors
{
    /// <summary>
    /// Represents the validation error raised when a visit does not contain any movements.
    /// </summary>
    public static readonly DomainError NoMovements =
        DomainError.Validation("Visit.NoMovements", "A visit must contain at least one movement.");

    /// <summary>
    /// Creates a validation error when a movement points to the visited terminal as its location instead of an external origin or destination.
    /// </summary>
    /// <param name="movementIndex">The zero-based index of the movement that violates the location rule.</param>
    /// <returns>A validation error describing the invalid terminal location.</returns>
    public static DomainError MovementLocationIsTerminal(int movementIndex) =>
        DomainError.Validation(
            "Visit.MovementLocationIsTerminal",
            $"Movement at index {movementIndex} names the visited terminal as its location; the location must be the external origin or destination.");

    /// <summary>
    /// Creates a not-found error for a visit that does not exist in the system.
    /// </summary>
    /// <param name="visitId">The identifier of the missing visit.</param>
    /// <returns>A not-found error for the specified visit.</returns>
    public static DomainError NotFound(Guid visitId) =>
        DomainError.NotFound("Visit.NotFound", $"Visit '{visitId}' was not found.");

    /// <summary>
    /// Creates a conflict error when a visit is already in the specified status.
    /// </summary>
    /// <param name="status">The status the visit is already in.</param>
    /// <returns>A conflict error indicating the visit cannot be re-applied to the same status.</returns>
    public static DomainError AlreadyInStatus(VisitStatus status) =>
        DomainError.Conflict("Visit.AlreadyInStatus", $"The visit is already in status '{status}'.");

    /// <summary>
    /// Creates a conflict error when a requested status transition is not allowed.
    /// </summary>
    /// <param name="from">The current status of the visit.</param>
    /// <param name="to">The requested target status.</param>
    /// <returns>A conflict error describing the allowed next status.</returns>
    public static DomainError InvalidTransition(VisitStatus from, VisitStatus to)
    {
        var next = VisitStatusTransitions.NextOf(from);
        var allowed = next is null ? "none, the visit is completed" : $"'{next}'";

        return DomainError.Conflict(
            "Visit.InvalidTransition",
            $"Cannot transition the visit from '{from}' to '{to}'. Allowed next status: {allowed}.");
    }

    /// <summary>
    /// Represents the conflict error raised when a concurrent update is detected for the same visit.
    /// </summary>
    public static readonly DomainError ConcurrentUpdate =
        DomainError.Conflict("Visit.ConcurrentUpdate", "The visit was modified by another request. Reload it and retry.");
}
