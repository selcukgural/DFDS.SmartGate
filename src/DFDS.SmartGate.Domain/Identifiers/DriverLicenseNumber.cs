using DFDS.SmartGate.Domain.Common;

namespace DFDS.SmartGate.Domain.Identifiers;

/// <summary>
/// Represents a normalized driving license number used for exact-match lookups.
/// </summary>
public readonly record struct DriverLicenseNumber
{
    /// <summary>
    /// The maximum allowed length of a driving license number value.
    /// </summary>
    public const int MaxLength = 32;

    /// <summary>
    /// The identifier name used in validation and error reporting.
    /// </summary>
    private const string Name = "DriverLicenseNumber";

    /// <summary>
    /// Initializes a new instance of the <see cref="DriverLicenseNumber"/> record with the normalized value.
    /// </summary>
    /// <param name="value">The normalized driving license number value.</param>
    private DriverLicenseNumber(string value) => Value = value;

    /// <summary>
    /// Gets the normalized driving license number value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a valid <see cref="DriverLicenseNumber"/> from the provided raw input.
    /// </summary>
    /// <param name="raw">The raw driving license number to normalize and validate.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the created value when the input is valid; otherwise, the validation error.
    /// </returns>
    public static Result<DriverLicenseNumber> Create(string? raw)
    {
        var normalized = IdentifierNormalizer.Normalize(raw);

        switch (normalized.Length)
        {
            case 0:
                return IdentifierErrors.Empty(Name);
            case > MaxLength:
                return IdentifierErrors.TooLong(Name, MaxLength);
        }

        if (!IdentifierNormalizer.IsValid(normalized))
        {
            return IdentifierErrors.InvalidCharacters(Name);
        }

        return new DriverLicenseNumber(normalized);
    }

    /// <summary>
    /// Returns the underlying normalized driving license number string.
    /// </summary>
    /// <returns>The driving license number value.</returns>
    public override string ToString() => Value;
}
