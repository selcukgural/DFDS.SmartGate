using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DFDS.SmartGate.Api.Auth;

/// <summary>Named authorization policies of the API.</summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Required by every visit endpoint: an authenticated caller whose token identifies it (<c>sub</c> or
    /// <c>client_id</c>), so that <c>createdBy</c> / <c>changedBy</c> can always be recorded. Terminal entitlement
    /// is not checked here but in the handlers, which decide between 403 and 404 per operation.
    /// </summary>
    public const string VisitAccess = "VisitAccess";

    /// <summary>Registers the policies and a fallback that denies anonymous access to any endpoint not opted out explicitly.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorizationBuilder()
            .AddPolicy(VisitAccess, static policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(static context => HasSubject(context.User)))
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    private static bool HasSubject(ClaimsPrincipal user) => CallerClaims.ResolveSubject(user) is not null;
}
