using System.ComponentModel;
using DFDS.SmartGate.Domain.Visits;

namespace DFDS.SmartGate.Application.Visits.Models;

/// <summary>One audit row of the visit's status history as returned to clients.</summary>
/// <param name="Status">The status entered.</param>
/// <param name="ChangedTime">When the change happened (UTC).</param>
/// <param name="ChangedBy">Token subject of the caller who made the change.</param>
/// <param name="Reason">Free-text reason, when supplied.</param>
[ImmutableObject(true)]
public sealed record StatusHistoryEntryDto(VisitStatus Status, DateTimeOffset ChangedTime, string ChangedBy, string? Reason);
