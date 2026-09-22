using DFDS.SmartGate.Api.Auth;
using DFDS.SmartGate.Api.Endpoints;
using DFDS.SmartGate.Api.Http;
using DFDS.SmartGate.Api.Observability;
using DFDS.SmartGate.Application;
using DFDS.SmartGate.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(HttpSetup.ConfigureKestrel);
builder.AddApiObservability();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiAuthentication();
builder.Services.AddApiHttp();

var app = builder.Build();

// Order matters: the correlation scope wraps everything so every log line carries the id; request logging sits
// outside the exception handler so the combined entry records the status the handler actually wrote.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseHttpLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    // TLS terminates at the load balancer in production; set ASPNETCORE_FORWARDEDHEADERS_ENABLED=true there so the
    // redirect and HSTS see the original scheme. The local http profile is left untouched.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.MapHealthEndpoints();
app.MapVisitEndpoints();

await app.RunAsync().ConfigureAwait(false);
