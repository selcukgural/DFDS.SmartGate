using System.ComponentModel;
using DFDS.SmartGate.Domain.Visits;

namespace DFDS.SmartGate.Application.Visits.Models;

/// <summary>
/// Search-result item: everything an operator needs to recognise a visit in a list, without the status history
/// (which is only returned by get-by-id to keep the list query to a single join).
/// </summary>
/// <param name="Id">Identifier of the visit.</param>
/// <param name="TerminalId">UN/LOCODE of the visited terminal.</param>
/// <param name="CurrentStatus">Latest status.</param>
/// <param name="Truck">The truck.</param>
/// <param name="DriverName">The driver's name.</param>
/// <param name="Movements">Deliveries and collections, in request order.</param>
/// <param name="CreatedTime">When the visit was created (UTC).</param>
/// <param name="CreatedBy">Token subject of the creator.</param>
[ImmutableObject(true)]
public sealed record VisitSummary(
    Guid Id,
    string TerminalId,
    VisitStatus CurrentStatus,
    TruckDto Truck,
    string DriverName,
    IReadOnlyList<MovementDto> Movements,
    DateTimeOffset CreatedTime,
    string CreatedBy);
