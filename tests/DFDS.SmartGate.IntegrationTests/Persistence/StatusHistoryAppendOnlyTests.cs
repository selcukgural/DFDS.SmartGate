using DFDS.SmartGate.Infrastructure.Persistence;
using DFDS.SmartGate.IntegrationTests.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace DFDS.SmartGate.IntegrationTests.Persistence;

/// <summary>
/// The audit trail is immutable at the database, not only in the aggregate: the trigger installed by the initial
/// migration rejects any UPDATE or DELETE on <c>visit_status_history</c>, even from a privileged connection.
/// </summary>
[Collection(ApiHost.Name)]
public sealed class StatusHistoryAppendOnlyTests(ApiFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Update_IsRejected()
    {
        var visitId = await CreateVisitAsync();
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<VisitDbContext>();

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlAsync($"UPDATE visit_status_history SET reason = 'tampered' WHERE visit_id = {visitId}", _ct));

        Assert.Equal("23000", error.SqlState);
        Assert.Contains("append-only", error.MessageText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Delete_IsRejected()
    {
        var visitId = await CreateVisitAsync();
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<VisitDbContext>();

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlAsync($"DELETE FROM visit_status_history WHERE visit_id = {visitId}", _ct));

        Assert.Equal("23000", error.SqlState);

        var remaining = await context.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM visit_status_history WHERE visit_id = {visitId}")
            .SingleAsync(_ct);
        Assert.Equal(1, remaining);
    }

    [Fact]
    public async Task Insert_ThroughTheAggregate_StillWorks()
    {
        var visitId = await CreateVisitAsync();
        using var client = fixture.CreateClient(fixture.Tokens.For("gate-1", "DKCPH"));
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<VisitDbContext>();

        await VisitApi.UpdateStatusAsync(client, visitId, Domain.Visits.VisitStatus.AtGate, _ct);

        var rows = await context.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM visit_status_history WHERE visit_id = {visitId}")
            .SingleAsync(_ct);
        Assert.Equal(2, rows);
    }

    private async Task<Guid> CreateVisitAsync()
    {
        using var client = fixture.CreateClient(fixture.Tokens.For("gate-1", "DKCPH"));
        return (await VisitApi.CreateAsync(client, "DKCPH", _ct)).Id;
    }
}
