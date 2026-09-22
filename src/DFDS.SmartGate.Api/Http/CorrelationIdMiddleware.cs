using System.Diagnostics;

namespace DFDS.SmartGate.Api.Http;

/// <summary>
/// Gives every request a correlation id that is echoed in the response, attached to the logging scope and to
/// ProblemDetails. A well-formed client-supplied <c>X-Correlation-ID</c> is honoured so callers can trace a request
/// through their own systems; otherwise the W3C trace id of the request activity is used (the id OpenTelemetry exports).
/// </summary>
/// <param name="next">The next middleware.</param>
/// <param name="logger">Logger the correlation scope is opened on.</param>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    /// <summary>Request and response header carrying the correlation id.</summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>Longest accepted client-supplied id.</summary>
    public const int MaxLength = 64;

    private const string ItemKey = "CorrelationId";

    /// <summary>Correlation id of the current request, once the middleware has run.</summary>
    /// <param name="context">The request.</param>
    /// <returns>The id, or <see langword="null"/> before the middleware ran.</returns>
    public static string? Get(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Items.TryGetValue(ItemKey, out var value) ? value as string : null;
    }

    /// <summary>
    /// Accepts an id only when it is short and made of characters safe for logs and headers (ASCII letters, digits,
    /// <c>-</c>, <c>_</c>, <c>.</c>, <c>:</c>), which rules out log-injection and header-splitting payloads.
    /// </summary>
    /// <param name="candidate">The header value.</param>
    /// <returns>Whether the value may be used as-is.</returns>
    public static bool IsWellFormed(ReadOnlySpan<char> candidate)
    {
        if (candidate.IsEmpty || candidate.Length > MaxLength)
        {
            return false;
        }

        foreach (var c in candidate)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_' or '.' or ':'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Resolves the id, stores it on the request, echoes it and opens the logging scope for the rest of the pipeline.</summary>
    /// <param name="context">The request.</param>
    /// <returns>Completion of the downstream pipeline.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = Resolve(context);

        context.Items[ItemKey] = correlationId;

        // Set on response start rather than now: the exception handler clears headers before writing its response.
        context.Response.OnStarting(static state =>
        {
            var httpContext = (HttpContext)state;
            httpContext.Response.Headers[HeaderName] = Get(httpContext);

            return Task.CompletedTask;
        }, context);

        return InvokeScopedAsync(context, correlationId);
    }

    private async Task InvokeScopedAsync(HttpContext context, string correlationId)
    {
        using (logger.BeginScope(new CorrelationScope(correlationId)))
        {
            await next(context).ConfigureAwait(false);
        }
    }

    private static string Resolve(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values)
            && values is [{ } supplied]
            && IsWellFormed(supplied))
        {
            return supplied;
        }

        return Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;
    }

    /// <summary>Single-entry logging scope so structured sinks emit <c>CorrelationId</c> as a property.</summary>
    private sealed class CorrelationScope(string correlationId) : IReadOnlyList<KeyValuePair<string, object?>>
    {
        public int Count => 1;

        public KeyValuePair<string, object?> this[int index] =>
            index == 0 ? new KeyValuePair<string, object?>(ItemKey, correlationId) : throw new ArgumentOutOfRangeException(nameof(index));

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            yield return this[0];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => $"{ItemKey}:{correlationId}";
    }
}
