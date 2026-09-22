using System.Security.Claims;

namespace DFDS.SmartGate.Api.Auth;

/// <summary>
/// Claim types the API reads from a bearer token. Inbound claim mapping is disabled, so these are the raw JWT
/// claim names. Changing the identity provider's claim layout means changing only this class.
/// </summary>
public static class CallerClaims
{
    /// <summary>Stable subject of an end user (<c>sub</c>).</summary>
    public const string Subject = "sub";

    /// <summary>Identifier of a machine client (<c>client_id</c>); used when the token carries no <see cref="Subject"/>.</summary>
    private const string ClientId = "client_id";

    /// <summary>Multi-valued claim listing the UN/LOCODEs of the terminals the caller may access, e.g. <c>["DKCPH","SEGOT"]</c>.</summary>
    public const string Terminal = "terminal";

    /// <summary>Resolves the caller's identity: <see cref="Subject"/>, falling back to <see cref="ClientId"/>.</summary>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The non-empty subject, or <see langword="null"/> when the token carries neither claim.</returns>
    public static string? ResolveSubject(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var subject = principal.FindFirstValue(Subject);

        if (!string.IsNullOrWhiteSpace(subject))
        {
            return subject;
        }

        var clientId = principal.FindFirstValue(ClientId);

        return string.IsNullOrWhiteSpace(clientId) ? null : clientId;
    }
}
