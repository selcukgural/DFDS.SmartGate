using DFDS.SmartGate.Application.Visits.Models;

namespace DFDS.SmartGate.IntegrationTests.Hosting;

/// <summary>
/// Structural comparison of visit representations. The response records hold lists (reference equality), and
/// timestamps produced in memory carry 100 ns ticks on some platforms while PostgreSQL stores microseconds, so
/// times are compared with a one-microsecond tolerance.
/// </summary>
internal static class VisitAssert
{
    public static readonly TimeSpan TimePrecision = TimeSpan.FromMicroseconds(1);

    public static void Equivalent(VisitResponse expected, VisitResponse actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.TerminalId, actual.TerminalId);
        Assert.Equal(expected.CurrentStatus, actual.CurrentStatus);
        Assert.Equal(expected.Truck, actual.Truck);
        Assert.Equal(expected.Driver, actual.Driver);
        Assert.Equal(expected.Movements, actual.Movements);
        Assert.Equal(expected.CreatedTime, actual.CreatedTime, TimePrecision);
        Assert.Equal(expected.CreatedBy, actual.CreatedBy);

        Assert.Equal(expected.StatusHistory.Count, actual.StatusHistory.Count);
        foreach (var (e, a) in expected.StatusHistory.Zip(actual.StatusHistory))
        {
            Assert.Equal((e.Status, e.ChangedBy, e.Reason), (a.Status, a.ChangedBy, a.Reason));
            Assert.Equal(e.ChangedTime, a.ChangedTime, TimePrecision);
        }
    }
}
