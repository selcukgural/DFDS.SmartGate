using System.Globalization;
using System.Net;
using DFDS.SmartGate.Application.Visits.Search;
using DFDS.SmartGate.Domain.Visits;
using DFDS.SmartGate.IntegrationTests.Hosting;

namespace DFDS.SmartGate.IntegrationTests.Api.Visits;

/// <summary>Search filters, paging and terminal scoping against real SQL (indexes, generated country columns, EXISTS predicates).</summary>
[Collection(ApiHost.Name)]
public sealed class SearchVisitsTests(ApiFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task WithoutFilters_ReturnsCallersTerminalsOnly_NewestFirst_WithDefaultWindow()
    {
        var (mine, alsoMine, foreign) = (Terminals.Next(), Terminals.Next(), Terminals.Next());
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", mine, alsoMine));
        using var other = fixture.CreateClient(fixture.Tokens.For("bob", foreign));
        var first = await VisitApi.CreateAsync(client, mine, _ct);
        await VisitApi.CreateAsync(other, foreign, _ct);
        var second = await VisitApi.CreateAsync(client, alsoMine, _ct);
        var third = await VisitApi.CreateAsync(client, mine, _ct);

        var page = await SearchAsync(client, "", _ct);

        Assert.Equal([third.Id, second.Id, first.Id], page.Items.Select(v => v.Id));
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(1, page.TotalPages);
        Assert.Equal(SearchLimits.DefaultPage, page.Page);
        Assert.Equal(SearchLimits.DefaultPageSize, page.PageSize);
        Assert.Equal(SearchLimits.DefaultWindow, page.CreatedTimeTo - page.CreatedTimeFrom);
        Assert.InRange(page.CreatedTimeTo, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Summary_CarriesTheListingFields()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", terminal));
        var visit = await VisitApi.CreateAsync(client, terminal, _ct);

        var page = await SearchAsync(client, $"terminalId={terminal}", _ct);

        var summary = Assert.Single(page.Items);
        Assert.Equal(visit.Id, summary.Id);
        Assert.Equal(terminal, summary.TerminalId);
        Assert.Equal(VisitStatus.PreRegistered, summary.CurrentStatus);
        Assert.Equal(visit.Truck, summary.Truck);
        Assert.Equal(visit.Driver.Name, summary.DriverName);
        Assert.Equal(visit.Movements, summary.Movements);
        Assert.Equal(visit.CreatedTime, summary.CreatedTime, VisitAssert.TimePrecision);
        Assert.Equal("alice", summary.CreatedBy);
    }

    [Fact]
    public async Task TerminalId_NarrowsToThatTerminal()
    {
        var (a, b) = (Terminals.Next(), Terminals.Next());
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", a, b));
        await VisitApi.CreateAsync(client, a, _ct);
        var inB = await VisitApi.CreateAsync(client, b, _ct);

        var page = await SearchAsync(client, $"terminalId={b.ToLowerInvariant()}", _ct);

        Assert.Equal(inB.Id, Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task TerminalIdOutsideTheToken_Is403()
    {
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", "DKCPH"));

        using var response = await client.GetAsync($"{VisitApi.Route}?terminalId=SEGOT", _ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Terminal.AccessDenied", (await response.ReadProblemAsync(_ct)).Code());
    }

    [Fact]
    public async Task CurrentStatus_FiltersCaseInsensitively()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", terminal));
        await VisitApi.CreateAsync(client, terminal, _ct);
        var arrived = await VisitApi.CreateAsync(client, terminal, _ct);
        await VisitApi.UpdateStatusAsync(client, arrived.Id, VisitStatus.AtGate, _ct);

        var page = await SearchAsync(client, $"terminalId={terminal}&currentStatus=atgate", _ct);

        var summary = Assert.Single(page.Items);
        Assert.Equal(arrived.Id, summary.Id);
        Assert.Equal(VisitStatus.AtGate, summary.CurrentStatus);
    }

    [Fact]
    public async Task UnknownStatus_Is400_ListingTheAllowedValues()
    {
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", "DKCPH"));

        using var response = await client.GetAsync($"{VisitApi.Route}?currentStatus=Rejected", _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadValidationProblemAsync(_ct);
        Assert.Contains("PreRegistered, AtGate, OnSite, Completed", Assert.Single(problem.Errors["currentStatus"]), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MovementFrom_MatchesCountryOrLocation_OfDeliveries()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", terminal));
        var fromGothenburg = await VisitApi.CreateAsync(client, terminal, _ct, r => r.Movements =
            [new MovementRequest { Type = "Delivery", UnitNumber = "c1", Location = "SEGOT" }]);
        var fromStockholm = await VisitApi.CreateAsync(client, terminal, _ct, r => r.Movements =
            [new MovementRequest { Type = "Delivery", UnitNumber = "c2", Location = "SESTO" }]);
        await VisitApi.CreateAsync(client, terminal, _ct, r => r.Movements =
            [new MovementRequest { Type = "Collection", UnitNumber = "c3", Location = "SEGOT" }]); // From = terminal, not SEGOT

        var byCountry = await SearchAsync(client, $"terminalId={terminal}&movementFrom=se", _ct);
        var byLocation = await SearchAsync(client, $"terminalId={terminal}&movementFrom=SEGOT", _ct);

        Assert.Equal([fromStockholm.Id, fromGothenburg.Id], byCountry.Items.Select(v => v.Id));
        Assert.Equal(fromGothenburg.Id, Assert.Single(byLocation.Items).Id);
    }

    [Fact]
    public async Task MovementFromAndTo_MustMatchTheSameMovement()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", terminal));
        // Delivery SEGOT -> terminal and collection terminal -> NLRTM: no single leg goes SE -> NLRTM.
        await VisitApi.CreateAsync(client, terminal, _ct);

        var acrossLegs = await SearchAsync(client, $"terminalId={terminal}&movementFrom=SE&movementTo=NLRTM", _ct);
        var sameLeg = await SearchAsync(client, $"terminalId={terminal}&movementFrom=SE&movementTo={terminal}", _ct);

        Assert.Empty(acrossLegs.Items);
        Assert.Single(sameLeg.Items);
    }

    [Theory]
    [InlineData("movementFrom=S")]
    [InlineData("movementTo=SWEDEN")]
    public async Task MovementFilter_WithWrongLength_Is400(string query)
    {
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", "DKCPH"));

        using var response = await client.GetAsync($"{VisitApi.Route}?{query}", _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadValidationProblemAsync(_ct);
        Assert.Contains(problem.Errors.Keys, key => key.StartsWith("movement", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CreatedBy_IsAnExactMatchOnTheSubject()
    {
        var terminal = Terminals.Next();
        using var alice = fixture.CreateClient(fixture.Tokens.For("alice", terminal));
        using var bob = fixture.CreateClient(fixture.Tokens.For("bob", terminal));
        await VisitApi.CreateAsync(alice, terminal, _ct);
        var byBob = await VisitApi.CreateAsync(bob, terminal, _ct);

        var page = await SearchAsync(alice, $"terminalId={terminal}&createdBy=bob", _ct);
        var none = await SearchAsync(alice, $"terminalId={terminal}&createdBy=BOB", _ct);

        Assert.Equal(byBob.Id, Assert.Single(page.Items).Id);
        Assert.Empty(none.Items);
    }

    [Fact]
    public async Task CreatedTimeWindow_IsInclusive_AndEchoed()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", terminal));
        var created = await VisitApi.CreateAsync(client, terminal, _ct);
        var visit = await VisitApi.GetAsync(client, created.Id, _ct); // stored (microsecond) timestamp, not the in-memory one
        var from = Encode(visit.CreatedTime);
        var beforeCreation = Encode(visit.CreatedTime.AddMilliseconds(-1));

        var inclusive = await SearchAsync(client, $"terminalId={terminal}&createdTimeFrom={from}&createdTimeTo={from}", _ct);
        var excluded = await SearchAsync(client, $"terminalId={terminal}&createdTimeTo={beforeCreation}", _ct);

        Assert.Equal(visit.Id, Assert.Single(inclusive.Items).Id);
        Assert.Equal(visit.CreatedTime, inclusive.CreatedTimeFrom);
        Assert.Equal(visit.CreatedTime, inclusive.CreatedTimeTo);
        Assert.Empty(excluded.Items);
    }

    [Fact]
    public async Task InvertedTimeWindow_Is400()
    {
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", "DKCPH"));
        var now = Encode(DateTimeOffset.UtcNow);
        var yesterday = Encode(DateTimeOffset.UtcNow.AddDays(-1));

        using var response = await client.GetAsync($"{VisitApi.Route}?createdTimeFrom={now}&createdTimeTo={yesterday}", _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadValidationProblemAsync(_ct);
        Assert.Contains("createdTimeFrom", problem.Errors.Keys);
    }

    [Fact]
    public async Task Paging_SplitsResults_AndReportsTotals()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", terminal));
        var ids = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            ids.Add((await VisitApi.CreateAsync(client, terminal, _ct)).Id);
        }
        ids.Reverse();

        var first = await SearchAsync(client, $"terminalId={terminal}&pageSize=2", _ct);
        var second = await SearchAsync(client, $"terminalId={terminal}&pageSize=2&page=2", _ct);
        var beyond = await SearchAsync(client, $"terminalId={terminal}&pageSize=2&page=3", _ct);

        Assert.Equal(ids.Take(2), first.Items.Select(v => v.Id));
        Assert.Equal((1, 2, 3, 2), (first.Page, first.PageSize, first.TotalCount, first.TotalPages));
        Assert.Equal(ids.Skip(2), second.Items.Select(v => v.Id));
        Assert.Equal(2, second.Page);
        Assert.Empty(beyond.Items);
        Assert.Equal(3, beyond.TotalCount);
    }

    [Theory]
    [InlineData("page=0", "page")]
    [InlineData("pageSize=0", "pageSize")]
    [InlineData("pageSize=101", "pageSize")]
    public async Task PagingOutOfRange_Is400(string query, string field)
    {
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", "DKCPH"));

        using var response = await client.GetAsync($"{VisitApi.Route}?{query}", _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadValidationProblemAsync(_ct);
        Assert.Contains(field, problem.Errors.Keys);
    }

    [Fact]
    public async Task NonNumericPage_Is400_NotServerError()
    {
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", "DKCPH"));

        using var response = await client.GetAsync($"{VisitApi.Route}?page=first", _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("The request could not be read.", (await response.ReadProblemAsync(_ct)).Title);
    }

    private static async Task<SearchVisitsResponse> SearchAsync(HttpClient client, string query, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync($"{VisitApi.Route}?{query}", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.ReadAsAsync<SearchVisitsResponse>(cancellationToken);
    }

    private static string Encode(DateTimeOffset value) =>
        Uri.EscapeDataString(value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
}
