using System.Linq.Expressions;
using DFDS.SmartGate.Domain.Locations;
using DFDS.SmartGate.Domain.Visits;
using DFDS.SmartGate.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DFDS.SmartGate.Infrastructure.Persistence;

/// <summary>
/// Builds translatable predicates over <see cref="Movement"/> rows and lifts them to the visit level as
/// <c>v.Movements.Any(...)</c>. Each criterion is one small expression; criteria that must hold for the
/// <em>same</em> movement are combined with <see cref="And"/> before lifting. A new movement filter is one more
/// factory method here, not another branch in the read store.
/// </summary>
internal static class MovementPredicates
{
    /// <summary>Matches movements whose origin is the given country or location.</summary>
    /// <param name="filter">Country (2 letters) or location (UN/LOCODE) to match.</param>
    /// <returns>A predicate that translates to an equality on an indexed column.</returns>
    public static Expression<Func<Movement, bool>> FromMatches(LocationFilter filter)
    {
        if (filter.IsCountry)
        {
            var country = filter.Country!.Value.Value;
            return m => EF.Property<string>(m, MovementConfiguration.FromCountryProperty) == country;
        }

        var location = filter.Location!.Value;
        return m => m.From == location;
    }

    /// <summary>Matches movements whose destination is the given country or location.</summary>
    /// <param name="filter">Country (2 letters) or location (UN/LOCODE) to match.</param>
    /// <returns>A predicate that translates to an equality on an indexed column.</returns>
    public static Expression<Func<Movement, bool>> ToMatches(LocationFilter filter)
    {
        if (filter.IsCountry)
        {
            var country = filter.Country!.Value.Value;
            return m => EF.Property<string>(m, MovementConfiguration.ToCountryProperty) == country;
        }

        var location = filter.Location!.Value;
        return m => m.To == location;
    }

    /// <summary>Combines two movement predicates so both must hold for the same row.</summary>
    /// <param name="left">First predicate.</param>
    /// <param name="right">Second predicate; its parameter is rebound to <paramref name="left"/>'s.</param>
    /// <returns><c>left &amp;&amp; right</c> as a single lambda.</returns>
    public static Expression<Func<Movement, bool>> And(Expression<Func<Movement, bool>> left, Expression<Func<Movement, bool>> right)
    {
        var parameter = left.Parameters[0];
        var rightBody = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body);

        return Expression.Lambda<Func<Movement, bool>>(Expression.AndAlso(left.Body, rightBody), parameter);
    }

    /// <summary>Lifts a movement predicate to the visit: <c>v =&gt; v.Movements.Any(predicate)</c>.</summary>
    /// <param name="predicate">The movement predicate.</param>
    /// <returns>A visit predicate that translates to an <c>EXISTS</c> subquery.</returns>
    public static Expression<Func<Visit, bool>> AnyMovement(Expression<Func<Movement, bool>> predicate)
    {
        var visit = Expression.Parameter(typeof(Visit), "v");
        var movements = Expression.Property(visit, nameof(Visit.Movements));
        var any = Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), [typeof(Movement)], movements, predicate);

        return Expression.Lambda<Func<Visit, bool>>(any, visit);
    }

    /// <summary>Rewrites references to one lambda parameter into another so two lambda bodies can share a parameter.</summary>
    /// <param name="source">The parameter to replace.</param>
    /// <param name="target">The parameter to substitute.</param>
    private sealed class ParameterReplacer(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        /// <inheritdoc/>
        protected override Expression VisitParameter(ParameterExpression node) => node == source ? target : base.VisitParameter(node);
    }
}
