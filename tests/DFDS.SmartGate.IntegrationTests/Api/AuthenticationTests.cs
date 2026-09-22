using System.Net;
using DFDS.SmartGate.IntegrationTests.Hosting;

namespace DFDS.SmartGate.IntegrationTests.Api;

/// <summary>Bearer authentication and the "caller must be identifiable" policy, end to end through the JWT handler.</summary>
[Collection(ApiHost.Name)]
public sealed class AuthenticationTests(ApiFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task MissingToken_Is401_WithChallengeAndProblemDetails()
    {
        using var client = fixture.CreateClient(bearerToken: null);

        using var response = await client.GetAsync(VisitApi.Route, _ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, h => h.Scheme == "Bearer");

        var problem = await response.ReadProblemAsync(_ct);
        Assert.Equal(401, problem.Status);
        Assert.NotNull(problem.CorrelationId());
    }

    [Fact]
    public async Task ExpiredToken_Is401()
    {
        using var client = fixture.CreateClient(fixture.Tokens.Expired("alice", "DKCPH"));

        using var response = await client.GetAsync(VisitApi.Route, _ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("expired", response.Headers.WwwAuthenticate.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TokenSignedByUnknownKey_Is401()
    {
        using var client = fixture.CreateClient(fixture.Tokens.SignedByStranger("alice", "DKCPH"));

        using var response = await client.GetAsync(VisitApi.Route, _ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GarbageToken_Is401()
    {
        using var client = fixture.CreateClient("not.a.jwt");

        using var response = await client.GetAsync(VisitApi.Route, _ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidTokenWithoutSubjectOrClientId_Is403()
    {
        using var client = fixture.CreateClient(fixture.Tokens.WithoutSubject("DKCPH"));

        using var response = await client.GetAsync(VisitApi.Route, _ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.ReadProblemAsync(_ct);
        Assert.Equal(403, problem.Status);
    }

    [Fact]
    public async Task MachineClient_IsIdentifiedByClientId()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.ForClient("gate-kiosk-7", terminal));

        var visit = await VisitApi.CreateAsync(client, terminal, _ct);

        Assert.Equal("gate-kiosk-7", visit.CreatedBy);
        Assert.Equal("gate-kiosk-7", Assert.Single(visit.StatusHistory).ChangedBy);
    }

    [Fact]
    public async Task HealthProbes_IgnoreInvalidTokens()
    {
        using var client = fixture.CreateClient("not.a.jwt");

        using var response = await client.GetAsync("/health/live", _ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
