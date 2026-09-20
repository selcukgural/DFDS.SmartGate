using System.ComponentModel;
using DFDS.SmartGate.Domain.Visits;

namespace DFDS.SmartGate.Application.Visits.Models;

/// <summary>One leg of the visit as returned to clients, with both ends of the leg resolved.</summary>
/// <param name="Id">Identifier of the movement.</param>
/// <param name="Type">Delivery or collection.</param>
/// <param name="UnitNumber">The unit being moved, normalised.</param>
/// <param name="From">Origin UN/LOCODE.</param>
/// <param name="To">Destination UN/LOCODE.</param>
/// <param name="Reference">Booking reference, when supplied.</param>
[ImmutableObject(true)]
public sealed record MovementDto(Guid Id, MovementType Type, string UnitNumber, string From, string To, string? Reference);
