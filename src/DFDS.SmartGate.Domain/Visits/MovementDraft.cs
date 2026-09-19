using DFDS.SmartGate.Domain.Identifiers;
using DFDS.SmartGate.Domain.Locations;

namespace DFDS.SmartGate.Domain.Visits;

/// <summary>
/// A movement as supplied by the client: only the external counterpart location is given; the terminal side
/// of the leg is derived by <see cref="Visit.Create"/> from the movement type.
/// </summary>
/// <param name="Type">Delivery (unit comes in) or collection (unit goes out).</param>
/// <param name="UnitNumber">The trailer/container being moved, normalised.</param>
/// <param name="Location">Origin (for a delivery) or destination (for a collection).</param>
/// <param name="Reference">Optional booking / bill-of-lading reference.</param>
public sealed record MovementDraft(MovementType Type, UnitNumber UnitNumber, LocationCode Location, string? Reference);
