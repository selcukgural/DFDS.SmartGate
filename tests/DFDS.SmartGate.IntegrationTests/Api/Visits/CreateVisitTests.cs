using System.Net;
using DFDS.SmartGate.Application.Visits.Models;
using DFDS.SmartGate.Domain.Visits;
using DFDS.SmartGate.IntegrationTests.Hosting;

namespace DFDS.SmartGate.IntegrationTests.Api.Visits;

[Collection(ApiHost.Name)]
public sealed class CreateVisitTests(ApiFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ValidRequest_Creates_NormalisesIdentifiers_AndStartsHistory()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", terminal));
        var before = DateTimeOffset.UtcNow;

        using var response = await client.PostJsonAsync(VisitApi.Route, new CreateVisitRequest { TerminalId = terminal.ToLowerInvariant() }, _ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var visit = await response.ReadAsAsync<VisitResponse>(_ct);

        Assert.Equal(new Uri($"{ApiFixture.Audience}{VisitApi.Route}/{visit.Id}"), response.Headers.Location);
        Assert.Equal(terminal, visit.TerminalId);
        Assert.Equal(VisitStatus.PreRegistered, visit.CurrentStatus);
        Assert.Equal("alice", visit.CreatedBy);
        Assert.InRange(visit.CreatedTime, before.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));

        // Business rule from the brief: identifiers are upper-cased with all whitespace removed.
        Assert.Equal("TRK001", visit.Truck.UnitNumber);
        Assert.Equal("AB12345", visit.Truck.LicensePlate);
        Assert.Equal("DL1234", visit.Driver.LicenseNumber);
        Assert.Equal("Acme Haulage", visit.Truck.Carrier);

        var history = Assert.Single(visit.StatusHistory);
        Assert.Equal(VisitStatus.PreRegistered, history.Status);
        Assert.Equal("alice", history.ChangedBy);
    }

    [Fact]
    public async Task Movements_DeriveTerminalSideFromType_AndKeepRequestOrder()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", terminal));

        var visit = await VisitApi.CreateAsync(client, terminal, _ct);

        Assert.Collection(
            visit.Movements,
            delivery =>
            {
                Assert.Equal(MovementType.Delivery, delivery.Type);
                Assert.Equal("CONT1", delivery.UnitNumber);
                Assert.Equal("SEGOT", delivery.From);
                Assert.Equal(terminal, delivery.To);
                Assert.Equal("BK-1", delivery.Reference);
            },
            collection =>
            {
                Assert.Equal(MovementType.Collection, collection.Type);
                Assert.Equal("CONT2", collection.UnitNumber);
                Assert.Equal(terminal, collection.From);
                Assert.Equal("NLRTM", collection.To);
                Assert.Null(collection.Reference);
            });
    }

    [Fact]
    public async Task CreatedVisit_IsReadableById_WithTheSameContent()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", terminal));
        var created = await VisitApi.CreateAsync(client, terminal, _ct);

        var fetched = await VisitApi.GetAsync(client, created.Id, _ct);

        VisitAssert.Equivalent(created, fetched);
    }

    [Fact]
    public async Task InvalidFields_Are400_KeyedByJsonPath()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", terminal));
        var request = new CreateVisitRequest
        {
            TerminalId = "DK",
            Truck = new TruckRequest { UnitNumber = "  ", LicensePlate = "ab 12 345" },
            Driver = new DriverRequest { Name = "", LicenseNumber = "dl 1", Phone = "123" },
            Movements = [new MovementRequest { Type = "Delivery", UnitNumber = "cont 1", Location = "SE" }],
        };

        using var response = await client.PostJsonAsync(VisitApi.Route, request, _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadValidationProblemAsync(_ct);
        Assert.Contains("terminalId", problem.Errors.Keys);
        Assert.Contains("truck.unitNumber", problem.Errors.Keys);
        Assert.Contains("driver.name", problem.Errors.Keys);
        Assert.Contains("driver.phone", problem.Errors.Keys);
        Assert.Contains("movements[0].location", problem.Errors.Keys);
    }

    [Fact]
    public async Task NoMovements_Is400()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", terminal));

        using var response = await client.PostJsonAsync(VisitApi.Route, new CreateVisitRequest { TerminalId = terminal, Movements = [] }, _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadValidationProblemAsync(_ct);
        Assert.Contains("movements", problem.Errors.Keys);
    }

    [Fact]
    public async Task MovementLocationEqualToTerminal_IsRejectedByDomain()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", terminal));
        var request = new CreateVisitRequest
        {
            TerminalId = terminal,
            Movements = [new MovementRequest { Type = "Delivery", UnitNumber = "cont 1", Location = terminal }],
        };

        using var response = await client.PostJsonAsync(VisitApi.Route, request, _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadProblemAsync(_ct);
        Assert.Equal("Visit.MovementLocationIsTerminal", problem.Code());
    }

    [Fact]
    public async Task ServerAssignedFieldsInBody_Are400()
    {
        var terminal = Terminals.Next();
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", terminal));
        var json = $$"""
            {
              "id": "{{Guid.CreateVersion7()}}",
              "createdBy": "mallory",
              "currentStatus": "Completed",
              "terminalId": "{{terminal}}",
              "truck": { "unitNumber": "trk 1", "licensePlate": "ab 1" },
              "driver": { "name": "Ada", "licenseNumber": "dl 1" },
              "movements": [ { "type": "Delivery", "unitNumber": "cont 1", "location": "SEGOT" } ]
            }
            """;

        using var response = await client.PostRawJsonAsync(VisitApi.Route, json, _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadProblemAsync(_ct);
        Assert.Equal("The request could not be read.", problem.Title);
    }

    [Fact]
    public async Task TerminalOutsideTheToken_Is403()
    {
        using var client = fixture.CreateClient(fixture.Tokens.For("alice", "DKCPH"));

        using var response = await client.PostJsonAsync(VisitApi.Route, new CreateVisitRequest { TerminalId = "SEGOT" }, _ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.ReadProblemAsync(_ct);
        Assert.Equal("Terminal.AccessDenied", problem.Code());
    }
}
