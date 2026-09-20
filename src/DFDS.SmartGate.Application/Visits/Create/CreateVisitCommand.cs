using System.Text.Json.Serialization;
using DFDS.SmartGate.Domain.Visits;

namespace DFDS.SmartGate.Application.Visits.Create;

/// <summary>
/// Request to create a visit. Raw client values; validated once by <see cref="CreateVisitCommandValidator"/> before the
/// handler runs. Unknown JSON members (e.g. <c>id</c>, <c>createdBy</c>, <c>currentStatus</c>) are rejected at
/// deserialisation so server-assigned fields cannot be mass-assigned.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateVisitCommand
{
    /// <summary>UN/LOCODE of the terminal being visited.</summary>
    public string? TerminalId { get; init; }

    /// <summary>The truck making the visit.</summary>
    public TruckInput? Truck { get; init; }

    /// <summary>The driver.</summary>
    public DriverInput? Driver { get; init; }

    /// <summary>At least one delivery or collection.</summary>
    public IReadOnlyList<MovementInput?>? Movements { get; init; }
}

/// <summary>Truck fields of <see cref="CreateVisitCommand"/>.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TruckInput
{
    /// <summary>Fleet/unit identifier; normalised to upper-case without whitespace.</summary>
    public string? UnitNumber { get; init; }

    /// <summary>Registration plate; normalised to upper-case without whitespace.</summary>
    public string? LicensePlate { get; init; }

    /// <summary>Optional haulier name.</summary>
    public string? Carrier { get; init; }
}

/// <summary>Driver fields of <see cref="CreateVisitCommand"/>.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DriverInput
{
    /// <summary>Full name.</summary>
    public string? Name { get; init; }

    /// <summary>Driving license number; normalised like the plate.</summary>
    public string? LicenseNumber { get; init; }

    /// <summary>Optional contact number in E.164 format (e.g. <c>+4512345678</c>).</summary>
    public string? Phone { get; init; }
}

/// <summary>One movement of <see cref="CreateVisitCommand"/>; the terminal side of the leg is derived from <see cref="Type"/>.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MovementInput
{
    /// <summary>Delivery (unit comes in) or collection (unit goes out).</summary>
    public MovementType? Type { get; init; }

    /// <summary>The trailer/container being moved; normalised.</summary>
    public string? UnitNumber { get; init; }

    /// <summary>UN/LOCODE of the external counterpart: origin for a delivery, destination for a collection.</summary>
    public string? Location { get; init; }

    /// <summary>Optional booking / bill-of-lading reference.</summary>
    public string? Reference { get; init; }
}
