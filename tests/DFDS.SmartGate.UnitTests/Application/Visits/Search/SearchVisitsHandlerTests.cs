using System;
using System.Threading.Tasks;
using DFDS.SmartGate.Application.Visits;
using DFDS.SmartGate.Application.Visits.Models;
using DFDS.SmartGate.Application.Visits.Search;
using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.Domain.Visits;
using DFDS.SmartGate.UnitTests.Application.Fakes;
using Xunit;
using static DFDS.SmartGate.UnitTests.Application.ApplicationTestData;

namespace DFDS.SmartGate.UnitTests.Application.Visits.Search;

public sealed class SearchVisitsHandlerTests
{
    private readonly InMemoryVisitReadStore _readStore = new();

    private SearchVisitsHandler Handler(FakeCallerContext? caller = null) => new(_readStore, caller ?? Caller(), Clock());

    private static VisitSummary Summary() =>
        new(Guid.CreateVersion7(), "DKCPH", VisitStatus.AtGate, new TruckDto("TRK001", "34ABC123", null), "Ayşe Yılmaz", [], Now, Subject);

    [Fact]
    public async Task NoTimeFilter_AppliesDefaultWindow_AndEchoesIt()
    {
        var result = await Handler().HandleAsync(new SearchVisitsQuery(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(Now, result.Value.CreatedTimeTo);
        Assert.Equal(Now - SearchLimits.DefaultWindow, result.Value.CreatedTimeFrom);
        Assert.Equal(Now, _readStore.LastCriteria!.CreatedTo);
        Assert.Equal(Now - SearchLimits.DefaultWindow, _readStore.LastCriteria.CreatedFrom);
    }

    [Fact]
    public async Task OnlyTo_Given_WindowEndsThere()
    {
        var to = Now.AddDays(-10);

        var result = await Handler().HandleAsync(new SearchVisitsQuery { CreatedTimeTo = to }, TestContext.Current.CancellationToken);

        Assert.Equal(to, result.Value.CreatedTimeTo);
        Assert.Equal(to - SearchLimits.DefaultWindow, result.Value.CreatedTimeFrom);
    }

    [Fact]
    public async Task ExplicitWindow_IsPassedThrough()
    {
        var from = Now.AddDays(-90);
        var to = Now.AddDays(-60);

        var result = await Handler().HandleAsync(new SearchVisitsQuery { CreatedTimeFrom = from, CreatedTimeTo = to }, TestContext.Current.CancellationToken);

        Assert.Equal(from, result.Value.CreatedTimeFrom);
        Assert.Equal(to, result.Value.CreatedTimeTo);
    }

    [Fact]
    public async Task NoTerminal_Given_SearchIsRestrictedToCallersTerminals()
    {
        await Handler(Caller(Copenhagen, Gothenburg)).HandleAsync(new SearchVisitsQuery(), TestContext.Current.CancellationToken);

        Assert.Equal([Copenhagen, Gothenburg], _readStore.LastCriteria!.Terminals);
    }

    [Fact]
    public async Task Terminal_Given_AndAllowed_SearchIsScopedToIt()
    {
        await Handler(Caller(Copenhagen, Gothenburg)).HandleAsync(new SearchVisitsQuery { TerminalId = "segot" }, TestContext.Current.CancellationToken);

        Assert.Equal([Gothenburg], _readStore.LastCriteria!.Terminals);
    }

    [Fact]
    public async Task Terminal_Given_ButNotAllowed_IsForbidden()
    {
        var result = await Handler(Caller(Copenhagen)).HandleAsync(new SearchVisitsQuery { TerminalId = "SEGOT" }, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, result.Error.Value.Kind);
        Assert.Null(_readStore.LastCriteria);
    }

    [Fact]
    public async Task CallerWithoutTerminals_GetsEmptyPage_WithoutQueryingStore()
    {
        var result = await Handler(new FakeCallerContext(Subject)).HandleAsync(new SearchVisitsQuery(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(0, result.Value.TotalPages);
        Assert.Null(_readStore.LastCriteria);
    }

    [Fact]
    public async Task Filters_AreResolvedIntoCriteria()
    {
        var query = new SearchVisitsQuery
        {
            CurrentStatus = VisitStatus.OnSite,
            MovementFrom = "tr",
            MovementTo = "SEGOT",
            CreatedBy = "gate-1",
            Page = 2,
            PageSize = 50,
        };

        await Handler().HandleAsync(query, TestContext.Current.CancellationToken);

        var criteria = _readStore.LastCriteria!;
        Assert.Equal(VisitStatus.OnSite, criteria.Status);
        Assert.True(criteria.MovementFrom!.Value.IsCountry);
        Assert.Equal("TR", criteria.MovementFrom.Value.Country!.Value.Value);
        Assert.False(criteria.MovementTo!.Value.IsCountry);
        Assert.Equal("SEGOT", criteria.MovementTo.Value.Location!.Value.Value);
        Assert.Equal("gate-1", criteria.CreatedBy);
        Assert.Equal(2, criteria.Page);
        Assert.Equal(50, criteria.PageSize);
        Assert.Equal(50, criteria.Skip);
    }

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(250, 100, 3)]
    public async Task PagingMetadata_IsComputedFromTotalCount(int totalCount, int pageSize, int expectedPages)
    {
        _readStore.SearchResult = new PagedItems<VisitSummary>([Summary()], totalCount);

        var result = await Handler().HandleAsync(new SearchVisitsQuery { PageSize = pageSize }, TestContext.Current.CancellationToken);

        Assert.Equal(totalCount, result.Value.TotalCount);
        Assert.Equal(expectedPages, result.Value.TotalPages);
        Assert.Equal(pageSize, result.Value.PageSize);
        Assert.Equal(1, result.Value.Page);
        Assert.Single(result.Value.Items);
    }
}
