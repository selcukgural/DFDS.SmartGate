using System;
using DFDS.SmartGate.Application;
using DFDS.SmartGate.Application.Abstractions;
using DFDS.SmartGate.Application.Visits;
using DFDS.SmartGate.Application.Visits.Create;
using DFDS.SmartGate.Application.Visits.GetById;
using DFDS.SmartGate.Application.Visits.Models;
using DFDS.SmartGate.Application.Visits.Search;
using DFDS.SmartGate.Application.Visits.UpdateStatus;
using DFDS.SmartGate.Domain.Visits;
using DFDS.SmartGate.UnitTests.Application.Fakes;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static DFDS.SmartGate.UnitTests.Application.ApplicationTestData;

namespace DFDS.SmartGate.UnitTests.Application;

public sealed class DependencyInjectionTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddHybridCache();
        services.AddScoped<ICallerContext>(_ => Caller());
        services.AddScoped<IUnitOfWork, FakeUnitOfWork>();
        services.AddScoped<IVisitRepository, InMemoryVisitRepository>();
        services.AddScoped<IVisitReadStore, InMemoryVisitReadStore>();
        services.AddApplication();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public void AddApplication_RegistersEveryHandler()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<CreateVisitHandler>(scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateVisitCommand, VisitResponse>>());
        Assert.IsType<UpdateVisitStatusHandler>(scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateVisitStatusCommand, VisitResponse>>());
        Assert.IsType<GetVisitHandler>(scope.ServiceProvider.GetRequiredService<IQueryHandler<GetVisitQuery, VisitResponse>>());
        Assert.IsType<SearchVisitsHandler>(scope.ServiceProvider.GetRequiredService<IQueryHandler<SearchVisitsQuery, SearchVisitsResponse>>());
    }

    [Fact]
    public void AddApplication_RegistersEveryValidator()
    {
        using var provider = BuildProvider();

        Assert.IsType<CreateVisitCommandValidator>(provider.GetRequiredService<IValidator<CreateVisitCommand>>());
        Assert.IsType<UpdateVisitStatusCommandValidator>(provider.GetRequiredService<IValidator<UpdateVisitStatusCommand>>());
        Assert.IsType<GetVisitQueryValidator>(provider.GetRequiredService<IValidator<GetVisitQuery>>());
        Assert.IsType<SearchVisitsQueryValidator>(provider.GetRequiredService<IValidator<SearchVisitsQuery>>());
    }

    [Fact]
    public void AddApplication_DoesNotOverrideHostTimeProvider()
    {
        var services = new ServiceCollection();
        var clock = Clock();
        services.AddSingleton<TimeProvider>(clock);
        services.AddApplication();

        using var provider = services.BuildServiceProvider();

        Assert.Same(clock, provider.GetRequiredService<TimeProvider>());
    }
}
