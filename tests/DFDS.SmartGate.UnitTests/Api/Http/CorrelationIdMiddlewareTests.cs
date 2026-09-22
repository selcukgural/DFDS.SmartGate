using DFDS.SmartGate.Api.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace DFDS.SmartGate.UnitTests.Api.Http;

public sealed class CorrelationIdMiddlewareTests
{
    [Theory]
    [InlineData("req-0001")]
    [InlineData("a1b2c3")]
    [InlineData("svc:order.42_x")]
    public void IsWellFormed_AcceptsSafeIds(string id)
    {
        Assert.True(CorrelationIdMiddleware.IsWellFormed(id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("new\nline")]
    [InlineData("<script>")]
    [InlineData("ünïcode")]
    public void IsWellFormed_RejectsUnsafeIds(string id)
    {
        Assert.False(CorrelationIdMiddleware.IsWellFormed(id));
    }

    [Fact]
    public void IsWellFormed_RejectsOverlongIds()
    {
        Assert.False(CorrelationIdMiddleware.IsWellFormed(new string('a', CorrelationIdMiddleware.MaxLength + 1)));
        Assert.True(CorrelationIdMiddleware.IsWellFormed(new string('a', CorrelationIdMiddleware.MaxLength)));
    }

    [Fact]
    public async Task Invoke_HonoursWellFormedClientId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "client-7";
        string? seenInsidePipeline = null;
        var middleware = new CorrelationIdMiddleware(
            ctx =>
            {
                seenInsidePipeline = CorrelationIdMiddleware.Get(ctx);
                return Task.CompletedTask;
            },
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("client-7", seenInsidePipeline);
    }

    [Fact]
    public async Task Invoke_GeneratesIdWhenHeaderIsUnsafe()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "bad value\r\n";
        var middleware = new CorrelationIdMiddleware(static _ => Task.CompletedTask, NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var id = CorrelationIdMiddleware.Get(context);
        Assert.NotNull(id);
        Assert.NotEqual("bad value\r\n", id);
        Assert.True(CorrelationIdMiddleware.IsWellFormed(id));
    }
}
