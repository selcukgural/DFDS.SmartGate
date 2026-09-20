using System;
using System.Threading.Tasks;
using DFDS.SmartGate.Application.Visits.GetById;
using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.UnitTests.Application.Fakes;
using Microsoft.Extensions.Caching.Hybrid;
using Xunit;
using static DFDS.SmartGate.UnitTests.Application.ApplicationTestData;

namespace DFDS.SmartGate.UnitTests.Application.Visits.GetById;

public sealed class GetVisitHandlerTests
{
    private readonly InMemoryVisitReadStore _readStore = new();
    private readonly HybridCache _cache = TestCache.Create();

    private GetVisitHandler Handler(FakeCallerContext? caller = null) => new(_readStore, _cache, caller ?? Caller());

    [Fact]
    public async Task ExistingVisit_IsReturned_AndSecondCallIsServedFromCache()
    {
        var visit = Response();
        _readStore.Add(visit);
        var handler = Handler();

        var first = await handler.HandleAsync(new GetVisitQuery(visit.Id), TestContext.Current.CancellationToken);
        var second = await handler.HandleAsync(new GetVisitQuery(visit.Id), TestContext.Current.CancellationToken);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(visit, second.Value);
        Assert.Equal(1, _readStore.GetByIdCalls);
    }

    [Fact]
    public async Task UnknownVisit_IsNotFound()
    {
        var result = await Handler().HandleAsync(new GetVisitQuery(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error.Value.Kind);
    }

    [Fact]
    public async Task VisitOfAnotherTerminal_IsNotFound_EvenWhenCached()
    {
        var visit = Response(terminalId: "SEGOT");
        _readStore.Add(visit);
        Assert.True((await Handler(Caller(Gothenburg)).HandleAsync(new GetVisitQuery(visit.Id), TestContext.Current.CancellationToken)).IsSuccess);

        var result = await Handler(Caller(Copenhagen)).HandleAsync(new GetVisitQuery(visit.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error.Value.Kind);
        Assert.Equal(1, _readStore.GetByIdCalls);
    }
}
