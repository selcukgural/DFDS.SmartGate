namespace DFDS.SmartGate.Application.Visits.GetById;

/// <summary>Request for one visit by id.</summary>
/// <param name="VisitId">Identifier of the visit (route parameter).</param>
public sealed record GetVisitQuery(Guid VisitId);
