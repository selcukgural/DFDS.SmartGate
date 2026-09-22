using System.Text.Json;
using DFDS.SmartGate.Api.Http;
using Microsoft.AspNetCore.Http;

namespace DFDS.SmartGate.UnitTests.Api.Http;

public sealed class BadRequestExceptionHandlerTests
{
    private static readonly JsonSerializerOptions Strict = new()
    {
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };

    [Fact]
    public void Describe_NamesJsonPathWithoutLeakingTypes()
    {
        JsonException json;
        try
        {
            JsonSerializer.Deserialize<Sample>("""{"id":"x"}""", Strict);
            throw new InvalidOperationException("expected a JsonException");
        }
        catch (JsonException e)
        {
            json = e;
        }

        var detail = BadRequestExceptionHandler.Describe(new BadHttpRequestException("Failed to read parameter \"Sample s\"", json));

        Assert.Contains("'$.id'", detail, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(Sample), detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Failed to read parameter", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Describe_ExplainsPayloadTooLarge()
    {
        var detail = BadRequestExceptionHandler.Describe(new BadHttpRequestException("too big", StatusCodes.Status413PayloadTooLarge));

        Assert.Contains("64 KB", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Describe_FallsBackToGenericBindingMessage()
    {
        var detail = BadRequestExceptionHandler.Describe(new BadHttpRequestException("Failed to bind parameter \"int Page\" from \"abc\"."));

        Assert.DoesNotContain("int Page", detail, StringComparison.Ordinal);
        Assert.Contains("query", detail, StringComparison.Ordinal);
    }

    private sealed record Sample(string Name);
}
