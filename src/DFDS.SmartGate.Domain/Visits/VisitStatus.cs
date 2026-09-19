namespace DFDS.SmartGate.Domain.Visits;

/// <summary>
/// Lifecycle of a truck visit. Values are explicit because they are persisted.
/// Display names in the case brief: "Pre-Registered", "At Gate", "On Site", "Completed".
/// </summary>

public enum VisitStatus
{
    /// <summary>
    /// The visit has been pre-registered and is awaiting arrival.
    /// </summary>
    PreRegistered = 1,

    /// <summary>
    /// The truck has arrived at the gate and is being processed.
    /// </summary>
    AtGate = 2,

    /// <summary>
    /// The truck is currently on site and active in the yard.
    /// </summary>
    OnSite = 3,

    /// <summary>
    /// The visit has been completed and closed.
    /// </summary>
    Completed = 4,
}
