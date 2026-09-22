using System.Net;
using DFDS.SmartGate.Application.Visits.Models;
using DFDS.SmartGate.Domain.Visits;

namespace DFDS.SmartGate.IntegrationTests.Hosting;

/// <summary>Happy-path calls used as set-up by many tests; each asserts the expected status so failures point at the set-up.</summary>
internal static class VisitApi
{
    public const string Route = "/api/visits";

    public static async Task<VisitResponse> CreateAsync(HttpClient client, string terminalId, CancellationToken cancellationToken, Action<CreateVisitRequest>? customise = null)
    {
        var request = new CreateVisitRequest { TerminalId = terminalId };
        customise?.Invoke(request);

        using var response = await client.PostJsonAsync(Route, request, cancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.ReadAsAsync<VisitResponse>(cancellationToken);
    }

    public static async Task<VisitResponse> GetAsync(HttpClient client, Guid visitId, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync($"{Route}/{visitId}", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.ReadAsAsync<VisitResponse>(cancellationToken);
    }

    public static async Task<VisitResponse> UpdateStatusAsync(HttpClient client, Guid visitId, VisitStatus status, CancellationToken cancellationToken, string? reason = null)
    {
        using var response = await client.PostJsonAsync($"{Route}/{visitId}/status", new UpdateStatusRequest { Status = status.ToString(), Reason = reason }, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.ReadAsAsync<VisitResponse>(cancellationToken);
    }
}
