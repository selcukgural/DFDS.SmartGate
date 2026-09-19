using DFDS.SmartGate.Domain.Locations;

namespace DFDS.SmartGate.Domain.Visits;

/// <summary>
/// Everything needed to create a <see cref="Visit"/>. All members are already validated value objects;
/// raw request validation happens at the API boundary, not here.
/// </summary>
/// <param name="TerminalId">Terminal being visited (UN/LOCODE).</param>
/// <param name="Truck">The truck.</param>
/// <param name="Driver">The driver.</param>
/// <param name="Movements">At least one delivery or collection.</param>
/// <param name="CreatedBy">Subject of the caller's token.</param>
public sealed record VisitDraft(
    LocationCode TerminalId,
    Truck Truck,
    Driver Driver,
    IReadOnlyList<MovementDraft> Movements,
    string CreatedBy);
