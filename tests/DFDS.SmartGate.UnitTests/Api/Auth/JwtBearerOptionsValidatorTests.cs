using System.Security.Cryptography;
using DFDS.SmartGate.Api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace DFDS.SmartGate.UnitTests.Api.Auth;

public sealed class JwtBearerOptionsValidatorTests
{
    private sealed class Environment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static JwtBearerOptionsValidator Validator(string environment = "Production") => new(new Environment(environment));

    private static JwtBearerOptions WithAuthority(bool https = true) => new()
    {
        Authority = "https://idp.example.com",
        RequireHttpsMetadata = https,
        TokenValidationParameters = { ValidAudiences = ["visits-api"] },
    };

    private static JwtBearerOptions WithSymmetricKey() => new()
    {
        TokenValidationParameters =
        {
            IssuerSigningKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)),
            ValidAudience = "visits-api",
        },
    };

    [Fact]
    public void OtherSchemes_AreSkipped()
    {
        Assert.True(Validator().Validate("Other", new JwtBearerOptions()).Skipped);
    }

    [Fact]
    public void AuthorityWithAudience_IsAccepted()
    {
        Assert.True(Validator().Validate(JwtBearerDefaults.AuthenticationScheme, WithAuthority()).Succeeded);
    }

    [Fact]
    public void NeitherAuthorityNorKeys_Fails()
    {
        var result = Validator().Validate(JwtBearerDefaults.AuthenticationScheme, new JwtBearerOptions { Audience = "a" });

        Assert.True(result.Failed);
        Assert.Contains("Authority", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingAudience_Fails()
    {
        var options = new JwtBearerOptions { Authority = "https://idp.example.com" };

        var result = Validator().Validate(JwtBearerDefaults.AuthenticationScheme, options);

        Assert.True(result.Failed);
        Assert.Contains("audience", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DisabledAudienceValidation_NeedsNoAudience()
    {
        var options = new JwtBearerOptions { Authority = "https://idp.example.com", TokenValidationParameters = { ValidateAudience = false } };

        Assert.True(Validator().Validate(JwtBearerDefaults.AuthenticationScheme, options).Succeeded);
    }

    [Fact]
    public void PlainHttpMetadata_FailsOutsideDevelopment()
    {
        Assert.True(Validator().Validate(JwtBearerDefaults.AuthenticationScheme, WithAuthority(https: false)).Failed);
        Assert.True(Validator("Development").Validate(JwtBearerDefaults.AuthenticationScheme, WithAuthority(https: false)).Succeeded);
    }

    [Fact]
    public void SymmetricKey_IsDevelopmentOnly()
    {
        var production = Validator().Validate(JwtBearerDefaults.AuthenticationScheme, WithSymmetricKey());
        var development = Validator("Development").Validate(JwtBearerDefaults.AuthenticationScheme, WithSymmetricKey());

        Assert.True(production.Failed);
        Assert.Contains("Symmetric", production.FailureMessage, StringComparison.Ordinal);
        Assert.True(development.Succeeded);
    }

    [Fact]
    public void SymmetricKeyInKeyList_IsDetected()
    {
        var options = new JwtBearerOptions
        {
            TokenValidationParameters =
            {
                IssuerSigningKeys = [new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32))],
                ValidAudiences = ["a"],
            },
        };

        Assert.True(Validator().Validate(JwtBearerDefaults.AuthenticationScheme, options).Failed);
    }
}
