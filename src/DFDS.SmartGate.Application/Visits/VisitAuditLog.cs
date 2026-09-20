using DFDS.SmartGate.Domain.Visits;
using Microsoft.Extensions.Logging;

namespace DFDS.SmartGate.Application.Visits;

/// <summary>
/// Source-generated audit events for visit writes: one structured entry per successful command, carrying only
/// identifiers and token subjects (never truck, driver or free-text fields).
/// </summary>
internal static partial class VisitAuditLog
{
    /// <summary>A visit was created.</summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="visitId">Identifier of the new visit.</param>
    /// <param name="terminalId">Terminal being visited.</param>
    /// <param name="movementCount">Number of movements in the visit.</param>
    /// <param name="createdBy">Token subject of the creator.</param>
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Visit {VisitId} created at terminal {TerminalId} with {MovementCount} movement(s) by {CreatedBy}")]
    public static partial void VisitCreated(ILogger logger, Guid visitId, string terminalId, int movementCount, string createdBy);

    /// <summary>A visit changed status.</summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="visitId">Identifier of the visit.</param>
    /// <param name="fromStatus">Status before the change.</param>
    /// <param name="toStatus">Status after the change.</param>
    /// <param name="changedBy">Token subject of the caller.</param>
    [LoggerMessage(EventId = 1002, Level = LogLevel.Information,
        Message = "Visit {VisitId} moved from {FromStatus} to {ToStatus} by {ChangedBy}")]
    public static partial void VisitStatusChanged(ILogger logger, Guid visitId, VisitStatus fromStatus, VisitStatus toStatus, string changedBy);
}
