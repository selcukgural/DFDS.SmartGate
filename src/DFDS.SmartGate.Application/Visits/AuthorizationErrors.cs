using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.Domain.Locations;

namespace DFDS.SmartGate.Application.Visits;

/// <summary>Errors raised when a caller's terminal entitlements do not cover the requested operation.</summary>
public static class AuthorizationErrors
{
    /// <summary>The caller's token does not grant access to <paramref name="terminal"/>.</summary>
    /// <param name="terminal">The terminal that was requested.</param>
    /// <returns>A forbidden error naming the terminal (the caller supplied it, so echoing it leaks nothing).</returns>
    public static DomainError TerminalAccessDenied(LocationCode terminal) =>
        DomainError.Forbidden("Terminal.AccessDenied", $"You are not authorised for terminal '{terminal}'.");
}
