using DFDS.SmartGate.Domain.Locations;

namespace DFDS.SmartGate.Application.Abstractions;

/// <summary>
/// Identity and terminal entitlements of the caller of the current request, resolved by the host from the bearer token.
/// Handlers use it for terminal-scoped authorization; the host guarantees the caller is authenticated before a handler runs.
/// </summary>
public interface ICallerContext
{
    /// <summary>Stable subject of the caller's token (<c>sub</c>, falling back to <c>client_id</c>). Recorded as <c>createdBy</c> / <c>changedBy</c>; never personal data.</summary>
    string Subject { get; }

    /// <summary>Terminals the caller may read and write. Empty when the token carries no terminal claim.</summary>
    IReadOnlyList<LocationCode> Terminals { get; }

    /// <summary>Checks whether the caller is entitled to <paramref name="terminal"/>.</summary>
    /// <param name="terminal">The terminal being accessed.</param>
    /// <returns><see langword="true"/> when <paramref name="terminal"/> is in <see cref="Terminals"/>.</returns>
    bool CanAccess(LocationCode terminal);
}
