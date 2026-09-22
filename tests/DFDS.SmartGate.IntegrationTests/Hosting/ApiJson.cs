using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DFDS.SmartGate.IntegrationTests.Hosting;

/// <summary>Serialisation helpers matching the API's wire format (camelCase, enums as names).</summary>
internal static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string requestUri, object body, CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(requestUri, body, Options, cancellationToken);

    public static Task<HttpResponseMessage> PostRawJsonAsync(this HttpClient client, string requestUri, string json, CancellationToken cancellationToken) =>
        client.PostAsync(requestUri, new StringContent(json, System.Text.Encoding.UTF8, "application/json"), cancellationToken);

    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Options, cancellationToken);
        Assert.NotNull(value);
        return value;
    }

    public static async Task<ProblemDetails> ReadProblemAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return await response.ReadAsAsync<ProblemDetails>(cancellationToken);
    }

    public static async Task<HttpValidationProblemDetails> ReadValidationProblemAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return await response.ReadAsAsync<HttpValidationProblemDetails>(cancellationToken);
    }

    /// <summary>The stable <c>code</c> extension of a domain-error ProblemDetails.</summary>
    public static string? Code(this ProblemDetails problem) =>
        problem.Extensions.TryGetValue("code", out var value) && value is JsonElement { ValueKind: JsonValueKind.String } element
            ? element.GetString()
            : null;

    /// <summary>The <c>correlationId</c> extension every ProblemDetails carries.</summary>
    public static string? CorrelationId(this ProblemDetails problem) =>
        problem.Extensions.TryGetValue("correlationId", out var value) && value is JsonElement { ValueKind: JsonValueKind.String } element
            ? element.GetString()
            : null;
}
