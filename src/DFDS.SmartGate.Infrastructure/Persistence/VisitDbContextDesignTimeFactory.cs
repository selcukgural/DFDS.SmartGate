using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DFDS.SmartGate.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> build the context without the Api host (<c>dotnet ef migrations add ... --project src/DFDS.Claude.Infrastructure</c>).
/// The connection string comes from <c>ConnectionStrings__Visits</c>; adding a migration needs none, so a local
/// placeholder is used when the variable is absent.
/// </summary>
public sealed class VisitDbContextDesignTimeFactory : IDesignTimeDbContextFactory<VisitDbContext>
{
    /// <summary>Environment variable read for <c>dotnet ef database update</c>.</summary>
    public const string ConnectionStringVariable = "ConnectionStrings__Visits";

    /// <inheritdoc/>
    public VisitDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable)
            ?? "Host=localhost;Database=dfds_visits";

        var options = new DbContextOptionsBuilder<VisitDbContext>()
            .UseNpgsql(connectionString, DependencyInjection.ConfigureNpgsql)
            .Options;

        return new VisitDbContext(options);
    }
}
