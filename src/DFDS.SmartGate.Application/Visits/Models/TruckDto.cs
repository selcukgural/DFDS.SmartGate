using System.ComponentModel;

namespace DFDS.SmartGate.Application.Visits.Models;

/// <summary>The truck as returned to clients; identifiers are in their normalised form.</summary>
/// <param name="UnitNumber">Fleet/unit identifier, upper-case without whitespace.</param>
/// <param name="LicensePlate">Registration plate, upper-case without whitespace.</param>
/// <param name="Carrier">Haulier name, when supplied.</param>
[ImmutableObject(true)]
public sealed record TruckDto(string UnitNumber, string LicensePlate, string? Carrier);
