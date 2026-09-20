using DFDS.SmartGate.Domain.Common;

namespace DFDS.SmartGate.Domain.Locations;

/// <summary>
/// UN/LOCODE (e.g. <c>DKCPH</c>, <c>TRIST</c>): the language-neutral identifier used for terminals and
/// movement origins/destinations. The first two characters are the ISO country code, exposed via
/// <see cref="Country"/> so searches can match on either granularity with an exact index lookup.
/// Only the format is validated; existence against the UN/LOCODE registry is out of scope.
/// </summary>
public readonly record struct LocationCode
{
    /// <summary>
    /// The exact number of characters in a UN/LOCODE (two-letter country code followed by a three-character location code).
    /// </summary>
    public const int Length = 5;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationCode"/> value with a normalized code and its country.
    /// </summary>
    /// <param name="value">The 5-character UN/LOCODE value.</param>
    /// <param name="country">The ISO country extracted from the first two characters of the code.</param>
    private LocationCode(string value, CountryCode country)
    {
        Value = value;
        Country = country;
    }

    /// <summary>
    /// Gets the normalized UN/LOCODE value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the ISO country code represented by the first two characters of the location code.
    /// </summary>
    public CountryCode Country { get; }

    /// <summary>
    /// Validates and creates a <see cref="LocationCode"/> from the supplied input.
    /// </summary>
    /// <param name="raw">The raw location code candidate, which may contain leading or trailing whitespace.</param>
    /// <param name="fieldName">The name of the field being validated, used when producing a validation error.</param>
    /// <returns>A result containing the created location code or a validation failure.</returns>
    public static Result<LocationCode> Create(string? raw, string fieldName = nameof(LocationCode))
    {
        var trimmed = raw.AsSpan().Trim();

        if (trimmed.IsEmpty)
        {
            return LocationErrors.LocationCodeEmpty(fieldName);
        }

        if (!HasValidFormat(trimmed))
        {
            return LocationErrors.LocationCodeInvalid(fieldName);
        }

        var value = string.Create(Length, trimmed, static (destination, source) => source.ToUpperInvariant(destination));
        return new LocationCode(value, CountryCode.FromValidated(value[..CountryCode.Length]));
    }

    /// <summary>
    /// Determines whether the provided span matches the UN/LOCODE format.
    /// </summary>
    /// <param name="candidate">The candidate value to validate.</param>
    /// <returns><c>true</c> when the value is exactly 5 characters long and conforms to the UN/LOCODE pattern; otherwise <c>false</c>.</returns>
    public static bool HasValidFormat(ReadOnlySpan<char> candidate)
    {
        if (candidate.Length != Length)
        {
            return false;
        }

        if (!char.IsAsciiLetter(candidate[0]) || !char.IsAsciiLetter(candidate[1]))
        {
            return false;
        }

        for (var i = CountryCode.Length; i < Length; i++)
        {
            var c = candidate[i];

            if (!char.IsAsciiLetter(c) && !char.IsBetween(c, '2', '9'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns the normalized UN/LOCODE value as its string representation.
    /// </summary>
    /// <returns>The current location code value.</returns>
    public override string ToString() => Value;
}
