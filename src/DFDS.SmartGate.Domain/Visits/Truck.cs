using DFDS.SmartGate.Domain.Identifiers;

namespace DFDS.SmartGate.Domain.Visits;

/// <summary>
/// The vehicle making the visit. Value object: identified by its data, owned by the <see cref="Visit"/>.
/// </summary>
/// <param name="UnitNumber">Fleet/unit identifier of the truck, normalised.</param>
/// <param name="LicensePlate">Registration plate, normalised.</param>
/// <param name="Carrier">Optional haulier name.</param>
public sealed record Truck(UnitNumber UnitNumber, LicensePlate LicensePlate, string? Carrier);
