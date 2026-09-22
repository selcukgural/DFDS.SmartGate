using System.Buffers;
using System.Security.Claims;
using DFDS.SmartGate.Application.Abstractions;
using DFDS.SmartGate.Domain.Locations;

namespace DFDS.SmartGate.Api.Auth;

/// <summary>
/// <see cref="ICallerContext"/> resolved once per request from the authenticated <see cref="ClaimsPrincipal"/>.
/// The <c>terminal</c> claim may be repeated (JSON array) or hold several codes separated by whitespace or commas
/// (the <c>scope</c>-style layout some identity providers emit). Values that are not well-formed UN/LOCODEs are
/// ignored (fail closed: the caller simply lacks that terminal).
/// </summary>
public sealed class CallerContext : ICallerContext
{
    private static readonly SearchValues<char> Separators = SearchValues.Create(" ,;\t");

    private readonly LocationCode[] _terminals;

    private CallerContext(string subject, LocationCode[] terminals)
    {
        Subject = subject;
        _terminals = terminals;
    }

    /// <inheritdoc/>
    public string Subject { get; }

    /// <inheritdoc/>
    public IReadOnlyList<LocationCode> Terminals => _terminals;

    /// <summary>Builds the context from a principal that passed the <see cref="AuthorizationPolicies.VisitAccess"/> policy.</summary>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The caller context.</returns>
    /// <exception cref="InvalidOperationException">The principal carries neither <c>sub</c> nor <c>client_id</c>; the policy should have rejected it.</exception>
    public static CallerContext FromPrincipal(ClaimsPrincipal principal)
    {
        var subject = CallerClaims.ResolveSubject(principal)
            ?? throw new InvalidOperationException("The caller has no subject claim; the authorization policy must reject such tokens.");

        return new CallerContext(subject, ResolveTerminals(principal));
    }

    /// <inheritdoc/>
    public bool CanAccess(LocationCode terminal)
    {
        return _terminals.Contains(terminal);
    }

    private static LocationCode[] ResolveTerminals(ClaimsPrincipal principal)
    {
        List<LocationCode>? terminals = null;

        foreach (var claim in principal.Claims)
        {
            if (!string.Equals(claim.Type, CallerClaims.Terminal, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var range in claim.Value.AsSpan().SplitAny(Separators))
            {
                var parsed = LocationCode.Create(claim.Value[range]);

                if (parsed.IsFailure)
                {
                    continue;
                }

                terminals ??= [];

                if (!terminals.Contains(parsed.Value))
                {
                    terminals.Add(parsed.Value);
                }
            }
        }

        return terminals is null ? [] : [.. terminals];
    }
}
