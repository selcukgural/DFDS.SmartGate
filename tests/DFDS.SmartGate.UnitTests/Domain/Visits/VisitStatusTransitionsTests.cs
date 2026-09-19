using System;
using DFDS.SmartGate.Domain.Visits;
using Xunit;

namespace DFDS.SmartGate.UnitTests.Domain.Visits;

public sealed class VisitStatusTransitionsTests
{
    [Theory]
    [InlineData(VisitStatus.PreRegistered, VisitStatus.AtGate)]
    [InlineData(VisitStatus.AtGate, VisitStatus.OnSite)]
    [InlineData(VisitStatus.OnSite, VisitStatus.Completed)]
    public void CanTransition_AllowsTheNextForwardStep(VisitStatus from, VisitStatus to)
    {
        Assert.True(VisitStatusTransitions.CanTransition(from, to));
        Assert.Equal(to, VisitStatusTransitions.NextOf(from));
    }

    public static TheoryData<VisitStatus, VisitStatus> ForbiddenTransitions()
    {
        var data = new TheoryData<VisitStatus, VisitStatus>();

        foreach (var from in Enum.GetValues<VisitStatus>())
        {
            foreach (var to in Enum.GetValues<VisitStatus>())
            {
                if (VisitStatusTransitions.NextOf(from) != to)
                {
                    data.Add(from, to);
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ForbiddenTransitions))]
    public void CanTransition_RejectsEverythingElse(VisitStatus from, VisitStatus to)
    {
        Assert.False(VisitStatusTransitions.CanTransition(from, to));
    }

    [Fact]
    public void Completed_IsTheOnlyTerminalStatus()
    {
        Assert.True(VisitStatusTransitions.IsTerminal(VisitStatus.Completed));
        Assert.Null(VisitStatusTransitions.NextOf(VisitStatus.Completed));

        Assert.False(VisitStatusTransitions.IsTerminal(VisitStatus.PreRegistered));
        Assert.False(VisitStatusTransitions.IsTerminal(VisitStatus.AtGate));
        Assert.False(VisitStatusTransitions.IsTerminal(VisitStatus.OnSite));
    }

    [Fact]
    public void Initial_IsPreRegistered()
    {
        Assert.Equal(VisitStatus.PreRegistered, VisitStatusTransitions.Initial);
    }
}
