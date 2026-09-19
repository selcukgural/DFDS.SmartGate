using DFDS.SmartGate.Domain.Common;

namespace DFDS.SmartGate.Domain.Identifiers;

/// <summary>
/// Represents the identifier of a transport unit, such as a truck, trailer, or container.
/// The value is stored in uppercase without whitespace, as required by the case brief.
/// </summary>
/// <remarks>
/// This is a value object modeled as a readonly record struct so it remains immutable,
/// lightweight, and easy to compare by value.
/// </remarks>
public readonly record struct UnitNumber
{
    /// <summary>
    /// Maximum allowed number of characters for a unit number.
    /// </summary>
    public const int MaxLength = 20;

    /// <summary>
    /// The display name used in validation and error messages.
    /// </summary>
    private const string Name = "UnitNumber";

    /// <summary>
    /// Initializes a new <see cref="UnitNumber"/> instance with a validated value.
    /// </summary>
    /// <param name="value">The normalized unit number value.</param>
    private UnitNumber(string value) => Value = value;

    /// <summary>
    /// Gets the underlying normalized unit number value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="UnitNumber"/> from a raw input value.
    /// </summary>
    /// <param name="raw">
    /// The raw candidate value. It may be null, empty, contain whitespace, or include invalid characters.
    /// </param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the created unit number when validation succeeds;
    /// otherwise, a domain error describing the specific validation failure.
    /// </returns>
    public static Result<UnitNumber> Create(string? raw)
    {
        // Normalize whitespace and casing according to the identifier rules.
        var normalized = IdentifierNormalizer.Normalize(raw);

        switch (normalized.Length)
        {
            case 0:
                return IdentifierErrors.Empty(Name);
            case > MaxLength:
                return IdentifierErrors.TooLong(Name, MaxLength);
        }

        // Ensure the value contains only valid identifier characters.
        if (!IdentifierNormalizer.IsValid(normalized))
        {
            return IdentifierErrors.InvalidCharacters(Name);
        }

        return new UnitNumber(normalized);
    }

    /// <summary>
    /// Returns the string representation of the unit number.
    /// </summary>
    /// <returns>The normalized unit number value.</returns>
    public override string ToString() => Value;
}