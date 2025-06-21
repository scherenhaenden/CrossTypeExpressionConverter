using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CrossTypeExpressionConverter;

/// <summary>
/// Provides a static facade for backward compatibility with older versions of the library.
/// It is recommended to use the instance-based <see cref="ExpressionConverter"/> via Dependency Injection for new development.
/// </summary>
public static class ExpressionConverterFacade
{
    /// <summary>
    /// Converts a predicate expression from a source type to an equivalent predicate for a destination type.
    /// This overload is kept for backward compatibility with pre-2.0 versions where ExpressionConverterOptions didn't exist.
    /// </summary>
    public static Expression<Func<TDestination, bool>> Convert<TSource, TDestination>(
        Expression<Func<TSource, bool>> sourcePredicate,
        IDictionary<string, string>? memberMap = null,
        Func<MemberExpression, ParameterExpression, Expression?>? customMap = null)
    {
        var options = new ExpressionConverterOptions
        {
            MemberMap = memberMap,
            CustomMap = customMap
        };
        var converter = new ExpressionConverter(options);
        return converter.Convert<TSource, TDestination>(sourcePredicate);
    }

    /// <summary>
    /// Converts a predicate expression from a source type to an equivalent predicate for a destination type using a configuration object.
    /// </summary>
    public static Expression<Func<TDestination, bool>> Convert<TSource, TDestination>(
        Expression<Func<TSource, bool>> sourcePredicate,
        ExpressionConverterOptions options)
    {
        var converter = new ExpressionConverter(options);
        return converter.Convert<TSource, TDestination>(sourcePredicate);
    }
}
