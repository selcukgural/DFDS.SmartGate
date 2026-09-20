using System;
using DFDS.SmartGate.Application.Visits.UpdateStatus;
using DFDS.SmartGate.Domain.Visits;
using FluentValidation.TestHelper;
using Xunit;

namespace DFDS.SmartGate.UnitTests.Application.Visits.UpdateStatus;

public sealed class UpdateVisitStatusCommandValidatorTests
{
    private readonly UpdateVisitStatusCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_HasNoErrors()
    {
        var command = new UpdateVisitStatusCommand { VisitId = Guid.CreateVersion7(), Status = VisitStatus.AtGate, Reason = "Lane 3" };

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyId_MissingStatus_AndLongReason_AreReported()
    {
        var command = new UpdateVisitStatusCommand { VisitId = Guid.Empty, Status = null, Reason = new string('r', 501) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.VisitId);
        result.ShouldHaveValidationErrorFor(x => x.Status);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void UnknownStatusValue_IsReported()
    {
        var command = new UpdateVisitStatusCommand { VisitId = Guid.CreateVersion7(), Status = (VisitStatus)42 };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Status);
    }
}
