using DFDS.SmartGate.Application.Abstractions;
using DFDS.SmartGate.Application.Visits.Models;
using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.Domain.Identifiers;
using DFDS.SmartGate.Domain.Locations;
using DFDS.SmartGate.Domain.Visits;
using Microsoft.Extensions.Logging;

namespace DFDS.SmartGate.Application.Visits.Create;

/// <summary>
/// Creates a visit for a terminal the caller is entitled to. Input has passed <see cref="CreateVisitCommandValidator"/>,
/// so value objects are constructed without further checks; the only business rules evaluated here are the
/// aggregate's own (<see cref="Visit.Create"/>).
/// </summary>
/// <param name="repository">Write-side access to visits.</param>
/// <param name="unitOfWork">Commits the new aggregate.</param>
/// <param name="caller">Identity and terminal entitlements of the caller.</param>
/// <param name="timeProvider">Clock; injected so tests can freeze time.</param>
/// <param name="logger">Receives one audit event per created visit.</param>
public sealed class CreateVisitHandler(
    IVisitRepository repository,
    IUnitOfWork unitOfWork,
    ICallerContext caller,
    TimeProvider timeProvider,
    ILogger<CreateVisitHandler> logger) : ICommandHandler<CreateVisitCommand, VisitResponse>
{
    /// <inheritdoc/>
    /// <returns>
    /// The created visit; <see cref="AuthorizationErrors.TerminalAccessDenied"/> when the caller lacks the terminal;
    /// a <see cref="VisitErrors"/> validation error when the aggregate rejects the draft; a conflict when saving fails.
    /// </returns>
    public async Task<Result<VisitResponse>> HandleAsync(CreateVisitCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var terminal = LocationCode.Create(command.TerminalId).Value;

        if (!caller.CanAccess(terminal))
        {
            return AuthorizationErrors.TerminalAccessDenied(terminal);
        }

        var created = Visit.Create(ToDraft(command, terminal, caller.Subject), timeProvider.GetUtcNow());

        if (created.IsFailure)
        {
            return created.Error.Value;
        }

        var visit = created.Value;
        repository.Add(visit);

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (saved.IsFailure)
        {
            return saved.Error.Value;
        }

        VisitAuditLog.VisitCreated(logger, visit.Id, visit.TerminalId.Value, visit.Movements.Count, visit.CreatedBy);

        return VisitMapping.ToResponse(visit);
    }

    /// <summary>Converts validated raw input into domain value objects. Factories cannot fail here; a failure would be a missing validator rule.</summary>
    /// <param name="command">The validated command.</param>
    /// <param name="terminal">The already-parsed terminal.</param>
    /// <param name="createdBy">Token subject of the caller.</param>
    /// <returns>The draft for <see cref="Visit.Create"/>.</returns>
    private static VisitDraft ToDraft(CreateVisitCommand command, LocationCode terminal, string createdBy)
    {
        var truckInput = command.Truck!;
        var driverInput = command.Driver!;
        var movementInputs = command.Movements!;

        var movements = new MovementDraft[movementInputs.Count];

        for (var i = 0; i < movements.Length; i++)
        {
            var m = movementInputs[i]!;

            movements[i] = new MovementDraft(
                m.Type!.Value,
                UnitNumber.Create(m.UnitNumber).Value,
                LocationCode.Create(m.Location).Value,
                m.Reference);
        }

        return new VisitDraft(
            terminal,
            new Truck(UnitNumber.Create(truckInput.UnitNumber).Value, LicensePlate.Create(truckInput.LicensePlate).Value, truckInput.Carrier),
            new Driver(driverInput.Name!, DriverLicenseNumber.Create(driverInput.LicenseNumber).Value, driverInput.Phone),
            movements,
            createdBy);
    }
}
