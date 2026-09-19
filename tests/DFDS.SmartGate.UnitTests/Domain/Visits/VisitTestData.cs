using System;
using System.Collections.Generic;
using DFDS.SmartGate.Domain.Identifiers;
using DFDS.SmartGate.Domain.Locations;
using DFDS.SmartGate.Domain.Visits;
using Xunit;

namespace DFDS.SmartGate.UnitTests.Domain.Visits;

internal static class VisitTestData
{
    public static readonly DateTimeOffset Now = new(2026, 9, 19, 10, 0, 0, TimeSpan.Zero);
    public const string Operator = "operator-1";

    public static LocationCode Terminal => LocationCode.Create("DKCPH").Value;

    public static LocationCode Istanbul => LocationCode.Create("TRIST").Value;

    public static LocationCode Gothenburg => LocationCode.Create("SEGOT").Value;

    public static Truck Truck() => new(
        UnitNumber.Create("TRK-001").Value,
        LicensePlate.Create("34ABC123").Value,
        Carrier: "Acme Haulage");

    public static Driver Driver() => new("Ayşe Yılmaz", DriverLicenseNumber.Create("DL123").Value, Phone: null);

    public static MovementDraft Delivery(LocationCode? from = null, string unit = "MSKU1234567") =>
        new(MovementType.Delivery, UnitNumber.Create(unit).Value, from ?? Istanbul, Reference: "BK-1");

    public static MovementDraft Collection(LocationCode? to = null, string unit = "HLXU7654321") =>
        new(MovementType.Collection, UnitNumber.Create(unit).Value, to ?? Gothenburg, Reference: null);

    public static VisitDraft Draft(params IReadOnlyList<MovementDraft> movements) =>
        new(Terminal, Truck(), Driver(), movements, Operator);

    public static Visit NewVisit(params IReadOnlyList<MovementDraft> movements)
    {
        var result = Visit.Create(Draft(movements.Count == 0 ? [Delivery()] : movements), Now);
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
