using DFDS.SmartGate.Domain.Common;

namespace DFDS.SmartGate.Domain.Locations;

/// <summary>
/// ISO 3166-1 alpha-2 country code. Only the format is validated; existence is not checked (no master data).
/// </summary>
public readonly record struct CountryCode
{
    /// <summary>
    /// The expected length of a country code in characters.
    /// </summary>
    public const int Length = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountryCode"/> structure with a canonicalized value.
    /// </summary>
    /// <param name="value">The validated uppercase country code value.</param>
    private CountryCode(string value) => Value = value;

    /// <summary>
    /// Gets the canonical uppercase value of the country code.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates and creates a country code from the provided raw value.
    /// </summary>
    /// <param name="raw">The raw input value, which may include surrounding whitespace.</param>
    /// <param name="fieldName">The field name used when creating a validation error.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the created country code when valid, or a validation error when invalid.
    /// </returns>
    public static Result<CountryCode> Create(string? raw, string fieldName = nameof(CountryCode))
    {
        var trimmed = raw.AsSpan().Trim();

        if (trimmed.Length != Length || !IsAsciiUpperOrLowerLetter(trimmed[0]) || !IsAsciiUpperOrLowerLetter(trimmed[1]))
        {
            return LocationErrors.CountryCodeInvalid(fieldName);
        }

        return new CountryCode(string.Create(Length, trimmed, static (destination, source) => source.ToUpperInvariant(destination)));
    }

    /// <summary>
    /// Builds a country code from a value already validated by <see cref="LocationCode"/>.
    /// </summary>
    /// <param name="value">The already-validated country code value.</param>
    /// <returns>A country code instance wrapping the validated value.</returns>
    internal static CountryCode FromValidated(string value) => new(value);

    /// <summary>
    /// Determines whether the specified character is an ASCII letter.
    /// </summary>
    /// <param name="c">The character to validate.</param>
    /// <returns><c>true</c> if the character is an ASCII letter; otherwise, <c>false</c>.</returns>
    private static bool IsAsciiUpperOrLowerLetter(char c) => char.IsAsciiLetter(c);

    /// <summary>
    /// Returns the string representation of the country code.
    /// </summary>
    /// <returns>The canonical uppercase country code value.</returns>
    public override string ToString() => Value;
}
