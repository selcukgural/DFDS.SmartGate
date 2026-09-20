using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace DFDS.SmartGate.UnitTests.Application.Fakes;

internal static class TestCache
{
    /// <summary>A real in-process HybridCache (L1 only), so eviction behaviour is exercised for real.</summary>
    public static HybridCache Create()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }
}
