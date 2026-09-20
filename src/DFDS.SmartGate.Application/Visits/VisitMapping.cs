using DFDS.SmartGate.Application.Visits.Models;
using DFDS.SmartGate.Domain.Visits;

namespace DFDS.SmartGate.Application.Visits;

/// <summary>Projects the <see cref="Visit"/> aggregate into response models after a write, so no re-read is needed.</summary>
internal static class VisitMapping
{
    /// <summary>Builds the full response from an aggregate.</summary>
    /// <param name="visit">The aggregate, freshly created or updated.</param>
    /// <returns>The response model.</returns>
    public static VisitResponse ToResponse(Visit visit)
    {
        var movements = new MovementDto[visit.Movements.Count];

        for (var i = 0; i < movements.Length; i++)
        {
            var m = visit.Movements[i];
            movements[i] = new MovementDto(m.Id, m.Type, m.UnitNumber.Value, m.From.Value, m.To.Value, m.Reference);
        }

        var history = new StatusHistoryEntryDto[visit.StatusHistory.Count];

        for (var i = 0; i < history.Length; i++)
        {
            var h = visit.StatusHistory[i];
            history[i] = new StatusHistoryEntryDto(h.Status, h.ChangedAt, h.ChangedBy, h.Reason);
        }

        return new VisitResponse(
            visit.Id,
            visit.TerminalId.Value,
            visit.CurrentStatus,
            new TruckDto(visit.Truck.UnitNumber.Value, visit.Truck.LicensePlate.Value, visit.Truck.Carrier),
            new DriverDto(visit.Driver.Name, visit.Driver.LicenseNumber.Value, visit.Driver.Phone),
            movements,
            history,
            visit.CreatedAt,
            visit.CreatedBy);
    }
}
