using DFDS.SmartGate.Domain.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DFDS.SmartGate.Api.Http;

/// <summary>
/// Maps <see cref="Result{TValue}"/> outcomes to HTTP results. A <see cref="DomainError"/> becomes an RFC 9457
/// ProblemDetails whose status follows <see cref="ErrorKind"/>; the stable <see cref="DomainError.Code"/> is exposed
/// in the <c>code</c> extension so clients can branch without parsing text.
/// </summary>
public static class ProblemResults
{
    /// <summary>Name of the ProblemDetails extension carrying <see cref="DomainError.Code"/>.</summary>
    public const string CodeExtension = "code";

    /// <summary>Returns <paramref name="onSuccess"/>'s result for a successful outcome, otherwise the error as ProblemDetails.</summary>
    /// <typeparam name="TValue">Value type of the result.</typeparam>
    /// <param name="result">The handler outcome.</param>
    /// <param name="onSuccess">Builds the success response (typically a static lambda).</param>
    /// <returns>The HTTP result.</returns>
    public static IResult Match<TValue>(this Result<TValue> result, Func<TValue, IResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result.IsSuccess ? onSuccess(result.Value) : result.Error.Value.ToProblem();
    }

    /// <summary>Converts a domain error into a ProblemDetails response.</summary>
    /// <param name="error">The error.</param>
    /// <returns>A problem result with the status of <see cref="StatusCodeFor"/>.</returns>
    public static ProblemHttpResult ToProblem(this DomainError error) =>
        TypedResults.Problem(
            detail: error.Message,
            statusCode: StatusCodeFor(error.Kind),
            title: TitleFor(error.Kind),
            extensions: new Dictionary<string, object?>(1, StringComparer.Ordinal) { [CodeExtension] = error.Code });

    /// <summary>HTTP status for an error kind.</summary>
    /// <param name="kind">The error kind.</param>
    /// <returns>400, 404, 409 or 403.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a known value.</exception>
    public static int StatusCodeFor(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown error kind."),
    };

    private static string TitleFor(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => "The request is invalid.",
        ErrorKind.NotFound => "The requested resource was not found.",
        ErrorKind.Conflict => "The request conflicts with the current state of the resource.",
        ErrorKind.Forbidden => "You are not allowed to perform this operation.",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown error kind."),
    };
}
