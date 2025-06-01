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
    /// <returns>An expression usable inside an <see cref="IQueryable{T}"/> against <typeparamref name="TDestination"/>.</returns>
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

        public Visitor(ParameterExpression replaceParam,
                       IDictionary<string, string>? memberMap,
                       Func<MemberExpression, ParameterExpression, Expression?>? customMap)
        {
            _replaceParam = replaceParam;
            _memberMap = memberMap;
            _customMap = customMap;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            // Replace the source parameter with the new destination parameter
            return node.Type == typeof(TSource) ? _replaceParam : base.VisitParameter(node);
        }

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
            string destMemberName = sourceMemberName;

            if (node.Member.DeclaringType == typeof(TSource) && _memberMap != null)
            {
                if (_memberMap.TryGetValue(sourceMemberName, out var mappedName))
                {
                    destMemberName = mappedName;
                }
            }
            
            Type targetType = visitedExpression.Type;
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            MemberInfo[] destMembers = targetType.GetMember(destMemberName, bindingFlags);

            if (destMembers == null || destMembers.Length == 0)
            {
                string onPathMessage = (visitedExpression == _replaceParam)
                    ? $"on destination type '{targetType.FullName}'"
                    : $"on nested type '{targetType.FullName}' (accessed via a path starting with '{_replaceParam.Name}')";

                // Adjusted error message to include "could not be mapped"
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