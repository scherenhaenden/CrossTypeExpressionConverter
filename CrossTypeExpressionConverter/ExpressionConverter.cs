using System.Linq.Expressions;
using System.Reflection; // Required for MemberInfo and BindingFlags

namespace CrossTypeExpressionConverter;

/// <summary>
/// Provides functionality to convert LINQ expressions from one type to another, allowing both automatic
/// and manual member mapping, while preserving full <c>IQueryable</c> support (no client‑side gaps).
/// </summary>
public static class ExpressionConverter
{
    /// <summary>
    /// Converts a LINQ expression designed for a source type to an equivalent expression for a destination type.
    /// </summary>
    /// <typeparam name="TSource">The type of the source expression.</typeparam>
    /// <typeparam name="TDestination">The type of the destination expression.</typeparam>
    /// <param name="sourcePredicate">The source expression to convert.</param>
    /// <param name="memberMap">Optional dictionary mapping <typeparamref name="TSource"/> members to their <typeparamref name="TDestination"/> counterparts. This map applies to direct members of TSource.</param>
    /// <param name="customMap">Optional callback to generate custom replacement expressions for any MemberExpression in the source. If it returns <c>null</c> for a given MemberExpression, the visitor falls back to default logic for that member.</param>
    /// <summary>
    /// Converts a predicate expression from a source type to an equivalent predicate for a destination type, enabling cross-type LINQ queries.
    /// </summary>
    /// <param name="sourcePredicate">The predicate expression defined on the source type.</param>
    /// <param name="memberMap">
    /// Optional mapping of member names from the source type to the destination type for direct member accesses.
    /// </param>
    /// <param name="customMap">
    /// Optional callback invoked for each member access; if it returns a non-null expression, that expression replaces the member access.
    /// </param>
    /// <returns>
    /// An expression usable inside an <see cref="IQueryable{T}"/> against <typeparamref name="TDestination"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the expression body cannot be converted or if a mapped member cannot be found on the destination type.
    /// </exception>
    public static Expression<Func<TDestination, bool>> Convert<TSource, TDestination>(
        Expression<Func<TSource, bool>> sourcePredicate,
        IDictionary<string, string>? memberMap = null,
        Func<MemberExpression, ParameterExpression, Expression?>? customMap = null)
    {
        // Use the source predicate's parameter name if available, otherwise default to "p".
        var sourceParameter = sourcePredicate.Parameters.FirstOrDefault();
        var parameterName = sourceParameter?.Name ?? "p";
        var replaceParam = Expression.Parameter(typeof(TDestination), parameterName);

        var visitor = new Visitor<TSource, TDestination>(replaceParam, memberMap, customMap);
        var body = visitor.Visit(sourcePredicate.Body);

        if (body == null)
        {
            throw new InvalidOperationException("The body of the expression could not be converted.");
        }
        return Expression.Lambda<Func<TDestination, bool>>(body, replaceParam);
    }

    /// <summary>
    /// Internal visitor that swaps <typeparamref name="TSource"/> access for <typeparamref name="TDestination"/>, keeping
    /// the expression provider (EF‑Core, LINQ‑to‑SQL, etc.) fully translatable.
    /// </summary>
    private sealed class Visitor<TSource, TDestination> : ExpressionVisitor
    {
        private readonly ParameterExpression _replaceParam;
        private readonly IDictionary<string, string>? _memberMap;
        private readonly Func<MemberExpression, ParameterExpression, Expression?>? _customMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="Visitor{TSource, TDestination}"/> class for converting expression trees between types.
        /// </summary>
        /// <param name="replaceParam">The parameter expression representing the destination type to substitute for the source parameter.</param>
        /// <param name="memberMap">Optional mapping of member names from the source type to the destination type for direct member accesses.</param>
        /// <param name="customMap">Optional callback to provide custom replacement expressions for member accesses; if it returns null, default mapping is used.</param>
        public Visitor(ParameterExpression replaceParam,
                       IDictionary<string, string>? memberMap,
                       Func<MemberExpression, ParameterExpression, Expression?>? customMap)
        {
            _replaceParam = replaceParam;
            _memberMap = memberMap;
            _customMap = customMap;
        }

        /// <summary>
        /// Replaces the parameter of type <typeparamref name="TSource"/> with the destination parameter during expression traversal.
        /// </summary>
        /// <param name="node">The parameter expression to visit.</param>
        /// <returns>The destination parameter if the node matches the source type; otherwise, the original or visited parameter expression.</returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            // Replace the source parameter with the new destination parameter
            return node.Type == typeof(TSource) ? _replaceParam : base.VisitParameter(node);
        }

        /// <summary>
        /// Visits a member access expression and converts it to reference the corresponding member on the destination type, applying custom or mapped member names as needed.
        /// </summary>
        /// <param name="node">The member access expression to visit.</param>
        /// <returns>An expression accessing the mapped member on the destination type or structure.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the member cannot be mapped to the destination type or structure, or if the inner expression cannot be visited.
        /// </exception>
        protected override Expression VisitMember(MemberExpression node)
        {
            // 1. Try customMap first.
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
            string destMemberName = sourceMemberName; // Default to original name

            // Apply memberMap ONLY if the member being accessed was directly on the TSource parameter
            // (which means visitedExpression will be _replaceParam) AND the original member was declared on TSource.
            if (visitedExpression == _replaceParam && node.Member.DeclaringType == typeof(TSource))
            {
                if (_memberMap != null && _memberMap.TryGetValue(sourceMemberName, out var mappedName))
                {
                    destMemberName = mappedName;
                }
                // If no map entry, destMemberName remains sourceMemberName.
                // The member (destMemberName) will be looked up on _replaceParam.Type (i.e., TDestination).
            }
            // For other cases (nested properties on the destination structure, or members of captured variables),
            // destMemberName remains sourceMemberName, and it will be looked up on visitedExpression.Type.
            
            Type targetType = visitedExpression.Type;
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            MemberInfo[] destMembers = targetType.GetMember(destMemberName, bindingFlags);

            if (destMembers == null || destMembers.Length == 0)
            {
                string onPathMessage;
                if (visitedExpression == _replaceParam) {
                    onPathMessage = $"on destination type '{targetType.FullName}'";
                } else {
                    // Check if the expression is derived from _replaceParam to determine if it's a nested property
                    // This check is simplified; a full check would require traversing up node.Expression.
                    // For now, consider it "nested" if not the direct _replaceParam.
                    // If it's a captured variable, targetType will be the type of that variable.
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
