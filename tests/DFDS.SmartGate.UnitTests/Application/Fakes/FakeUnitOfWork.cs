using System.Threading;
using System.Threading.Tasks;
using DFDS.SmartGate.Application.Abstractions;
using DFDS.SmartGate.Domain.Common;

namespace DFDS.SmartGate.UnitTests.Application.Fakes;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCalls { get; private set; }

    public Result NextResult { get; set; } = Result.Success();

    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCalls++;
        return Task.FromResult(NextResult);
    }
}
