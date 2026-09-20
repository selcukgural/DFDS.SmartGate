using DFDS.SmartGate.Application.Abstractions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DFDS.SmartGate.Application;

/// <summary>Registers the Application layer: every handler and validator in this assembly, discovered once at start-up.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds all <see cref="ICommandHandler{TCommand, TResult}"/> / <see cref="IQueryHandler{TQuery, TResult}"/>
    /// implementations (scoped, they depend on the per-request <see cref="ICallerContext"/>) and all
    /// <see cref="IValidator"/> implementations (singleton, they are stateless). Adding a use case therefore
    /// needs no registration code. <see cref="TimeProvider"/> is registered as the system clock unless the host
    /// supplied one. The host must still provide the ports: <see cref="ICallerContext"/>, <see cref="IUnitOfWork"/>,
    /// <see cref="Domain.Visits.IVisitRepository"/>, <see cref="Visits.IVisitReadStore"/> and a <c>HybridCache</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        foreach (var type in typeof(DependencyInjection).Assembly.GetTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false })
            {
                continue;
            }

            foreach (var contract in type.GetInterfaces())
            {
                if (!contract.IsGenericType)
                {
                    continue;
                }

                var definition = contract.GetGenericTypeDefinition();

                if (definition == typeof(ICommandHandler<,>) || definition == typeof(IQueryHandler<,>))
                {
                    services.AddScoped(contract, type);
                }
                else if (definition == typeof(IValidator<>))
                {
                    services.AddSingleton(contract, type);
                }
            }
        }

        return services;
    }
}
