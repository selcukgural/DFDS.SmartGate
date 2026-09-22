using System.Net;
using DFDS.SmartGate.Api.Http;
using DFDS.SmartGate.IntegrationTests.Hosting;

namespace DFDS.SmartGate.IntegrationTests.Api;

/// <summary>
/// Cross-cutting HTTP behaviour: security headers, correlation ids, HSTS and framework-level error mapping.
/// Kestrel-only settings (request body limit, <c>Server</c> header) are not observable through the in-memory test
/// server and are covered by the unit tests of <see cref="HttpSetup"/> / <see cref="BadRequestExceptionHandler"/>.
/// </summary>
[Collection(ApiHost.Name)]
public sealed class HttpPipelineTests(ApiFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task EveryResponse_CarriesSecurityHeaders()
    {
        using var client = fixture.CreateClient(bearerToken: null);

        using var response = await client.GetAsync("/health/live", _ct);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("default-src 'none'; frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.True(response.Headers.Contains("Strict-Transport-Security"), "HSTS must be on outside Development.");
    }

    [Fact]
    public async Task ErrorResponses_CarrySecurityHeadersToo()
    {
        using var client = fixture.CreateClient(bearerToken: null);

        using var response = await client.GetAsync(VisitApi.Route, _ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task WellFormedCorrelationId_IsEchoed_AndAppearsInProblemDetails()
    {
        using var client = fixture.CreateClient(bearerToken: null);
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "order-42:retry.1");

        using var response = await client.GetAsync(VisitApi.Route, _ct);

        Assert.Equal("order-42:retry.1", response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
        var problem = await response.ReadProblemAsync(_ct);
        Assert.Equal("order-42:retry.1", problem.CorrelationId());
    }

    [Fact]
    public async Task MalformedCorrelationId_IsReplaced()
    {
        using var client = fixture.CreateClient(bearerToken: null);
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "<script>alert(1)</script>");

        using var response = await client.GetAsync("/health/live", _ct);

        var echoed = response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
        Assert.True(CorrelationIdMiddleware.IsWellFormed(echoed));
        Assert.DoesNotContain("<", echoed, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingCorrelationId_IsGenerated()
    {
        using var client = fixture.CreateClient(bearerToken: null);

        using var response = await client.GetAsync("/health/live", _ct);

        var generated = response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
        Assert.True(CorrelationIdMiddleware.IsWellFormed(generated));
    }

    [Fact]
    public async Task UnknownRoute_IsProblemDetails404()
    {
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", "DKCPH"));

        using var response = await client.GetAsync("/api/visits/not-a-guid", _ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.ReadProblemAsync(_ct);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task MalformedJsonBody_Is400_WithoutLeakingInternals()
    {
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", "DKCPH"));

        using var response = await client.PostRawJsonAsync(VisitApi.Route, """{ "terminalId": "DKCPH", "truck": { """, _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadProblemAsync(_ct);
        Assert.Equal("The request could not be read.", problem.Title);
        Assert.DoesNotContain("Exception", problem.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateVisitCommand", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WrongValueType_Is400_NamingTheJsonPath()
    {
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", "DKCPH"));

        using var response = await client.PostRawJsonAsync(VisitApi.Route, """{ "terminalId": "DKCPH", "truck": "not-an-object" }""", _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadProblemAsync(_ct);
        Assert.Contains("$.truck", problem.Detail, StringComparison.Ordinal);
    }
}
