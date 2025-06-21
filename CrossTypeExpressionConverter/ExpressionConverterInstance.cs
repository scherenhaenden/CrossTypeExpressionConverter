using System;
using System.Linq.Expressions;

namespace CrossTypeExpressionConverter;

/// <summary>
/// Provides an instance-based converter configured with <see cref="ExpressionConverterOptions"/>.
/// </summary>
public sealed class ExpressionConverterInstance : IExpressionConverter
{
    private readonly ExpressionConverterOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionConverterInstance"/> class.
    /// </summary>
    /// <param name="options">The options to apply during conversion.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public ExpressionConverterInstance(ExpressionConverterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Expression<Func<TDestination, bool>> Convert<TSource, TDestination>(Expression<Func<TSource, bool>> sourcePredicate)
    {
        return ExpressionConverter.Convert<TSource, TDestination>(sourcePredicate, _options);
    }
}
