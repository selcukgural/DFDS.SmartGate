using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DFDS.SmartGate.Api.Auth;

/// <summary>
/// Fails start-up when the bearer scheme is not configured well enough to ever accept a token, instead of letting
/// every request die with a silent 401. Production additionally refuses shared-secret (symmetric) signing keys and
/// plain-HTTP metadata: tokens must come from an identity provider reachable over TLS.
/// </summary>
/// <param name="environment">Host environment; relaxations apply to <c>Development</c> only.</param>
public sealed class JwtBearerOptionsValidator(IHostEnvironment environment) : IValidateOptions<JwtBearerOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Skip;
        }

        var parameters = options.TokenValidationParameters;
        var hasAuthority = !string.IsNullOrWhiteSpace(options.Authority);
        var hasStaticKeys = parameters.IssuerSigningKey is not null || HasAny(parameters.IssuerSigningKeys);

        if (!hasAuthority && !hasStaticKeys)
        {
            return ValidateOptionsResult.Fail(
                "Bearer authentication is not configured: set Authentication:Schemes:Bearer:Authority (identity provider) " +
                "or, for local development, run 'dotnet user-jwts create'.");
        }

        if (!HasAudience(options, parameters))
        {
            return ValidateOptionsResult.Fail(
                "Bearer authentication requires an audience: set Authentication:Schemes:Bearer:ValidAudiences.");
        }

        if (environment.IsDevelopment())
        {
            return ValidateOptionsResult.Success;
        }

        if (hasAuthority && !options.RequireHttpsMetadata)
        {
            return ValidateOptionsResult.Fail("Authentication:Schemes:Bearer:RequireHttpsMetadata must be true outside Development.");
        }

        if (parameters.IssuerSigningKey is SymmetricSecurityKey || ContainsSymmetricKey(parameters.IssuerSigningKeys))
        {
            return ValidateOptionsResult.Fail(
                "Symmetric signing keys (dotnet user-jwts) are allowed in Development only; configure an identity provider Authority.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool HasAudience(JwtBearerOptions options, TokenValidationParameters parameters)
    {
        if (!parameters.ValidateAudience)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(options.Audience)
            || !string.IsNullOrWhiteSpace(parameters.ValidAudience)
            || HasAny(parameters.ValidAudiences);
    }

    private static bool HasAny<T>(IEnumerable<T>? items)
    {
        return items is not null && items.Any();
    }

    private static bool ContainsSymmetricKey(IEnumerable<SecurityKey>? keys)
    {
        return keys is not null && keys.Any(e => e is SymmetricSecurityKey);
    }
}
