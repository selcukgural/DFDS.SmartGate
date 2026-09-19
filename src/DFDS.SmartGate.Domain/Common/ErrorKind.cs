namespace DFDS.SmartGate.Domain.Common;

/// <summary>
/// Classifies a <see cref="DomainError"/> so the presentation layer can map it to a transport status
/// (e.g. HTTP 400 / 404 / 409) without inspecting error codes.
/// </summary>
/// <summary>
/// Represents the high-level classification of a domain error.
/// This allows the presentation layer to translate a domain failure into an appropriate transport-level status such as validation, missing resource, or conflict.
/// </summary>
public enum ErrorKind
{
    /// <summary>
    /// The request is invalid due to bad input or violated business rules.
    /// </summary>
    Validation = 1,

    /// <summary>
    /// The requested resource could not be found.
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// The request conflicts with the current state of the resource or system.
    /// </summary>
    Conflict = 3,
}
