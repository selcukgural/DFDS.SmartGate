using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DFDS.SmartGate.Domain.Visits;

namespace DFDS.SmartGate.UnitTests.Application.Fakes;

internal sealed class InMemoryVisitRepository : IVisitRepository
{
    private readonly Dictionary<Guid, Visit> _visits = [];

    public IReadOnlyCollection<Visit> Visits => _visits.Values;

    public Task<Visit?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_visits.GetValueOrDefault(id));

    public void Add(Visit visit) => _visits.Add(visit.Id, visit);
}
