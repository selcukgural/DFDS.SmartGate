using DFDS.SmartGate.Domain.Visits;

namespace DFDS.SmartGate.Application.Visits;

/// <summary>
/// Parses the wire representation of <see cref="VisitStatus"/> used in query strings (<c>PreRegistered</c>,
/// <c>AtGate</c>, <c>OnSite</c>, <c>Completed</c>), case-insensitively like the JSON converter does for request
/// bodies. Numeric values are rejected so the enum's storage numbers never become part of the contract.
/// </summary>
public static class VisitStatusNames
{
    /// <summary>Comma-separated list of accepted names, for validation messages.</summary>
    public static readonly string AllowedValues = string.Join(", ", Enum.GetNames<VisitStatus>());

    /// <summary>Tries to parse a status name.</summary>
    /// <param name="raw">The client-supplied value.</param>
    /// <param name="status">The parsed status when the method returns <see langword="true"/>.</param>
    /// <returns>Whether <paramref name="raw"/> is a known status name (any casing, surrounding whitespace ignored).</returns>
    public static bool TryParse(string? raw, out VisitStatus status)
    {
        var trimmed = raw.AsSpan().Trim();

        if (trimmed.IsEmpty || char.IsAsciiDigit(trimmed[0]) || trimmed[0] == '-')
        {
            status = default;
            return false;
        }

        return Enum.TryParse(trimmed, ignoreCase: true, out status) && Enum.IsDefined(status);
    }

    /// <summary>Parses a status name that has already passed validation.</summary>
    /// <param name="raw">The validated value.</param>
    /// <returns>The status.</returns>
    /// <exception cref="ArgumentException"><paramref name="raw"/> is not a status name; indicates a missing validator rule.</exception>
    public static VisitStatus Parse(string? raw) =>
        TryParse(raw, out var status) ? status : throw new ArgumentException($"'{raw}' is not a visit status.", nameof(raw));
}
