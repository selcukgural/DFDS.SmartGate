using DFDS.SmartGate.Application.Abstractions;
using DFDS.SmartGate.Domain.Visits;
using DFDS.SmartGate.IntegrationTests.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DFDS.SmartGate.IntegrationTests.Persistence;

/// <summary>Optimistic concurrency on the visit row (PostgreSQL <c>xmin</c>): the second of two racing transitions loses.</summary>
[Collection(ApiHost.Name)]
public sealed class ConcurrencyTests(ApiFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ConcurrentTransitions_SecondWriterGetsConflict_AndNothingIsLost()
    {
        using var client = fixture.CreateClient(fixture.Tokens.For("gate-1", "DKCPH"));
        var visit = await VisitApi.CreateAsync(client, "DKCPH", _ct);

        await using var firstScope = fixture.Services.CreateAsyncScope();
        await using var secondScope = fixture.Services.CreateAsyncScope();
        var first = await LoadAsync(firstScope);
        var second = await LoadAsync(secondScope);

        Assert.True(first.TransitionTo(VisitStatus.AtGate, "gate-1", "first", DateTimeOffset.UtcNow).IsSuccess);
        Assert.True(second.TransitionTo(VisitStatus.AtGate, "gate-2", "second", DateTimeOffset.UtcNow).IsSuccess);

        var firstSave = await firstScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(_ct);
        var secondSave = await secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(_ct);

        Assert.True(firstSave.IsSuccess);
        Assert.True(secondSave.IsFailure);
        Assert.Equal(VisitErrors.ConcurrentUpdate.Code, secondSave.Error.Value.Code);

        var stored = await VisitApi.GetAsync(client, visit.Id, _ct);
        Assert.Equal(VisitStatus.AtGate, stored.CurrentStatus);
        Assert.Equal(2, stored.StatusHistory.Count);
        Assert.Equal("first", stored.StatusHistory[^1].Reason);

        async Task<Visit> LoadAsync(AsyncServiceScope scope)
        {
            var loaded = await scope.ServiceProvider.GetRequiredService<IVisitRepository>().GetByIdAsync(visit.Id, _ct);
            Assert.NotNull(loaded);
            return loaded;
        }
    }
}
