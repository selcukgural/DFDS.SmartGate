using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace DFDS.SmartGate.Api.Endpoints;

/// <summary>
/// Kubernetes-style probes. <c>/health/live</c> only proves the process answers; <c>/health/ready</c> runs the checks
/// tagged <c>ready</c> (PostgreSQL) so a pod with a broken database is taken out of rotation without being restarted.
/// Both are anonymous but return a bare status word, never check details.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>Tag of checks that gate readiness.</summary>
    private const string ReadyTag = "ready";

    /// <summary>Maps the liveness and readiness probes.</summary>
    /// <param name="endpoints">The route builder.</param>
    /// <returns><paramref name="endpoints"/>, for chaining.</returns>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = static _ => false })
            .AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = static check => check.Tags.Contains(ReadyTag) })
            .AllowAnonymous();

        return endpoints;
    }
}
