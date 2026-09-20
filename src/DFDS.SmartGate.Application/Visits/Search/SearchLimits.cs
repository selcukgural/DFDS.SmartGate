namespace DFDS.SmartGate.Application.Visits.Search;

/// <summary>Paging and time-window bounds of the search endpoint. Documented in the API contract; change here and in the docs together.</summary>
public static class SearchLimits
{
    /// <summary>Items per page when the client does not specify one.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Largest page a client may request; larger values are rejected, not clamped.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Window applied when the client gives no <c>createdTimeFrom</c>: bounds the scan of an otherwise unfiltered search.</summary>
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromDays(30);
}
