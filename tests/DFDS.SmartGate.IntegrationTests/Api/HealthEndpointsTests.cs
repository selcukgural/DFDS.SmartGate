using System.Net;
using DFDS.SmartGate.IntegrationTests.Hosting;

namespace DFDS.SmartGate.IntegrationTests.Api;

[Collection(ApiHost.Name)]
public sealed class HealthEndpointsTests(ApiFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Probes_AreAnonymous_AndReportHealthy(string path)
    {
        using var client = fixture.CreateClient(bearerToken: null);

        using var response = await client.GetAsync(path, _ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(_ct));
    }

    [Fact]
    public async Task OpenApiDocument_IsNotServedOutsideDevelopment()
    {
        // Authenticated on purpose: anonymous callers get 401 for every unmapped path (fallback policy), which would
        // not tell whether the document exists.
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", "DKCPH"));

        using var response = await client.GetAsync("/openapi/v1.json", _ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
