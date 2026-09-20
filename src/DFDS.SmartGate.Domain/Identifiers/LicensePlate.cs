using DFDS.SmartGate.Domain.Common;

namespace DFDS.SmartGate.Domain.Identifiers;

/// <summary>
/// Vehicle registration plate. Stored capitalised without whitespace as required by the case brief.
/// </summary>
public readonly record struct LicensePlate
{
    /// <summary>
    /// The maximum allowed length of a normalized license plate value.
    /// </summary>
    public const int MaxLength = 20;
    private const string Name = "LicensePlate";

    /// <summary>
    /// Initializes a new license plate instance with a normalized value.
    /// </summary>
    /// <param name="value">The normalized vehicle registration value.</param>
    private LicensePlate(string value) => Value = value;

    /// <summary>
    /// Gets the canonical registration plate value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a license plate from a raw string input after normalization and validation.
    /// </summary>
    /// <param name="raw">The raw plate value supplied by the caller.</param>
    /// <returns>
    /// A successful result containing the validated plate, or a failure result describing the validation issue.
    /// </returns>
    public static Result<LicensePlate> Create(string? raw)
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

        return new LicensePlate(normalized);
    }

    /// <summary>
    /// Returns the underlying registration plate value.
    /// </summary>
    /// <returns>The license plate text.</returns>
    public override string ToString() => Value;
}
