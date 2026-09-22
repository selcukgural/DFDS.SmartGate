using System.Text.Json.Serialization;
using DFDS.SmartGate.Api.OpenApi;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace DFDS.SmartGate.Api.Http;

/// <summary>Registers the HTTP-facing conventions: JSON, ProblemDetails, OpenAPI and Kestrel limits.</summary>
public static class HttpSetup
{
    /// <summary>
    /// Largest request body the API accepts. A create request with a handful of movements is well under 10 KB; anything
    /// larger is not a legitimate visit and is rejected with 413 before deserialisation.
    /// </summary>
    public const long MaxRequestBodyBytes = 64 * 1024;

    /// <summary>
    /// Adds JSON options (enum names on the wire, case-insensitive), ProblemDetails for every error path including
    /// framework-generated 400/401/404s (binding failures are surfaced as exceptions in every environment and mapped by
    /// <see cref="BadRequestExceptionHandler"/>), and the OpenAPI document (served in Development only).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddApiHttp(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ConfigureHttpJsonOptions(static options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.Configure<RouteHandlerOptions>(static options => options.ThrowOnBadRequest = true);
        services.AddExceptionHandler<BadRequestExceptionHandler>();

        services.AddProblemDetails(static options => options.CustomizeProblemDetails = static context =>
        {
            if (CorrelationIdMiddleware.Get(context.HttpContext) is { } correlationId)
            {
                context.ProblemDetails.Extensions["correlationId"] = correlationId;
            }
        });

        services.AddOpenApi(static options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        return services;
    }

    /// <summary>Hardens Kestrel: no <c>Server</c> header and a small request-body limit.</summary>
    /// <param name="options">Kestrel options.</param>
    public static void ConfigureKestrel(KestrelServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddServerHeader = false;
        options.Limits.MaxRequestBodySize = MaxRequestBodyBytes;
    }
}
