using System.ComponentModel;
using DFDS.SmartGate.Domain.Visits;

namespace DFDS.SmartGate.Application.Visits.Models;

/// <summary>
/// Full representation of a visit, returned by create, status update and get-by-id. Property names follow the
/// case brief (<c>createdTime</c>, <c>currentStatus</c>, <c>statusHistory</c>).
/// Marked immutable so <c>HybridCache</c> can hand out the cached instance directly instead of
/// deserialising a copy on every L1 hit.
/// </summary>
/// <param name="Id">Identifier of the visit.</param>
/// <param name="TerminalId">UN/LOCODE of the visited terminal.</param>
/// <param name="CurrentStatus">Latest status.</param>
/// <param name="Truck">The truck.</param>
/// <param name="Driver">The driver.</param>
/// <param name="Movements">Deliveries and collections, in request order.</param>
/// <param name="StatusHistory">Append-only audit trail, oldest first.</param>
/// <param name="CreatedTime">When the visit was created (UTC).</param>
/// <param name="CreatedBy">Token subject of the creator.</param>
[ImmutableObject(true)]
public sealed record VisitResponse(
    Guid Id,
    string TerminalId,
    VisitStatus CurrentStatus,
    TruckDto Truck,
    DriverDto Driver,
    IReadOnlyList<MovementDto> Movements,
    IReadOnlyList<StatusHistoryEntryDto> StatusHistory,
    DateTimeOffset CreatedTime,
    string CreatedBy);
