using System;
using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.Domain.Visits;
using Xunit;
using static DFDS.SmartGate.UnitTests.Domain.Visits.VisitTestData;

namespace DFDS.SmartGate.UnitTests.Domain.Visits;

public sealed class VisitTransitionTests
{
    [Fact]
    public void TransitionTo_FollowsTheFullForwardFlow_AndAppendsHistory()
    {
        var visit = NewVisit();

        Assert.True(visit.TransitionTo(VisitStatus.AtGate, "gate-1", "Arrived lane 3", Now.AddMinutes(1)).IsSuccess);
        Assert.True(visit.TransitionTo(VisitStatus.OnSite, "gate-1", null, Now.AddMinutes(2)).IsSuccess);
        Assert.True(visit.TransitionTo(VisitStatus.Completed, "gate-2", null, Now.AddMinutes(3)).IsSuccess);

        Assert.Equal(VisitStatus.Completed, visit.CurrentStatus);
        Assert.Equal(4, visit.StatusHistory.Count);

        Assert.Equal(VisitStatus.PreRegistered, visit.StatusHistory[0].Status);
        Assert.Equal(VisitStatus.AtGate, visit.StatusHistory[1].Status);
        Assert.Equal("Arrived lane 3", visit.StatusHistory[1].Reason);
        Assert.Equal("gate-1", visit.StatusHistory[1].ChangedBy);
        Assert.Equal(Now.AddMinutes(1), visit.StatusHistory[1].ChangedAt);
        Assert.Equal(VisitStatus.OnSite, visit.StatusHistory[2].Status);
        Assert.Equal(VisitStatus.Completed, visit.StatusHistory[3].Status);
        Assert.Equal("gate-2", visit.StatusHistory[3].ChangedBy);
    }

    [Fact]
    public void TransitionTo_SameStatus_ReturnsConflict_AndLeavesVisitUntouched()
    {
        var visit = NewVisit();

        var result = visit.TransitionTo(VisitStatus.PreRegistered, Operator, null, Now);

        AssertConflict(result, "Visit.AlreadyInStatus");
        Assert.Equal(VisitStatus.PreRegistered, visit.CurrentStatus);
        Assert.Single(visit.StatusHistory);
    }

    [Theory]
    [InlineData(VisitStatus.OnSite)]
    [InlineData(VisitStatus.Completed)]
    public void TransitionTo_SkippingAStep_ReturnsConflict(VisitStatus target)
    {
        var visit = NewVisit();

        var result = visit.TransitionTo(target, Operator, null, Now);

        var error = AssertConflict(result, "Visit.InvalidTransition");
        Assert.Contains("'AtGate'", error.Message, StringComparison.Ordinal);
        Assert.Equal(VisitStatus.PreRegistered, visit.CurrentStatus);
        Assert.Single(visit.StatusHistory);
    }

    [Fact]
    public void TransitionTo_GoingBackwards_ReturnsConflict()
    {
        var visit = NewVisit();
        Assert.True(visit.TransitionTo(VisitStatus.AtGate, Operator, null, Now).IsSuccess);

        var result = visit.TransitionTo(VisitStatus.PreRegistered, Operator, null, Now);

        AssertConflict(result, "Visit.InvalidTransition");
        Assert.Equal(VisitStatus.AtGate, visit.CurrentStatus);
        Assert.Equal(2, visit.StatusHistory.Count);
    }

    [Fact]
    public void TransitionTo_FromCompleted_IsAlwaysRejected()
    {
        var visit = NewVisit();
        visit.TransitionTo(VisitStatus.AtGate, Operator, null, Now);
        visit.TransitionTo(VisitStatus.OnSite, Operator, null, Now);
        visit.TransitionTo(VisitStatus.Completed, Operator, null, Now);

        var result = visit.TransitionTo(VisitStatus.OnSite, Operator, null, Now);

        var error = AssertConflict(result, "Visit.InvalidTransition");
        Assert.Contains("completed", error.Message, StringComparison.Ordinal);
        Assert.Equal(4, visit.StatusHistory.Count);
    }

    [Fact]
    public void TransitionTo_Throws_WhenChangedByIsMissing()
    {
        var visit = NewVisit();

        Assert.Throws<ArgumentException>(() => visit.TransitionTo(VisitStatus.AtGate, "", null, Now));
    }

    private static DomainError AssertConflict(Result result, string expectedCode)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Value.Code);
        Assert.Equal(ErrorKind.Conflict, result.Error.Value.Kind);
        return result.Error.Value;
    }
}
