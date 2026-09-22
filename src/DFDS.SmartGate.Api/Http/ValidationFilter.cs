using System.Reflection;
using FluentValidation;
using FluentValidation.Results;

namespace DFDS.SmartGate.Api.Http;

/// <summary>
/// Runs the Application-layer <see cref="IValidator{T}"/> of a request before its handler is invoked, turning failures
/// into a 400 <c>ValidationProblemDetails</c> keyed by camelCase JSON path. The validator and the argument position are
/// resolved once when the endpoint is built, so a request costs one synchronous validation and no lookups.
/// </summary>
public static class ValidationFilter
{
    /// <summary>Validates the endpoint argument of type <typeparamref name="TRequest"/> with its registered validator.</summary>
    /// <typeparam name="TRequest">The command or query type bound from the request.</typeparam>
    /// <param name="builder">The endpoint being configured.</param>
    /// <returns><paramref name="builder"/>, for chaining, with a 400 validation-problem response documented.</returns>
    /// <exception cref="InvalidOperationException">The handler has no parameter of type <typeparamref name="TRequest"/>.</exception>
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddEndpointFilterFactory(static (factoryContext, next) =>
        {
            var index = IndexOfParameter<TRequest>(factoryContext.MethodInfo);
            var validator = factoryContext.ApplicationServices.GetRequiredService<IValidator<TRequest>>();

            return invocationContext =>
            {
                var result = validator.Validate(invocationContext.GetArgument<TRequest>(index));

                return result.IsValid
                    ? next(invocationContext)
                    : ValueTask.FromResult<object?>(TypedResults.ValidationProblem(ToErrors(result.Errors)));
            };
        });

        return builder.ProducesValidationProblem();
    }

    /// <summary>Position of the first handler parameter assignable to <typeparamref name="TRequest"/>.</summary>
    /// <typeparam name="TRequest">The parameter type.</typeparam>
    /// <param name="handler">The endpoint handler.</param>
    /// <returns>The zero-based argument index.</returns>
    /// <exception cref="InvalidOperationException">No such parameter exists.</exception>
    public static int IndexOfParameter<TRequest>(MethodInfo handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var parameters = handler.GetParameters();

        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType == typeof(TRequest))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"Endpoint handler '{handler.Name}' has no parameter of type '{typeof(TRequest).Name}'.");
    }

    /// <summary>Groups validation failures by camelCase property path.</summary>
    /// <param name="failures">The failures reported by the validator.</param>
    /// <returns>Field name → messages, in first-seen order.</returns>
    public static Dictionary<string, string[]> ToErrors(IReadOnlyList<ValidationFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);

        var grouped = new Dictionary<string, List<string>>(failures.Count, StringComparer.Ordinal);

        foreach (var failure in failures)
        {
            var key = JsonPropertyPath.ToCamelCase(failure.PropertyName);

            if (!grouped.TryGetValue(key, out var messages))
            {
                messages = [];
                grouped[key] = messages;
            }

            messages.Add(failure.ErrorMessage);
        }

        var errors = new Dictionary<string, string[]>(grouped.Count, StringComparer.Ordinal);

        foreach (var (key, messages) in grouped)
        {
            errors[key] = [.. messages];
        }

        return errors;
    }
}
