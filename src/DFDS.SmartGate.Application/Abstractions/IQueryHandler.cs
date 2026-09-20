namespace DFDS.SmartGate.Application.Abstractions;

/// <summary>
/// Handles a read-only use case. Same registration and decoration model as <see cref="ICommandHandler{TCommand, TResult}"/>;
/// kept separate so read-side concerns (caching, read replicas) can be applied to queries only.
/// </summary>
/// <typeparam name="TQuery">The validated input of the query.</typeparam>
/// <typeparam name="TResult">The read model produced on success.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : class
{
    /// <summary>Executes the query.</summary>
    /// <param name="query">Input that has already passed its <see cref="FluentValidation.IValidator{T}"/>; handlers do not re-validate it.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The read model on success or a <see cref="Domain.Common.DomainError"/> for expected failures (e.g. not found).</returns>
    Task<Domain.Common.Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
