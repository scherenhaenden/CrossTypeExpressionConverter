using System.Linq.Expressions;
using System.Reflection;

namespace CrossTypeExpressionConverter;

/// <summary>
/// Provides functionality to convert LINQ expressions from one type to another.
/// </summary>
public static class ExpressionConverter
{
    /// <summary>
    /// Converts a predicate expression from a source type to an equivalent predicate for a destination type.
    /// This overload is kept for backward compatibility with pre-2.0 versions where ExpressionConverterOptions didn't exist.
    /// It provides a simpler interface for basic member mapping scenarios.
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
        return ConvertCore<TSource, TDestination>(sourcePredicate, options);
    }

    /// <summary>
    /// Converts a predicate expression from a source type to an equivalent predicate for a destination type using a configuration object.
    /// </summary>
    /// <returns>An expression usable inside an <see cref="IQueryable{T}"/> against <typeparamref name="TDestination"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if sourcePredicate or options are null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the expression body cannot be converted or if a mapped member cannot be found on the destination type (and ErrorHandling is set to Throw).</exception>
    public static Expression<Func<TDestination, bool>> Convert<TSource, TDestination>(
        Expression<Func<TSource, bool>> sourcePredicate,
        ExpressionConverterOptions options)
    {
        return ConvertCore<TSource, TDestination>(sourcePredicate, options);
    }

    private static Expression<Func<TDestination, bool>> ConvertCore<TSource, TDestination>(
        Expression<Func<TSource, bool>> sourcePredicate,
        ExpressionConverterOptions options)
    {
        if (sourcePredicate == null) throw new ArgumentNullException(nameof(sourcePredicate));
        if (options == null) throw new ArgumentNullException(nameof(options));

        var sourceParameter = sourcePredicate.Parameters.FirstOrDefault();
        var parameterName = sourceParameter?.Name ?? "p";
        var replaceParam = Expression.Parameter(typeof(TDestination), parameterName);

        var visitor = new Visitor<TSource, TDestination>(replaceParam, options);
        var body = visitor.Visit(sourcePredicate.Body);

        if (body == null)
        {
            throw new InvalidOperationException("The body of the expression could not be converted.");
        }

        return Expression.Lambda<Func<TDestination, bool>>(body, replaceParam);
    }

    private sealed class Visitor<TSource, TDestination> : ExpressionVisitor
    {
        private readonly ParameterExpression _replaceParam;
        private readonly IDictionary<string, string>? _memberMap;
        private readonly Func<MemberExpression, ParameterExpression, Expression?>? _customMap;
        private readonly MemberMappingErrorHandling _errorHandling;

        public Visitor(ParameterExpression replaceParam, ExpressionConverterOptions options)
        {
            _replaceParam = replaceParam;
            _memberMap = options.MemberMap;
            _customMap = options.CustomMap;
            _errorHandling = options.ErrorHandling;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node.Type == typeof(TSource) ? _replaceParam : base.VisitParameter(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (_customMap != null)
            {
                var customResult = _customMap(node, _replaceParam);
                if (customResult != null)
                {
                    return customResult;
                }
            }

            Expression? visitedExpression = Visit(node.Expression);

            if (visitedExpression == null)
            {
                throw new InvalidOperationException($"Failed to visit the expression for member '{node.Member.Name}'. Source: {node.Expression}");
            }

            string sourceMemberName = node.Member.Name;
            string destMemberName = sourceMemberName;

            if (visitedExpression == _replaceParam && node.Member.DeclaringType == typeof(TSource))
            {
                if (_memberMap != null && _memberMap.TryGetValue(sourceMemberName, out var mappedName))
                {
                    destMemberName = mappedName;
                }
            }
            
            Type targetType = visitedExpression.Type;
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            MemberInfo[] destMembers = targetType.GetMember(destMemberName, bindingFlags);

            if (destMembers == null || destMembers.Length == 0)
            {
                if (_errorHandling == MemberMappingErrorHandling.ReturnDefault)
                {
                    // Correctly returns a default value for the member's type,
                    // allowing the rest of the expression to evaluate.
                    return Expression.Default(node.Type);
                }

                // Logic to create a detailed error message...
                string onPathMessage;
                if (visitedExpression == _replaceParam) {
                    onPathMessage = $"on destination type '{targetType.FullName}'";
                } else {
                    // Simplified check for nested properties for the error message
                    bool isLikelyNestedOnDestination = false;
                    Expression? current = visitedExpression;
                    while(current is MemberExpression memberExpr) {
                        current = memberExpr.Expression;
                        if (current == _replaceParam) {
                            isLikelyNestedOnDestination = true;
                            break;
                        }
                    }
                    if (current == _replaceParam) isLikelyNestedOnDestination = true;

                    if (isLikelyNestedOnDestination) {
                        onPathMessage = $"on nested type '{targetType.FullName}' (derived from '{_replaceParam.Name}')";
                    } else {
                        onPathMessage = $"on type '{targetType.FullName}' (from expression '{visitedExpression.ToString()}')";
                    }
                }
                
                throw new InvalidOperationException(
                    $"Member '{sourceMemberName}' from source type '{node.Member.DeclaringType?.FullName ?? "UnknownType"}' " +
                    $"(attempting to map to '{destMemberName}') could not be mapped because the destination member was not found {onPathMessage}. " +
                    $"Full source member expression being processed: {node.ToString()}");
            }
            
            MemberInfo destMember = destMembers[0];
            return Expression.MakeMemberAccess(visitedExpression, destMember);
        }
    }
}