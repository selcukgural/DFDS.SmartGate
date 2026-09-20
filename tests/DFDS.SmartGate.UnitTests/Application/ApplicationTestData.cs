using System;
using System.Collections.Generic;
using DFDS.SmartGate.Application.Visits.Create;
using DFDS.SmartGate.Application.Visits.Models;
using DFDS.SmartGate.Domain.Identifiers;
using DFDS.SmartGate.Domain.Locations;
using DFDS.SmartGate.Domain.Visits;
using DFDS.SmartGate.UnitTests.Application.Fakes;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DFDS.SmartGate.UnitTests.Application;

internal static class ApplicationTestData
{
    public static readonly DateTimeOffset Now = new(2026, 9, 19, 10, 0, 0, TimeSpan.Zero);
    public const string Subject = "operator-1";

    public static LocationCode Copenhagen => LocationCode.Create("DKCPH").Value;

    public static LocationCode Gothenburg => LocationCode.Create("SEGOT").Value;

    public static LocationCode Istanbul => LocationCode.Create("TRIST").Value;

    public static FakeTimeProvider Clock() => new(Now);

    public static FakeCallerContext Caller(params IReadOnlyList<LocationCode> terminals) =>
        new(Subject, terminals.Count == 0 ? [Copenhagen] : terminals);

    public static CreateVisitCommand ValidCreateCommand(string terminalId = "DKCPH") => new()
    {
        TerminalId = terminalId,
        Truck = new TruckInput { UnitNumber = "trk 001", LicensePlate = "34 abc 123", Carrier = "Acme Haulage" },
        Driver = new DriverInput { Name = "Ayşe Yılmaz", LicenseNumber = "dl 123", Phone = "+905321234567" },
        Movements =
        [
            new MovementInput { Type = MovementType.Delivery, UnitNumber = "MSKU1234567", Location = "TRIST", Reference = "BK-1" },
            new MovementInput { Type = MovementType.Collection, UnitNumber = "HLXU7654321", Location = "SEGOT" },
        ],
    };

    public static Visit NewVisit(LocationCode? terminal = null)
    {
        var draft = new VisitDraft(
            terminal ?? Copenhagen,
            new Truck(UnitNumber.Create("TRK-001").Value, LicensePlate.Create("34ABC123").Value, null),
            new Driver("Ayşe Yılmaz", DriverLicenseNumber.Create("DL123").Value, null),
            [new MovementDraft(MovementType.Delivery, UnitNumber.Create("MSKU1234567").Value, Istanbul, null)],
            Subject);

        var result = Visit.Create(draft, Now);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    public static VisitResponse Response(Guid? id = null, string terminalId = "DKCPH") => new(
        id ?? Guid.CreateVersion7(),
        terminalId,
        VisitStatus.PreRegistered,
        new TruckDto("TRK-001", "34ABC123", null),
        new DriverDto("Ayşe Yılmaz", "DL123", null),
        [new MovementDto(Guid.CreateVersion7(), MovementType.Delivery, "MSKU1234567", "TRIST", terminalId, null)],
        [new StatusHistoryEntryDto(VisitStatus.PreRegistered, Now, Subject, null)],
        Now,
        Subject);
}
