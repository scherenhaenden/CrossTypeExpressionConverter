using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<MemberInfo, string?> _mapsToCache = new();

        private static string? GetAttributeMapping(MemberInfo member)
        {
            return _mapsToCache.GetOrAdd(member, static m =>
                m.GetCustomAttribute<MapsToAttribute>(true)?.DestinationMemberName);
        }

        public Visitor(ParameterExpression replaceParam, ExpressionConverterOptions options)
        {
            _replaceParam = replaceParam;
            _options = options;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node.Type == typeof(TSource) ? _replaceParam : base.VisitParameter(node);
        }

        /// <summary>
        /// Visits a <see cref="MemberExpression"/> (e.g., 'user.Id') to replace it with the corresponding member from the destination type.
        /// This method is the core of the conversion logic, handling the mapping precedence:
        /// 1. Custom Mapping Delegate
        /// 2. Dictionary-based Mapping
        /// 3. Attribute-Based Mapping via <see cref="MapsToAttribute"/>
        /// 4. By-name Matching
        /// </summary>
        /// <param name="node">The MemberExpression node to visit and convert.</param>
        /// <returns>
        /// A new <see cref="MemberExpression"/> pointing to the corresponding member on the destination type,
        /// or a <see cref="DefaultExpression"/> if the member is not found and the error handling is set to <see cref="MemberMappingErrorHandling.ReturnDefault"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if a member cannot be mapped and error handling is set to <see cref="MemberMappingErrorHandling.Throw"/>.</exception>
        protected override Expression VisitMember(MemberExpression node)
        {
            // Step 1: Handle custom mapping first, as it has the highest precedence.
            // The user can provide a function to manually define a replacement expression.
            if (_options.CustomMap != null)
            {
                var customResult = _options.CustomMap(node, _replaceParam);
                if (customResult != null)
                {
                    // If the custom map function returns a result, use it immediately and skip all other logic.
                    return customResult;
                }
            }

            // Step 2: Recursively visit the expression that this member is being accessed on.
            // For example, in 'user.Address.Street', this would first visit 'user.Address'.
            // The result of this visit will be the new base expression (e.g., the converted 'user' or 'user.Address').
            var visitedExpression = Visit(node.Expression);
            if (visitedExpression == null)
            {
                // This should not happen in a valid expression tree, but if it does, provide detailed context.
                throw new InvalidOperationException($"Failed to visit the expression for member '{node.Member.Name}'. Source: {node.Expression}");
            }

            // Step 3: Determine the source and destination member names.
            // By default, assume the destination member will have the same name as the source.
            string sourceMemberName = node.Member.Name;
            string destMemberName = sourceMemberName;

            // Step 4: Check for a dictionary-based mapping.
            // This logic only applies if we are accessing a property directly on the root parameter (e.g., 'user.Id').
            if (visitedExpression == _replaceParam && node.Member.DeclaringType == typeof(TSource))
            {
                if (_options.MemberMap != null && _options.MemberMap.TryGetValue(sourceMemberName, out var mappedName))
                {
                    // If a mapping is found in the dictionary, update the destination name.
                    destMemberName = mappedName;
                }
                else
                {
                    // Step 4b: Check for [MapsTo] attribute on the source member.
                    var attrName = GetAttributeMapping(node.Member);
                    if (!string.IsNullOrEmpty(attrName))
                    {
                        destMemberName = attrName;
                    }
                }
            }

            // Step 5: Using reflection, find the corresponding member on the destination type.
            // For example, if destMemberName is "UserId", it will search for a property named "UserId" on the destination type.
            var destMember = visitedExpression.Type.GetMember(destMemberName, BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();

            // Step 6: Handle the case where no corresponding member is found on the destination type.
            if (destMember == null)
            {
                // Check the configured error handling strategy.
                if (_options.ErrorHandling == MemberMappingErrorHandling.ReturnDefault)
                {
                    // If configured to ignore, return a default value for the member's type (e.g., null, 0, false).
                    // This allows the rest of the LINQ expression to continue evaluating.
                    return Expression.Default(node.Type);
                }

                // If error handling is set to Throw, construct and throw a detailed exception.
                string mappingDetail = (sourceMemberName == destMemberName)
                    ? ""
                    : $" (when trying to map to '{destMemberName}')";

                throw new InvalidOperationException(
                    $"Member '{sourceMemberName}' from source type '{node.Member.DeclaringType?.FullName ?? "UnknownType"}' " +
                    $"(attempting to map to '{destMemberName}') could not be mapped because the destination member was not found on type '{visitedExpression.Type.FullName}'. " +
                    $"Full source member expression being processed: {node}");
            }

            // Step 7: If a destination member was found, create and return a new MemberExpression
            // that accesses this new member on the already-visited expression.
            return Expression.MakeMemberAccess(visitedExpression, destMember);
        }
    }
}
