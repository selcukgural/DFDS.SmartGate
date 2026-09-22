using DFDS.SmartGate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace DFDS.SmartGate.IntegrationTests.Hosting;

/// <summary>
/// Hosts the real API (Program.cs, full middleware pipeline, EF Core + PostgreSQL) once per test collection.
/// The database is a throw-away PostgreSQL container (Testcontainers; works with Docker or Podman) unless
/// <c>SMARTGATE_TEST_DATABASE</c> points at an existing server, in which case that database is migrated and used.
/// The host runs with a non-Development environment so the production rules apply: asymmetric signing keys only,
/// HSTS, JSON logging.
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Environment name of the test host; anything but <c>Development</c> exercises the production code paths.</summary>
    public const string EnvironmentName = "Testing";

    /// <summary>Issuer of the test tokens.</summary>
    public const string Issuer = "smartgate-tests";

    /// <summary>Audience of the test tokens; also the base address so HSTS (which skips localhost) is exercised.</summary>
    public const string Audience = "https://smartgate.test";

    /// <summary>Environment variable naming an external PostgreSQL connection string to use instead of a container.</summary>
    public const string ExternalDatabaseVariable = "SMARTGATE_TEST_DATABASE";

    private readonly PostgreSqlContainer? _container;
    private string _connectionString;

    public ApiFixture()
    {
        var external = Environment.GetEnvironmentVariable(ExternalDatabaseVariable);

        if (string.IsNullOrWhiteSpace(external))
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            _connectionString = string.Empty;
        }
        else
        {
            _connectionString = external;
        }
    }

    /// <summary>Issues bearer tokens the host accepts (and a few it must reject).</summary>
    public TestTokens Tokens { get; } = new(Issuer, Audience);

    public async ValueTask InitializeAsync()
    {
        if (_container is not null)
        {
            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }

        // Migrating here doubles as the "schema applies to an empty database" check.
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<VisitDbContext>().Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>Creates a client that sends to the audience host and does not follow redirects.</summary>
    /// <param name="bearerToken">Token for the <c>Authorization</c> header, or <see langword="null"/> for anonymous calls.</param>
    public HttpClient CreateClient(string? bearerToken)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(Audience),
            AllowAutoRedirect = false,
        });

        if (bearerToken is not null)
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", bearerToken);
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);

        builder.UseSetting("ConnectionStrings:Visits", _connectionString);
        builder.UseSetting("Authentication:Schemes:Bearer:ValidIssuer", Issuer);
        builder.UseSetting("Authentication:Schemes:Bearer:ValidAudiences:0", Audience);
        builder.UseSetting("Logging:LogLevel:Default", "Warning");
        builder.UseSetting("Logging:LogLevel:Microsoft.AspNetCore.HttpLogging", "Warning");
        // Expected failures (trigger rejections, first-run migration probes) would otherwise be logged as errors.
        builder.UseSetting("Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command", "Critical");

        // The signing key cannot come from configuration (production refuses symmetric keys), so it is injected
        // the way an identity provider's discovery document would supply it.
        builder.ConfigureTestServices(services => services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options => options.TokenValidationParameters.IssuerSigningKey = Tokens.SigningKey));
    }
}

/// <summary>Shares one <see cref="ApiFixture"/> (host + database) across all test classes marked <c>[Collection(ApiHost.Name)]</c>.</summary>
[CollectionDefinition(Name)]
public sealed class ApiHost : ICollectionFixture<ApiFixture>
{
    /// <summary>Collection name.</summary>
    public const string Name = "api";
}
