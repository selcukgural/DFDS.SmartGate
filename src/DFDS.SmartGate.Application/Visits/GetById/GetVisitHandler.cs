using DFDS.SmartGate.Application.Abstractions;
using DFDS.SmartGate.Application.Visits.Models;
using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.Domain.Locations;
using DFDS.SmartGate.Domain.Visits;
using Microsoft.Extensions.Caching.Hybrid;

namespace DFDS.SmartGate.Application.Visits.GetById;

/// <summary>
/// Returns one visit through the cache. The cache key is caller-independent (the same representation is valid for
/// every entitled caller); authorization is applied after the lookup, and a visit of another terminal is reported
/// as not found. Entries are evicted by the commands that change a visit.
/// </summary>
/// <param name="readStore">Read-side projection of visits.</param>
/// <param name="cache">L1/L2 cache; L2 is present only when the host configured a distributed cache.</param>
/// <param name="caller">Terminal entitlements of the caller.</param>
public sealed class GetVisitHandler(IVisitReadStore readStore, HybridCache cache, ICallerContext caller)
    : IQueryHandler<GetVisitQuery, VisitResponse>
{
    /// <summary>
    /// Cache lifetime. The local (L1) lifetime is deliberately short because an eviction after a status change only
    /// reaches the L1 of the instance that handled the write; other instances converge within this window.
    /// </summary>
    internal static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromSeconds(30),
    };

    private static readonly string[] CacheTags = [VisitCacheKeys.Tag];

    /// <inheritdoc/>
    /// <returns>The visit; <see cref="VisitErrors.NotFound"/> when it does not exist or belongs to a terminal the caller lacks.</returns>
    public async Task<Result<VisitResponse>> HandleAsync(GetVisitQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var visit = await cache.GetOrCreateAsync(
            VisitCacheKeys.ById(query.VisitId),
            (readStore, query.VisitId),
            static (state, ct) => new ValueTask<VisitResponse?>(state.readStore.GetByIdAsync(state.VisitId, ct)),
            CacheOptions,
            CacheTags,
            cancellationToken).ConfigureAwait(false);

        if (visit is null || !caller.CanAccess(LocationCode.Create(visit.TerminalId).Value))
        {
            return VisitErrors.NotFound(query.VisitId);
        }

        return visit;
    }
}
