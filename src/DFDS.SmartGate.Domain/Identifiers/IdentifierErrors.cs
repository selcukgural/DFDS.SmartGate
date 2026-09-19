using DFDS.SmartGate.Domain.Common;

namespace DFDS.SmartGate.Domain.Identifiers;

internal static class IdentifierErrors
{
    /// <summary>
    /// Creates a validation error indicating that an identifier value is missing.
    /// </summary>
    /// <param name="identifierName">The name of the identifier that is empty.</param>
    /// <returns>A validation domain error describing the missing identifier.</returns>
    public static DomainError Empty(string identifierName) =>
        DomainError.Validation($"{identifierName}.Empty", $"{identifierName} is required.");

    /// <summary>
    /// Creates a validation error indicating that an identifier exceeds its maximum allowed length after normalization.
    /// </summary>
    /// <param name="identifierName">The name of the identifier that is too long.</param>
    /// <param name="maxLength">The maximum allowed length for the normalized identifier.</param>
    /// <returns>A validation domain error describing the identifier length violation.</returns>
    public static DomainError TooLong(string identifierName, int maxLength) =>
        DomainError.Validation($"{identifierName}.TooLong", $"{identifierName} must not exceed {maxLength} characters after normalisation.");

    /// <summary>
    /// Creates a validation error indicating that an identifier contains invalid characters.
    /// </summary>
    /// <param name="identifierName">The name of the identifier that contains invalid characters.</param>
    /// <returns>A validation domain error describing the invalid character set.</returns>
    public static DomainError InvalidCharacters(string identifierName) =>
        DomainError.Validation($"{identifierName}.InvalidCharacters", $"{identifierName} may only contain letters, digits and hyphens.");
}
