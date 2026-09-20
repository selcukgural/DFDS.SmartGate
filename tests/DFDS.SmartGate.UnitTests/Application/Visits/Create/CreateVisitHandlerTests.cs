using System.Threading.Tasks;
using DFDS.SmartGate.Application.Visits.Create;
using DFDS.SmartGate.Domain.Common;
using DFDS.SmartGate.Domain.Visits;
using DFDS.SmartGate.UnitTests.Application.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static DFDS.SmartGate.UnitTests.Application.ApplicationTestData;

namespace DFDS.SmartGate.UnitTests.Application.Visits.Create;

public sealed class CreateVisitHandlerTests
{
    private readonly InMemoryVisitRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private CreateVisitHandler Handler(FakeCallerContext? caller = null) =>
        new(_repository, _unitOfWork, caller ?? Caller(), Clock(), NullLogger<CreateVisitHandler>.Instance);

    [Fact]
    public async Task ValidCommand_CreatesVisit_NormalisesIdentifiers_AndReturnsFullResponse()
    {
        var result = await Handler().HandleAsync(ValidCreateCommand(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var response = result.Value;

        Assert.Equal("DKCPH", response.TerminalId);
        Assert.Equal(VisitStatus.PreRegistered, response.CurrentStatus);
        Assert.Equal("TRK001", response.Truck.UnitNumber);
        Assert.Equal("34ABC123", response.Truck.LicensePlate);
        Assert.Equal("Acme Haulage", response.Truck.Carrier);
        Assert.Equal("DL123", response.Driver.LicenseNumber);
        Assert.Equal("+905321234567", response.Driver.Phone);
        Assert.Equal(Now, response.CreatedTime);
        Assert.Equal(Subject, response.CreatedBy);

        Assert.Collection(
            response.Movements,
            m =>
            {
                Assert.Equal(MovementType.Delivery, m.Type);
                Assert.Equal("TRIST", m.From);
                Assert.Equal("DKCPH", m.To);
                Assert.Equal("BK-1", m.Reference);
            },
            m =>
            {
                Assert.Equal(MovementType.Collection, m.Type);
                Assert.Equal("DKCPH", m.From);
                Assert.Equal("SEGOT", m.To);
                Assert.Null(m.Reference);
            });

        var history = Assert.Single(response.StatusHistory);
        Assert.Equal(VisitStatus.PreRegistered, history.Status);
        Assert.Equal(Subject, history.ChangedBy);

        Assert.Single(_repository.Visits);
        Assert.Equal(1, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task TerminalOutsideCallerClaims_IsForbidden_AndNothingIsSaved()
    {
        var result = await Handler(Caller(Gothenburg)).HandleAsync(ValidCreateCommand("DKCPH"), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, result.Error.Value.Kind);
        Assert.Equal("Terminal.AccessDenied", result.Error.Value.Code);
        Assert.Empty(_repository.Visits);
        Assert.Equal(0, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task MovementAtTerminal_IsRejectedByAggregate_AsValidationError()
    {
        var command = ValidCreateCommand() with
        {
            Movements = [new MovementInput { Type = MovementType.Delivery, UnitNumber = "MSKU1", Location = "DKCPH" }],
        };

        var result = await Handler().HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Visit.MovementLocationIsTerminal", result.Error.Value.Code);
        Assert.Equal(ErrorKind.Validation, result.Error.Value.Kind);
        Assert.Equal(0, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task SaveFailure_IsPropagated()
    {
        _unitOfWork.NextResult = VisitErrors.ConcurrentUpdate;

        var result = await Handler().HandleAsync(ValidCreateCommand(), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Visit.ConcurrentUpdate", result.Error.Value.Code);
    }
}
