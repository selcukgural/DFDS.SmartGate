using System.Net;
using DFDS.SmartGate.IntegrationTests.Hosting;

namespace DFDS.SmartGate.IntegrationTests.Api.Visits;

[Collection(ApiHost.Name)]
public sealed class GetVisitTests(ApiFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task UnknownId_Is404_WithStableCode()
    {
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", "DKCPH"));

        using var response = await client.GetAsync($"{VisitApi.Route}/{Guid.CreateVersion7()}", _ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.ReadProblemAsync(_ct);
        Assert.Equal("Visit.NotFound", problem.Code());
    }

    [Fact]
    public async Task VisitOfAnotherTerminal_Is404_NotForbidden()
    {
        var terminal = Terminals.Next();
        using var owner = fixture.CreateClient(fixture.Tokens.For("alice", terminal));
        var visit = await VisitApi.CreateAsync(owner, terminal, _ct);

        using var outsider = fixture.CreateClient(fixture.Tokens.For("bob", "DKCPH"));
        using var response = await outsider.GetAsync($"{VisitApi.Route}/{visit.Id}", _ct);

        // Existence must not leak across terminals, so the answer is indistinguishable from an unknown id.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.ReadProblemAsync(_ct);
        Assert.Equal("Visit.NotFound", problem.Code());
    }

    [Fact]
    public async Task SecondReader_OfTheSameTerminal_SeesTheVisit()
    {
        var terminal = Terminals.Next();
        using var creator = fixture.CreateClient(fixture.Tokens.For("alice", terminal));
        var visit = await VisitApi.CreateAsync(creator, terminal, _ct);

        using var colleague = fixture.CreateClient(fixture.Tokens.For("bob", terminal, "DKCPH"));
        var fetched = await VisitApi.GetAsync(colleague, visit.Id, _ct);

        Assert.Equal(visit.Id, fetched.Id);
        Assert.Equal("alice", fetched.CreatedBy);
    }
}
