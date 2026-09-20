using System.Collections.Generic;
using System.Linq;
using DFDS.SmartGate.Application.Abstractions;
using DFDS.SmartGate.Domain.Locations;

namespace DFDS.SmartGate.UnitTests.Application.Fakes;

internal sealed class FakeCallerContext(string subject, params IReadOnlyList<LocationCode> terminals) : ICallerContext
{
    public string Subject { get; } = subject;

    public IReadOnlyList<LocationCode> Terminals { get; } = terminals;

    public bool CanAccess(LocationCode terminal) => Terminals.Contains(terminal);
}
