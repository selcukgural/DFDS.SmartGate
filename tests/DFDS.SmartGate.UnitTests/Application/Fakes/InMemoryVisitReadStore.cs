using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DFDS.SmartGate.Application.Visits;
using DFDS.SmartGate.Application.Visits.Models;

namespace DFDS.SmartGate.UnitTests.Application.Fakes;

internal sealed class InMemoryVisitReadStore : IVisitReadStore
{
    private readonly Dictionary<Guid, VisitResponse> _visits = [];

    public int GetByIdCalls { get; private set; }

    public VisitSearchCriteria? LastCriteria { get; private set; }

    public PagedItems<VisitSummary> SearchResult { get; set; } = PagedItems.Empty<VisitSummary>();

    public void Add(VisitResponse visit) => _visits[visit.Id] = visit;

    public Task<VisitResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        GetByIdCalls++;
        return Task.FromResult(_visits.GetValueOrDefault(id));
    }

    public Task<PagedItems<VisitSummary>> SearchAsync(VisitSearchCriteria criteria, CancellationToken cancellationToken)
    {
        LastCriteria = criteria;
        return Task.FromResult(SearchResult);
    }
}
