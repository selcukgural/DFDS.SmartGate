using DFDS.SmartGate.Domain.Common;

namespace DFDS.SmartGate.Domain.Locations;

/// <summary>
/// A search criterion that is either a whole country (2-letter code) or a single location (5-letter UN/LOCODE),
/// decided by the length of the supplied value. Used by the <c>movementFrom</c> / <c>movementTo</c> query parameters.
/// </summary>
public readonly record struct LocationFilter
{
    /// <summary>
    /// Initializes a location filter with either a country code or a location code.
    /// </summary>
    /// <param name="country">The country code when the filter represents a country; otherwise, <see langword="null"/>.</param>
    /// <param name="location">The location code when the filter represents a single location; otherwise, <see langword="null"/>.</param>
    private LocationFilter(CountryCode? country, LocationCode? location)
    {
        Country = country;
        Location = location;
    }

    /// <summary>
    /// Gets the country part of the filter, when the value represents a whole country.
    /// </summary>
    public CountryCode? Country { get; }

    /// <summary>
    /// Gets the location part of the filter, when the value represents a single location.
    /// </summary>
    public LocationCode? Location { get; }

    /// <summary>
    /// Gets a value indicating whether the filter contains a country code instead of a location code.
    /// </summary>
    public bool IsCountry => Country is not null;

    /// <summary>
    /// Creates a validated location filter from a raw input value.
    /// </summary>
    /// <param name="raw">The raw filter value to validate. It must be either a 2-letter country code or a 5-letter UN/LOCODE.</param>
    /// <param name="fieldName">The field or parameter name used in validation errors.</param>
    /// <returns>
    /// A successful result containing the parsed filter when the input is valid; otherwise, a validation error result.
    /// </returns>
    public static Result<LocationFilter> Create(string? raw, string fieldName = nameof(LocationFilter))
    {
        var trimmedLength = raw.AsSpan().Trim().Length;

        switch (trimmedLength)
        {
            case CountryCode.Length:
            {
                var country = CountryCode.Create(raw, fieldName);
                return country.IsSuccess ? new LocationFilter(country.Value, null) : LocationErrors.LocationFilterInvalid(fieldName);
            }
            case LocationCode.Length:
            {
                var location = LocationCode.Create(raw, fieldName);
                return location.IsSuccess ? new LocationFilter(null, location.Value) : LocationErrors.LocationFilterInvalid(fieldName);
            }
            default:
                return LocationErrors.LocationFilterInvalid(fieldName);
        }
    }
}
