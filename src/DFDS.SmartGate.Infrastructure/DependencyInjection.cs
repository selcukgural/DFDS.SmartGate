using DFDS.SmartGate.Application.Abstractions;
using DFDS.SmartGate.Application.Visits;
using DFDS.SmartGate.Domain.Visits;
using DFDS.SmartGate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace DFDS.SmartGate.Infrastructure;

/// <summary>
/// Registers the Infrastructure layer: PostgreSQL persistence behind the Application ports, the cache and the
/// readiness health check. Configuration keys (never committed with real values — use user-secrets or environment
/// variables): <c>ConnectionStrings:Visits</c> (required) and <c>ConnectionStrings:Redis</c> (optional).
/// </summary>
public static class DependencyInjection
{
    /// <summary>Configuration key of the PostgreSQL connection string.</summary>
    private const string VisitsConnectionName = "Visits";

    /// <summary>Configuration key of the optional Redis connection string that enables the distributed cache tier.</summary>
    private const string RedisConnectionName = "Redis";

    /// <summary>Health-check name reported for the database; tagged <c>ready</c> for the readiness probe.</summary>
    private const string DatabaseHealthCheckName = "postgres";

    /// <summary>
    /// Adds a pooled <see cref="VisitDbContext"/> with transient-fault retries, the EF implementations of
    /// <see cref="IVisitRepository"/>, <see cref="IVisitReadStore"/> and <see cref="IUnitOfWork"/>, a
    /// <c>HybridCache</c> whose distributed tier is switched on by configuring <c>ConnectionStrings:Redis</c>,
    /// and a database health check. Swapping the store means replacing the three port implementations here.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration holding the connection strings.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    /// <exception cref="InvalidOperationException"><c>ConnectionStrings:Visits</c> is not configured.</exception>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(VisitsConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{VisitsConnectionName}' is not configured.");

        services.AddDbContextPool<VisitDbContext>(options => options.UseNpgsql(connectionString, ConfigureNpgsql));

        services.AddScoped<IVisitRepository, EfVisitRepository>();
        services.AddScoped<IVisitReadStore, EfVisitReadStore>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddCaching(configuration);

        services.AddHealthChecks()
            .AddDbContextCheck<VisitDbContext>(DatabaseHealthCheckName, tags: ["ready"]);

        return services;
    }

    /// <summary>
    /// Provider options shared by the runtime registration and the design-time factory: retry on transient
    /// failures (connection drops, failovers) so a single blip does not fail a request.
    /// </summary>
    /// <param name="npgsql">The Npgsql options builder.</param>
    internal static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(2), errorCodesToAdd: null);

    /// <summary>
    /// In-process L1 is always on; when <c>ConnectionStrings:Redis</c> is present the same registration gains a
    /// Redis L2 without any code change (HybridCache picks up the <c>IDistributedCache</c>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    private static void AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var redis = configuration.GetConnectionString(RedisConnectionName);

        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redis);
        }

        services.AddHybridCache();
    }
}
