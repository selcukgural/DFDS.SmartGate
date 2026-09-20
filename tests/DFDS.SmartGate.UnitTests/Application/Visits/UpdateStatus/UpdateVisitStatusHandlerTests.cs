using System;
using System.Threading.Tasks;
using DFDS.SmartGate.Application.Visits;
using DFDS.SmartGate.Application.Visits.Models;
using DFDS.SmartGate.Application.Visits.UpdateStatus;
using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.Domain.Visits;
using DFDS.SmartGate.UnitTests.Application.Fakes;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static DFDS.SmartGate.UnitTests.Application.ApplicationTestData;

namespace DFDS.SmartGate.UnitTests.Application.Visits.UpdateStatus;

public sealed class UpdateVisitStatusHandlerTests
{
    private readonly InMemoryVisitRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly HybridCache _cache = TestCache.Create();

    private UpdateVisitStatusHandler Handler(FakeCallerContext? caller = null) =>
        new(_repository, _unitOfWork, caller ?? Caller(), _cache, Clock(), NullLogger<UpdateVisitStatusHandler>.Instance);

    private static UpdateVisitStatusCommand Command(Guid id, VisitStatus status, string? reason = null) =>
        new() { VisitId = id, Status = status, Reason = reason };

    [Fact]
    public async Task ValidTransition_AppendsHistory_Saves_AndReturnsUpdatedVisit()
    {
        var visit = NewVisit();
        _repository.Add(visit);

        var result = await Handler().HandleAsync(Command(visit.Id, VisitStatus.AtGate, "Lane 3"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(VisitStatus.AtGate, result.Value.CurrentStatus);
        Assert.Equal(2, result.Value.StatusHistory.Count);
        Assert.Equal("Lane 3", result.Value.StatusHistory[1].Reason);
        Assert.Equal(Subject, result.Value.StatusHistory[1].ChangedBy);
        Assert.Equal(Now, result.Value.StatusHistory[1].ChangedTime);
        Assert.Equal(1, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task SuccessfulTransition_EvictsCachedVisit()
    {
        var visit = NewVisit();
        _repository.Add(visit);
        var key = VisitCacheKeys.ById(visit.Id);
        await _cache.SetAsync(key, Response(visit.Id), cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(await _cache.GetOrCreateAsync<VisitResponse?>(key, _ => ValueTask.FromResult<VisitResponse?>(null), cancellationToken: TestContext.Current.CancellationToken));

        await Handler().HandleAsync(Command(visit.Id, VisitStatus.AtGate), TestContext.Current.CancellationToken);

        var afterEviction = await _cache.GetOrCreateAsync<VisitResponse?>(key, _ => ValueTask.FromResult<VisitResponse?>(null), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(afterEviction);
    }

    [Fact]
    public async Task UnknownVisit_IsNotFound()
    {
        var result = await Handler().HandleAsync(Command(Guid.CreateVersion7(), VisitStatus.AtGate), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error.Value.Kind);
        Assert.Equal(0, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task VisitOfAnotherTerminal_IsNotFound_NotForbidden()
    {
        var visit = NewVisit(Gothenburg);
        _repository.Add(visit);

        var result = await Handler(Caller(Copenhagen)).HandleAsync(Command(visit.Id, VisitStatus.AtGate), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error.Value.Kind);
        Assert.Equal(VisitStatus.PreRegistered, visit.CurrentStatus);
    }

    [Fact]
    public async Task InvalidTransition_IsConflict_AndNothingIsSaved()
    {
        var visit = NewVisit();
        _repository.Add(visit);

        var result = await Handler().HandleAsync(Command(visit.Id, VisitStatus.Completed), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Conflict, result.Error.Value.Kind);
        Assert.Equal("Visit.InvalidTransition", result.Error.Value.Code);
        Assert.Equal(0, _unitOfWork.SaveCalls);
        Assert.Single(visit.StatusHistory);
    }

    [Fact]
    public async Task ConcurrentUpdate_IsConflict_AndCacheIsLeftAlone()
    {
        var visit = NewVisit();
        _repository.Add(visit);
        _unitOfWork.NextResult = VisitErrors.ConcurrentUpdate;
        var key = VisitCacheKeys.ById(visit.Id);
        await _cache.SetAsync(key, Response(visit.Id), cancellationToken: TestContext.Current.CancellationToken);

        var result = await Handler().HandleAsync(Command(visit.Id, VisitStatus.AtGate), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Visit.ConcurrentUpdate", result.Error.Value.Code);
        Assert.NotNull(await _cache.GetOrCreateAsync<VisitResponse?>(key, _ => ValueTask.FromResult<VisitResponse?>(null), cancellationToken: TestContext.Current.CancellationToken));
    }
}
