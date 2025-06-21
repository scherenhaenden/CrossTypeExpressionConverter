using System.Collections.Concurrent;
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
    
    // The cache for attribute lookups. It is static to be shared across all converter instances for max performance.
    private static readonly ConcurrentDictionary<MemberInfo, string?> _mapsToCache = new();

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
            if (_options.CustomMap != null)
            {
                var customResult = _options.CustomMap(node, _replaceParam);
                if (customResult != null)
                {
                    return customResult;
                }
            }

            // Step 2: Recursively visit the expression that this member is being accessed on.
            var visitedExpression = Visit(node.Expression);
            if (visitedExpression == null)
            {
                throw new InvalidOperationException($"Failed to visit the expression for member '{node.Member.Name}'. Source: {node.Expression}");
            }

            // Step 3: Determine the source and destination member names.
            string sourceMemberName = node.Member.Name;
            string destMemberName = sourceMemberName;

            // Step 4: Check for mappings with the correct precedence.
            if (visitedExpression == _replaceParam && node.Member.DeclaringType == typeof(TSource))
            {
                // Precedence 2: MemberMap dictionary
                if (_options.MemberMap != null && _options.MemberMap.TryGetValue(sourceMemberName, out var mappedName))
                {
                    destMemberName = mappedName;
                }
                // Precedence 3: [MapsTo] attribute
                else
                {
                    var attrName = MemberMappingCache.GetMapping(node.Member);
                    if (!string.IsNullOrEmpty(attrName))
                    {
                        destMemberName = attrName;
                    }
                }
            }

            // Step 5: Using reflection, find the corresponding member on the destination type.
            var destMember = visitedExpression.Type.GetMember(destMemberName, BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();

            // Step 6: Handle the case where no corresponding member is found on the destination type.
            if (destMember == null)
            {
                if (_options.ErrorHandling == MemberMappingErrorHandling.ReturnDefault)
                {
                    return Expression.Default(node.Type);
                }

                // This is the single line that was removed, as it's now unused.
                // string mappingDetail = (sourceMemberName == destMemberName)...

                // If error handling is set to Throw, construct and throw a detailed exception.
                throw new InvalidOperationException(
                    $"Member '{sourceMemberName}' from source type '{node.Member.DeclaringType?.FullName ?? "UnknownType"}' " +
                    $"(attempting to map to '{destMemberName}') could not be mapped because the destination member was not found on type '{visitedExpression.Type.FullName}'. " +
                    $"Full source member expression being processed: {node}");
            }

            // Step 7: If a destination member was found, create and return a new MemberExpression.
            return Expression.MakeMemberAccess(visitedExpression, destMember);
        }
    }
}