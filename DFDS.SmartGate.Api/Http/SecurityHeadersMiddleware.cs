namespace DFDS.SmartGate.Api.Http;

/// <summary>
/// Adds the OWASP-recommended response headers for a JSON API on every response (including errors): no MIME sniffing,
/// no framing, a deny-all CSP, no referrer leakage, and <c>Cache-Control: no-store</c> because responses contain
/// personal data (driver details). HSTS is added separately by <c>UseHsts()</c> outside Development.
/// </summary>
/// <param name="next">The next middleware.</param>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>Applies the headers just before the response starts, so they survive whatever the pipeline does.</summary>
    /// <param name="context">The request.</param>
    /// <returns>Completion of the downstream pipeline.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.OnStarting(static state =>
        {
            var headers = ((HttpContext)state).Response.Headers;

            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=()";
            headers.CacheControl = "no-store";

            return Task.CompletedTask;
        }, context);

        return next(context);
    }
}
