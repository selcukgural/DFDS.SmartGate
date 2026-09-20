namespace DFDS.SmartGate.Application.Abstractions;

/// <summary>
/// Handles a state-changing use case. One implementation per command; implementations are discovered and registered
/// by <see cref="DependencyInjection.AddApplication"/>. Cross-cutting behaviour (logging, metrics, …) is added by
/// decorating this interface rather than by editing handlers.
/// </summary>
/// <typeparam name="TCommand">The validated input of the use case.</typeparam>
/// <typeparam name="TResult">The value produced on success.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : class
{
    /// <summary>Executes the use case.</summary>
    /// <param name="command">Input that has already passed its <see cref="FluentValidation.IValidator{T}"/>; handlers do not re-validate it.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The outcome: a value on success or a <see cref="Domain.Common.DomainError"/> for expected failures.</returns>
    Task<Domain.Common.Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
