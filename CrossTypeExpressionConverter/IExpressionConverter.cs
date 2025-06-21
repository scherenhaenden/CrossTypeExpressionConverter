using System;
using System.Linq.Expressions;

namespace CrossTypeExpressionConverter;

/// <summary>
/// Defines a contract for converting predicate expressions between types.
/// </summary>
public interface IExpressionConverter
{
    /// <summary>
    /// Converts a predicate expression from <typeparamref name="TSource"/> to an equivalent expression for <typeparamref name="TDestination"/>.
    /// </summary>
    /// <param name="sourcePredicate">The source predicate to convert.</param>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <returns>The converted predicate expression.</returns>
    Expression<Func<TDestination, bool>> Convert<TSource, TDestination>(Expression<Func<TSource, bool>> sourcePredicate);
}
