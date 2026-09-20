namespace DFDS.SmartGate.Application.Visits;

/// <summary>Cache key conventions for visits, shared by the query that fills the cache and the commands that invalidate it.</summary>
public static class VisitCacheKeys
{
    /// <summary>Tag applied to every cached visit so the whole set can be evicted at once (e.g. after a bulk import).</summary>
    public const string Tag = "visits";

    /// <summary>Key of the cached <see cref="Models.VisitResponse"/> for one visit.</summary>
    /// <param name="visitId">Identifier of the visit.</param>
    /// <returns>The key, e.g. <c>visit:0199…</c>.</returns>
    public static string ById(Guid visitId) => $"visit:{visitId}";
}
