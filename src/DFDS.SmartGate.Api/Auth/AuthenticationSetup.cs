using DFDS.SmartGate.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace DFDS.SmartGate.Api.Auth;

/// <summary>Wires OAuth2 / JWT bearer authentication, the authorization policies and the per-request caller context.</summary>
public static class AuthenticationSetup
{
    /// <summary>
    /// Registers the <c>Bearer</c> scheme. All token settings are bound from configuration section
    /// <c>Authentication:Schemes:Bearer</c> (<c>Authority</c>, <c>ValidAudiences</c>, <c>ValidIssuer</c>, …), which
    /// production supplies through environment variables and local development through <c>dotnet user-jwts</c>
    /// (user-secrets). Nothing secret lives in <c>appsettings*.json</c>. The configuration is validated on start-up by
    /// <see cref="JwtBearerOptionsValidator"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(static options =>
            {
                // Keep the raw JWT claim names (sub, client_id, terminal) instead of the legacy SOAP-style mapping.
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = CallerClaims.Subject;
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(1);
            });

        services.AddSingleton<IValidateOptions<JwtBearerOptions>, JwtBearerOptionsValidator>();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).ValidateOnStart();

        services.AddApiAuthorization();

        services.AddHttpContextAccessor();
        services.AddScoped<ICallerContext>(static provider =>
        {
            var httpContext = provider.GetRequiredService<IHttpContextAccessor>().HttpContext
                ?? throw new InvalidOperationException("ICallerContext is only available while handling an HTTP request.");

            return CallerContext.FromPrincipal(httpContext.User);
        });

        return services;
    }
}
