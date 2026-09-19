namespace DFDS.SmartGate.Domain.Common;

/// <summary>
/// Represents a domain-level failure that is expected and handled as part of normal business logic.
/// This value object is used to carry a stable machine-readable error code, a user-facing message,
/// and the category of the failure.
/// </summary>
/// <remarks>
/// This type is intentionally modeled as a readonly record struct so it remains lightweight,
/// immutable, and easy to compare by value across domain operations.
/// </remarks>
public readonly record struct DomainError(string Code, string Message, ErrorKind Kind)
{
    /// <summary>
    /// Creates a validation error for a domain rule or input validation failure.
    /// </summary>
    /// <param name="code">
    /// Stable, machine-readable error code such as <c>Visit.InvalidTransition</c>.
    /// </param>
    /// <param name="message">
    /// Human-readable description that may be returned to API clients or UI layers.
    /// </param>
    /// <returns>A <see cref="DomainError"/> marked as a validation error.</returns>
    public static DomainError Validation(string code, string message) => new(code, message, ErrorKind.Validation);

    /// <summary>
    /// Creates a not-found error when a requested domain entity or resource cannot be located.
    /// </summary>
    /// <param name="code">
    /// Stable, machine-readable identifier for the missing resource.
    /// </param>
    /// <param name="message">
    /// User-friendly message describing the missing resource or lookup failure.
    /// </param>
    /// <returns>A <see cref="DomainError"/> marked as not found.</returns>
    public static DomainError NotFound(string code, string message) => new(code, message, ErrorKind.NotFound);

    /// <summary>
    /// Creates a conflict error when the requested operation cannot be completed due to
    /// an invalid state or a conflicting existing resource.
    /// </summary>
    /// <param name="code">
    /// Stable, machine-readable identifier for the conflict condition.
    /// </param>
    /// <param name="message">
    /// Human-readable explanation of the conflict or state mismatch.
    /// </param>
    /// <returns>A <see cref="DomainError"/> marked as a conflict.</returns>
    public static DomainError Conflict(string code, string message) => new(code, message, ErrorKind.Conflict);
}