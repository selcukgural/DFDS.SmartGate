using Microsoft.AspNetCore.HttpLogging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DFDS.SmartGate.Api.Observability;

/// <summary>
/// Logging, request logging and OpenTelemetry wiring. Everything is vendor-neutral: logs are JSON on stdout outside
/// Development, traces and metrics leave through OTLP when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set (an OTel/ADOT
/// collector then forwards to CloudWatch, Grafana, …) and stay in-process otherwise.
/// </summary>
public static class ObservabilitySetup
{
    /// <summary>Logical service name reported in traces and metrics.</summary>
    private const string ServiceName = "dfds-visits-api";

    private const string OtlpEndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>
    /// Configures structured logging (JSON console outside Development, scopes included so <c>CorrelationId</c> and
    /// <c>TraceId</c> appear on every entry), one combined log line per request, and OpenTelemetry tracing and metrics
    /// for ASP.NET Core (throughput, latency histograms), the runtime, Npgsql and EF Core.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static WebApplicationBuilder AddApiObservability(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!builder.Environment.IsDevelopment())
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddJsonConsole(static options =>
            {
                options.IncludeScopes = true;
                options.UseUtcTimestamp = true;
                options.TimestampFormat = "O";
            });
        }

        // One aggregated entry per request; request/response headers and bodies are deliberately excluded
        // (they would carry the bearer token and personal data).
        builder.Services.AddHttpLogging(static options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestMethod
                | HttpLoggingFields.RequestPath
                | HttpLoggingFields.RequestProtocol
                | HttpLoggingFields.ResponseStatusCode
                | HttpLoggingFields.Duration;
            options.CombineLogs = true;
        });

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(static resource => resource.AddService(ServiceName, serviceVersion: ServiceVersion()))
            .WithTracing(static tracing => tracing
                .AddAspNetCoreInstrumentation(static options => options.Filter = static context => !IsHealthProbe(context))
                .AddSource("Npgsql"))
            .WithMetrics(static metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("Npgsql", "Microsoft.EntityFrameworkCore"));

        if (!string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointVariable]))
        {
            telemetry.UseOtlpExporter();
        }

        return builder;
    }

    private static bool IsHealthProbe(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);

    private static string ServiceVersion() =>
        typeof(ObservabilitySetup).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}
