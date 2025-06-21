using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CrossTypeExpressionConverter;

/// <summary>
/// Provides functionality to convert LINQ expressions from one type to another based on a configured set of options.
/// This is the main instance-based converter for the library.
/// </summary>
public sealed class ExpressionConverter : IExpressionConverter
{
    private readonly ExpressionConverterOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionConverter"/> class.
    /// </summary>
    /// <param name="options">The options to apply during conversion.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public ExpressionConverter(ExpressionConverterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Expression<Func<TDestination, bool>> Convert<TSource, TDestination>(Expression<Func<TSource, bool>> sourcePredicate)
    {
        if (sourcePredicate == null) throw new ArgumentNullException(nameof(sourcePredicate));

        var sourceParameter = sourcePredicate.Parameters.FirstOrDefault();
        var parameterName = sourceParameter?.Name ?? "p";
        var replaceParam = Expression.Parameter(typeof(TDestination), parameterName);

        var visitor = new Visitor<TSource, TDestination>(replaceParam, _options);
        var body = visitor.Visit(sourcePredicate.Body);

        if (body == null)
        {
            throw new InvalidOperationException("The body of the expression could not be converted.");
        }

        return Expression.Lambda<Func<TDestination, bool>>(body, replaceParam);
    }

    // The entire Visitor logic is now encapsulated within the main instance-based class.
    private sealed class Visitor<TSource, TDestination> : ExpressionVisitor
    {
        private readonly ParameterExpression _replaceParam;
        private readonly ExpressionConverterOptions _options;

        public Visitor(ParameterExpression replaceParam, ExpressionConverterOptions options)
        {
            _replaceParam = replaceParam;
            _options = options;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node.Type == typeof(TSource) ? _replaceParam : base.VisitParameter(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (_options.CustomMap != null)
            {
                var customResult = _options.CustomMap(node, _replaceParam);
                if (customResult != null)
                {
                    return customResult;
                }
            }

            var visitedExpression = Visit(node.Expression);
            if (visitedExpression == null)
            {
                throw new InvalidOperationException($"Failed to visit the expression for member '{node.Member.Name}'.");
            }

            string sourceMemberName = node.Member.Name;
            string destMemberName = sourceMemberName;

            if (visitedExpression == _replaceParam && node.Member.DeclaringType == typeof(TSource))
            {
                if (_options.MemberMap != null && _options.MemberMap.TryGetValue(sourceMemberName, out var mappedName))
                {
                    destMemberName = mappedName;
                }
            }

            var destMember = visitedExpression.Type.GetMember(destMemberName, BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();

            if (destMember == null)
            {
                if (_options.ErrorHandling == MemberMappingErrorHandling.ReturnDefault)
                {
                    return Expression.Default(node.Type);
                }
                throw new InvalidOperationException($"Member '{sourceMemberName}' could not be mapped to destination type '{visitedExpression.Type.FullName}'.");
            }

            return Expression.MakeMemberAccess(visitedExpression, destMember);
        }
    }
}
