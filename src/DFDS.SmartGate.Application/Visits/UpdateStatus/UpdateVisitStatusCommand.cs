using System.Text.Json.Serialization;
using DFDS.SmartGate.Domain.Visits;

namespace DFDS.SmartGate.Application.Visits.UpdateStatus;

/// <summary>
/// Request to move a visit to a new status. <see cref="VisitId"/> comes from the route, the rest from the body.
/// Unknown JSON members are rejected at deserialisation.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateVisitStatusCommand
{
    /// <summary>Identifier of the visit (route parameter, not part of the body).</summary>
    [JsonIgnore]
    public Guid VisitId { get; init; }

    /// <summary>The status to enter. Must be the next status in the forward flow.</summary>
    public VisitStatus? Status { get; init; }

    /// <summary>Optional free-text reason recorded in the audit trail.</summary>
    public string? Reason { get; init; }
}
