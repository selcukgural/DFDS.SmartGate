using System.Net;
using DFDS.SmartGate.Domain.Visits;
using DFDS.SmartGate.IntegrationTests.Hosting;

namespace DFDS.SmartGate.IntegrationTests.Api.Visits;

[Collection(ApiHost.Name)]
public sealed class UpdateVisitStatusTests(ApiFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ForwardFlow_AppendsHistory_AndIsVisibleOnRead()
    {
        var terminal = Terminals.Next();
        using var operatorClient = fixture.CreateClient(fixture.Tokens.For("gate-1", terminal));
        using var yardClient = fixture.CreateClient(fixture.Tokens.For("yard-2", terminal));
        var visit = await VisitApi.CreateAsync(operatorClient, terminal, _ct);

        var atGate = await VisitApi.UpdateStatusAsync(operatorClient, visit.Id, VisitStatus.AtGate, _ct, reason: "Arrived at gate 3");
        var onSite = await VisitApi.UpdateStatusAsync(yardClient, visit.Id, VisitStatus.OnSite, _ct);
        var completed = await VisitApi.UpdateStatusAsync(yardClient, visit.Id, VisitStatus.Completed, _ct, reason: "Left via gate 1");

        Assert.Equal(VisitStatus.AtGate, atGate.CurrentStatus);
        Assert.Equal(VisitStatus.OnSite, onSite.CurrentStatus);
        Assert.Equal(VisitStatus.Completed, completed.CurrentStatus);

        Assert.Collection(
            completed.StatusHistory,
            h => Assert.Equal((VisitStatus.PreRegistered, "gate-1", (string?)null), (h.Status, h.ChangedBy, h.Reason)),
            h => Assert.Equal((VisitStatus.AtGate, "gate-1", "Arrived at gate 3"), (h.Status, h.ChangedBy, h.Reason)),
            h => Assert.Equal((VisitStatus.OnSite, "yard-2", (string?)null), (h.Status, h.ChangedBy, h.Reason)),
            h => Assert.Equal((VisitStatus.Completed, "yard-2", "Left via gate 1"), (h.Status, h.ChangedBy, h.Reason)));

        Assert.True(completed.StatusHistory.Zip(completed.StatusHistory.Skip(1)).All(pair => pair.First.ChangedTime <= pair.Second.ChangedTime));

        // The cached representation must have been evicted by the update.
        var fetched = await VisitApi.GetAsync(operatorClient, visit.Id, _ct);
        VisitAssert.Equivalent(completed, fetched);
    }

    [Fact]
    public async Task SkippingAStatus_Is409_NamingTheAllowedNext()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("gate-1", terminal));
        var visit = await VisitApi.CreateAsync(client, terminal, _ct);

        using var response = await client.PostJsonAsync($"{VisitApi.Route}/{visit.Id}/status", new UpdateStatusRequest { Status = "OnSite" }, _ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.ReadProblemAsync(_ct);
        Assert.Equal("Visit.InvalidTransition", problem.Code());
        Assert.Contains("'AtGate'", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MovingBackwards_Is409()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("gate-1", terminal));
        var visit = await VisitApi.CreateAsync(client, terminal, _ct);
        await VisitApi.UpdateStatusAsync(client, visit.Id, VisitStatus.AtGate, _ct);

        using var response = await client.PostJsonAsync($"{VisitApi.Route}/{visit.Id}/status", new UpdateStatusRequest { Status = "PreRegistered" }, _ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Visit.InvalidTransition", (await response.ReadProblemAsync(_ct)).Code());
    }

    [Fact]
    public async Task SameStatusTwice_Is409()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("gate-1", terminal));
        var visit = await VisitApi.CreateAsync(client, terminal, _ct);
        await VisitApi.UpdateStatusAsync(client, visit.Id, VisitStatus.AtGate, _ct);

        using var response = await client.PostJsonAsync($"{VisitApi.Route}/{visit.Id}/status", new UpdateStatusRequest { Status = "AtGate" }, _ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Visit.AlreadyInStatus", (await response.ReadProblemAsync(_ct)).Code());
    }

    [Fact]
    public async Task CompletedVisit_IsTerminal()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("gate-1", terminal));
        var visit = await VisitApi.CreateAsync(client, terminal, _ct);
        await VisitApi.UpdateStatusAsync(client, visit.Id, VisitStatus.AtGate, _ct);
        await VisitApi.UpdateStatusAsync(client, visit.Id, VisitStatus.OnSite, _ct);
        await VisitApi.UpdateStatusAsync(client, visit.Id, VisitStatus.Completed, _ct);

        using var response = await client.PostJsonAsync($"{VisitApi.Route}/{visit.Id}/status", new UpdateStatusRequest { Status = "AtGate" }, _ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.ReadProblemAsync(_ct);
        Assert.Equal("Visit.InvalidTransition", problem.Code());
        Assert.Contains("completed", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownStatusName_Is400()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("gate-1", terminal));
        var visit = await VisitApi.CreateAsync(client, terminal, _ct);

        using var response = await client.PostJsonAsync($"{VisitApi.Route}/{visit.Id}/status", new UpdateStatusRequest { Status = "Rejected" }, _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadProblemAsync(_ct);
        Assert.Contains("$.status", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingStatus_Is400_ValidationProblem()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("gate-1", terminal));
        var visit = await VisitApi.CreateAsync(client, terminal, _ct);

        using var response = await client.PostJsonAsync($"{VisitApi.Route}/{visit.Id}/status", new UpdateStatusRequest { Reason = "no status" }, _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadValidationProblemAsync(_ct);
        Assert.Contains("status", problem.Errors.Keys);
    }

    [Fact]
    public async Task VisitOfAnotherTerminal_Is404()
    {
        var terminal = Terminals.Next();
        using var owner = fixture.CreateClient(fixture.Tokens.For("gate-1", terminal));
        var visit = await VisitApi.CreateAsync(owner, terminal, _ct);

        using var outsider = fixture.CreateClient(fixture.Tokens.For("bob", "DKCPH"));
        using var response = await outsider.PostJsonAsync($"{VisitApi.Route}/{visit.Id}/status", new UpdateStatusRequest { Status = "AtGate" }, _ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Visit.NotFound", (await response.ReadProblemAsync(_ct)).Code());

        var unchanged = await VisitApi.GetAsync(owner, visit.Id, _ct);
        Assert.Equal(VisitStatus.PreRegistered, unchanged.CurrentStatus);
    }

    [Fact]
    public async Task UnknownVisit_Is404()
    {
        using var client = fixture.CreateClient(fixture.Tokens.For("gate-1", "DKCPH"));

        using var response = await client.PostJsonAsync($"{VisitApi.Route}/{Guid.CreateVersion7()}/status", new UpdateStatusRequest { Status = "AtGate" }, _ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
