using DFDS.SmartGate.Infrastructure.Persistence;
using DFDS.SmartGate.IntegrationTests.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DFDS.SmartGate.IntegrationTests.Persistence;

/// <summary>The schema the fixture applied to an empty database is complete and matches the EF model.</summary>
[Collection(ApiHost.Name)]
public sealed class MigrationTests(ApiFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task AllMigrations_AreApplied()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<VisitDbContext>().Database;

        Assert.Empty(await database.GetPendingMigrationsAsync(_ct));
        Assert.Contains(await database.GetAppliedMigrationsAsync(_ct), m => m.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Model_HasNoChangesWithoutAMigration()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<VisitDbContext>().Database;

        // Guards against editing an entity configuration and forgetting `dotnet ef migrations add`.
        Assert.False(database.HasPendingModelChanges());
    }

    [Fact]
    public async Task ExpectedTablesAndIndexes_Exist()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<VisitDbContext>();

        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT tablename AS \"Value\" FROM pg_tables WHERE schemaname = 'public'")
            .ToListAsync(_ct);
        var indexes = await context.Database
            .SqlQueryRaw<string>("SELECT indexname AS \"Value\" FROM pg_indexes WHERE schemaname = 'public'")
            .ToListAsync(_ct);

        Assert.Superset(new HashSet<string>(["visits", "visit_movements", "visit_status_history"], StringComparer.Ordinal), tables.ToHashSet(StringComparer.Ordinal));
        Assert.Superset(
            new HashSet<string>(
            [
                "ix_visits_terminal_created",
                "ix_visits_terminal_status_created",
                "ix_visits_terminal_created_by_created",
                "ix_visit_movements_from_country",
                "ix_visit_movements_from_location",
                "ix_visit_movements_to_country",
                "ix_visit_movements_to_location",
                "ix_visit_movements_visit_sequence",
                "ix_visit_status_history_visit_changed_at",
            ], StringComparer.Ordinal),
            indexes.ToHashSet(StringComparer.Ordinal));
    }
}
