namespace DFDS.SmartGate.Application.Visits;

/// <summary>One page of a read-store query together with the total number of matches.</summary>
/// <typeparam name="T">The read model type.</typeparam>
/// <param name="Items">The items of the requested page, in the query's fixed order.</param>
/// <param name="TotalCount">Total number of matches across all pages.</param>
public sealed record PagedItems<T>(IReadOnlyList<T> Items, int TotalCount);

/// <summary>Factory helpers for <see cref="PagedItems{T}"/>.</summary>
public static class PagedItems
{
    /// <summary>A page with no items.</summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <returns>An empty page with a total count of zero.</returns>
    public static PagedItems<T> Empty<T>() => new([], 0);
}
