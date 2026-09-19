using System;
using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.Domain.Visits;
using Xunit;
using static DFDS.SmartGate.UnitTests.Domain.Visits.VisitTestData;

namespace DFDS.SmartGate.UnitTests.Domain.Visits;

public sealed class VisitCreateTests
{
    [Fact]
    public void Create_StartsPreRegistered_WithSingleAuditEntry()
    {
        var visit = NewVisit();

        Assert.Equal(VisitStatus.PreRegistered, visit.CurrentStatus);

        var entry = Assert.Single(visit.StatusHistory);
        Assert.Equal(VisitStatus.PreRegistered, entry.Status);
        Assert.Equal(Now, entry.ChangedAt);
        Assert.Equal(Operator, entry.ChangedBy);
        Assert.Null(entry.Reason);
    }

    [Fact]
    public void Create_CopiesDraftData()
    {
        var draft = Draft(Delivery());

        var visit = Visit.Create(draft, Now).Value;

        Assert.NotEqual(Guid.Empty, visit.Id);
        Assert.Equal(draft.TerminalId, visit.TerminalId);
        Assert.Same(draft.Truck, visit.Truck);
        Assert.Same(draft.Driver, visit.Driver);
        Assert.Equal(Now, visit.CreatedAt);
        Assert.Equal(Operator, visit.CreatedBy);
    }

    [Fact]
    public void Create_GeneratesTimeOrderedIds()
    {
        var earlier = Visit.Create(Draft(Delivery()), Now).Value;
        var later = Visit.Create(Draft(Delivery()), Now.AddSeconds(1)).Value;

        Assert.Equal(7, earlier.Id.Version);
        Assert.True(string.CompareOrdinal(earlier.Id.ToString(), later.Id.ToString()) < 0);
    }

    [Fact]
    public void Create_Delivery_RunsFromExternalLocationToTerminal()
    {
        var visit = NewVisit(Delivery(from: Istanbul));

        var movement = Assert.Single(visit.Movements);
        Assert.Equal(MovementType.Delivery, movement.Type);
        Assert.Equal(Istanbul, movement.From);
        Assert.Equal(Terminal, movement.To);
        Assert.Equal("MSKU1234567", movement.UnitNumber.Value);
        Assert.Equal("BK-1", movement.Reference);
    }

    [Fact]
    public void Create_Collection_RunsFromTerminalToExternalLocation()
    {
        var visit = NewVisit(Collection(to: Gothenburg));

        var movement = Assert.Single(visit.Movements);
        Assert.Equal(MovementType.Collection, movement.Type);
        Assert.Equal(Terminal, movement.From);
        Assert.Equal(Gothenburg, movement.To);
    }

    [Fact]
    public void Create_PreservesMovementOrder_AndSupportsBothTypes()
    {
        var visit = NewVisit(Delivery(unit: "UNIT1"), Collection(unit: "UNIT2"), Delivery(unit: "UNIT3"));

        Assert.Equal(3, visit.Movements.Count);
        Assert.Equal("UNIT1", visit.Movements[0].UnitNumber.Value);
        Assert.Equal("UNIT2", visit.Movements[1].UnitNumber.Value);
        Assert.Equal("UNIT3", visit.Movements[2].UnitNumber.Value);
    }

    [Fact]
    public void Create_Fails_WhenThereAreNoMovements()
    {
        var result = Visit.Create(Draft(), Now);

        Assert.True(result.IsFailure);
        Assert.Equal("Visit.NoMovements", result.Error.Value.Code);
        Assert.Equal(ErrorKind.Validation, result.Error.Value.Kind);
    }

    [Fact]
    public void Create_Fails_WhenAMovementLocationIsTheVisitedTerminal()
    {
        var result = Visit.Create(Draft(Delivery(), Collection(to: Terminal)), Now);

        Assert.True(result.IsFailure);
        Assert.Equal("Visit.MovementLocationIsTerminal", result.Error.Value.Code);
        Assert.Contains("index 1", result.Error.Value.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_Throws_WhenCreatedByIsMissing()
    {
        var draft = Draft(Delivery()) with { CreatedBy = " " };

        Assert.Throws<ArgumentException>(() => Visit.Create(draft, Now));
    }
}
