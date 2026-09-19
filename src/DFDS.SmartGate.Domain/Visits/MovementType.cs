namespace DFDS.SmartGate.Domain.Visits;

/// <summary>
/// Direction of a unit movement relative to the terminal: a delivery brings a unit in, a collection takes one out.
/// </summary>
/// <summary>
/// Represents the direction of a unit movement relative to the terminal.
/// </summary>
/// <remarks>
/// A delivery brings a unit into the terminal, while a collection takes one out.
/// </remarks>
public enum MovementType
{
    /// <summary>
    /// Indicates that a unit is arriving at the terminal.
    /// </summary>
    Delivery = 1,

    /// <summary>
    /// Indicates that a unit is leaving the terminal.
    /// </summary>
    Collection = 2,
}
