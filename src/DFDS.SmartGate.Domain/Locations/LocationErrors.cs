using DFDS.SmartGate.Domain.Common;

namespace DFDS.SmartGate.Domain.Locations;

/// <summary>
/// Provides reusable domain validation errors for location-related fields and filters.
/// </summary>
internal static class LocationErrors
{
    /// <summary>
    /// Creates a validation error indicating that a location code field is required.
    /// </summary>
    /// <param name="fieldName">The name of the field being validated.</param>
    /// <returns>A domain validation error for an empty location code.</returns>
    public static DomainError LocationCodeEmpty(string fieldName) =>
        DomainError.Validation("LocationCode.Empty", $"{fieldName} is required.");

    /// <summary>
    /// Creates a validation error indicating that a location code does not match the expected UN/LOCODE format.
    /// </summary>
    /// <param name="fieldName">The name of the field being validated.</param>
    /// <returns>A domain validation error for an invalid location code.</returns>
    public static DomainError LocationCodeInvalid(string fieldName) =>
        DomainError.Validation("LocationCode.Invalid", $"{fieldName} must be a 5-character UN/LOCODE (2-letter country followed by 3 letters or digits 2-9), e.g. 'DKCPH'.");

    /// <summary>
    /// Creates a validation error indicating that a country code does not match the expected ISO 3166-1 alpha-2 format.
    /// </summary>
    /// <param name="fieldName">The name of the field being validated.</param>
    /// <returns>A domain validation error for an invalid country code.</returns>
    public static DomainError CountryCodeInvalid(string fieldName) =>
        DomainError.Validation("CountryCode.Invalid", $"{fieldName} must be a 2-letter ISO 3166-1 alpha-2 country code, e.g. 'DK'.");

    /// <summary>
    /// Creates a validation error indicating that a location filter value is neither a valid ISO country code nor a valid UN/LOCODE.
    /// </summary>
    /// <param name="fieldName">The name of the field being validated.</param>
    /// <returns>A domain validation error for an invalid location filter.</returns>
    public static DomainError LocationFilterInvalid(string fieldName) =>
        DomainError.Validation("LocationFilter.Invalid", $"{fieldName} must be either a 2-letter ISO country code (e.g. 'DK') or a 5-character UN/LOCODE (e.g. 'DKCPH').");
}
