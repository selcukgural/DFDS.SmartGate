using DFDS.SmartGate.Domain.Common;
using FluentValidation;

namespace DFDS.SmartGate.Application.Validation;

/// <summary>
/// Bridges FluentValidation rules to the domain value-object factories so format rules exist exactly once
/// (in the value object) and are merely invoked at the boundary. The failure message is the
/// <see cref="DomainError.Message"/>, which is client-safe by design.
/// </summary>
public static class DomainRuleExtensions
{
    /// <summary>
    /// Fails the rule when <paramref name="factory"/> cannot build the value object from the property value.
    /// </summary>
    /// <typeparam name="T">The object being validated.</typeparam>
    /// <typeparam name="TValue">The value object type.</typeparam>
    /// <param name="rule">The rule builder for a raw string property.</param>
    /// <param name="factory">The value object's <c>Create(raw)</c> factory.</param>
    /// <returns>The rule builder, for chaining.</returns>
    public static IRuleBuilderOptionsConditions<T, string?> MustCreate<T, TValue>(
        this IRuleBuilder<T, string?> rule,
        Func<string?, Result<TValue>> factory)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(factory);

        return rule.Custom((value, context) =>
        {
            var result = factory(value);

            if (result.IsFailure)
            {
                context.AddFailure(result.Error.Value.Message);
            }
        });
    }

    /// <summary>
    /// Fails the rule when <paramref name="factory"/> cannot build the value object from the property value;
    /// the property path (e.g. <c>Movements[1].Location</c>) is passed to the factory so the message names the offending field.
    /// </summary>
    /// <typeparam name="T">The object being validated.</typeparam>
    /// <typeparam name="TValue">The value object type.</typeparam>
    /// <param name="rule">The rule builder for a raw string property.</param>
    /// <param name="factory">The value object's <c>Create(raw, fieldName)</c> factory.</param>
    /// <returns>The rule builder, for chaining.</returns>
    public static IRuleBuilderOptionsConditions<T, string?> MustCreate<T, TValue>(
        this IRuleBuilder<T, string?> rule,
        Func<string?, string, Result<TValue>> factory)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(factory);

        return rule.Custom((value, context) =>
        {
            var result = factory(value, context.PropertyPath);

            if (result.IsFailure)
            {
                context.AddFailure(result.Error.Value.Message);
            }
        });
    }
}
