using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DFDS.SmartGate.Api.Http;

/// <summary>
/// Turns the framework's request-binding failures (malformed JSON, unknown JSON members, unparseable query values,
/// oversized bodies) into a ProblemDetails response with a client-safe explanation. The raw exception text is never
/// returned: it names .NET types and, for JSON errors, could echo request content. Any other exception falls through
/// to the default 500 handling.
/// </summary>
/// <param name="problemDetails">Writer that applies the shared ProblemDetails conventions (trace and correlation ids).</param>
/// <param name="logger">Receives the framework's own description of the failure, for diagnosis on the server side only.</param>
public sealed partial class BadRequestExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<BadRequestExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc/>
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not BadHttpRequestException badRequest)
        {
            return ValueTask.FromResult(false);
        }

        LogRejected(logger, badRequest.StatusCode, badRequest.Message);
        httpContext.Response.StatusCode = badRequest.StatusCode;

        return problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "The request could not be read.",
                Detail = Describe(badRequest),
            },
        });
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Debug, Message = "Request rejected with {StatusCode}: {Reason}")]
    private static partial void LogRejected(ILogger logger, int statusCode, string reason);

    /// <summary>Client-safe explanation of a binding failure.</summary>
    /// <param name="exception">The framework exception.</param>
    /// <returns>A sentence naming the JSON path when one is known.</returns>
    public static string Describe(BadHttpRequestException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            return $"The request body exceeds the limit of {HttpSetup.MaxRequestBodyBytes / 1024} KB.";
        }

        if (exception.InnerException is not JsonException json)
        {
            return "A parameter could not be read from the request. Check the types and formats of the route and query values.";
        }

        var location = json.Path is null ? string.Empty : $" at '{json.Path}'";

        return $"The request body is not valid JSON for this operation{location}. " +
               "Check for malformed JSON, values of the wrong type, and properties that are not part of the contract (server-assigned fields such as 'id' must not be sent).";

    }
}
