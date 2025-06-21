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
        // This convenience overload packages the simple mapping parameters into an ExpressionConverterOptions object.
        var options = new ExpressionConverterOptions
        {
            MemberMap = memberMap,
            CustomMap = customMap
        };
        
        // It then delegates the call to the core overload, ensuring the conversion logic is not duplicated.
        return Convert<TSource, TDestination>(sourcePredicate, options);
    }

    /// <summary>
    /// Converts a predicate expression from a source type to an equivalent predicate for a destination type using a configuration object.
    /// </summary>
    public static Expression<Func<TDestination, bool>> Convert<TSource, TDestination>(
        Expression<Func<TSource, bool>> sourcePredicate,
        ExpressionConverterOptions options)
    {
        // This is the core entry point for the static facade.
        // It creates a new, temporary instance of the main ExpressionConverter and uses it to perform the conversion.
        var converter = new ExpressionConverter(options);
        return converter.Convert<TSource, TDestination>(sourcePredicate);
    }
}