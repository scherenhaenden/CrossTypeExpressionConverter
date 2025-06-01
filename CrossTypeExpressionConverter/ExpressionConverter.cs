using System.Linq.Expressions;

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
        /// <param name="memberMap">Optional dictionary mapping <typeparamref name="TSource"/> members to their <typeparamref name="TDestination"/> counterparts.</param>
        /// <param name="customMap">Optional callback to generate custom replacement expressions. If it returns <c>null</c>, the visitor falls back to dictionary or name‑matching logic.</param>
        /// <returns>An expression usable inside an <see cref="IQueryable{T}"/> against <typeparamref name="TDestination"/>.</returns>
        public static Expression<Func<TDestination, bool>> Convert<TSource, TDestination>(
            Expression<Func<TSource, bool>> sourcePredicate,
            IDictionary<string, string>? memberMap = null,
            Func<MemberExpression, ParameterExpression, Expression?>? customMap = null)
        {
            var replaceParam = Expression.Parameter(typeof(TDestination), "p");
            var visitor      = new Visitor<TSource, TDestination>(replaceParam, memberMap, customMap);
            var body         = visitor.Visit(sourcePredicate.Body);
            return Expression.Lambda<Func<TDestination, bool>>(body!, replaceParam);
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
                _memberMap    = memberMap;
                _customMap    = customMap;
            }

            protected override Expression VisitParameter(ParameterExpression node)
                => node.Type == typeof(TSource) ? _replaceParam : base.VisitParameter(node);

            protected override Expression VisitMember(MemberExpression node)
            {
                // Translate only members that belong to TSource
                if (node.Member.DeclaringType != typeof(TSource))
                    return base.VisitMember(node);

                // 1. Delegate override
                if (_customMap is not null && _customMap(node, _replaceParam) is { } manual)
                    return manual;

                // Decide which destination member name to use
                var destName = _memberMap != null && _memberMap.TryGetValue(node.Member.Name, out var mapped)
                                ? mapped
                                : node.Member.Name;

                var destMember = typeof(TDestination).GetMember(destName)[0];

                // *** Critical fix for IQueryable ***
                // Re‑visit the inner expression so we keep any navigation chain (p.Category -> p.Category.Id, etc.).
                var visitedInstance = Visit(node.Expression);
                return Expression.MakeMemberAccess(visitedInstance, destMember);
            }
        }
    }