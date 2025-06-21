using System.Linq.Expressions;

namespace CrossTypeExpressionConverter;

/// <summary>
/// Provides configuration options for <see cref="ExpressionConverter"/>.
/// This class uses a fluent-like API to encourage immutability.
/// </summary>
public sealed class ExpressionConverterOptions
{
    /// <summary>
    /// Gets a default instance of the configuration options.
    /// </summary>
    public static ExpressionConverterOptions Default => new();

    /// <summary>
    /// Determines how member mapping errors are handled. Defaults to <see cref="MemberMappingErrorHandling.Throw"/>.
    /// </summary>
    public MemberMappingErrorHandling ErrorHandling { get; init; }

    /// <summary>
    /// A dictionary mapping source member names to destination member names.
    /// </summary>
    public IDictionary<string, string>? MemberMap { get; init; }

    /// <summary>
    /// Callback used to provide custom replacement expressions for specific members.
    /// </summary>
    public Func<MemberExpression, ParameterExpression, Expression?>? CustomMap { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="ExpressionConverterOptions"/> with default values.
    /// </summary>
    public ExpressionConverterOptions()
    {
        ErrorHandling = MemberMappingErrorHandling.Throw;
        MemberMap = null;
        CustomMap = null;
    }

    /// <summary>
    /// Private constructor used internally for the fluent API.
    /// </summary>
    private ExpressionConverterOptions(
        MemberMappingErrorHandling errorHandling,
        IDictionary<string, string>? memberMap,
        Func<MemberExpression, ParameterExpression, Expression?>? customMap)
    {
        ErrorHandling = errorHandling;
        MemberMap = memberMap;
        CustomMap = customMap;
    }

    /// <summary>
    /// Creates a new options instance with the specified error handling strategy.
    /// </summary>
    public ExpressionConverterOptions WithErrorHandling(MemberMappingErrorHandling errorHandling)
    {
        return new ExpressionConverterOptions(errorHandling, MemberMap, CustomMap);
    }

    /// <summary>
    /// Creates a new options instance with the provided member map.
    /// </summary>
    public ExpressionConverterOptions WithMemberMap(IDictionary<string, string>? memberMap)
    {
        return new ExpressionConverterOptions(ErrorHandling, memberMap, CustomMap);
    }

    /// <summary>
    /// Creates a new options instance with the provided custom mapping delegate.
    /// </summary>
    public ExpressionConverterOptions WithCustomMap(Func<MemberExpression, ParameterExpression, Expression?>? customMap)
    {
        return new ExpressionConverterOptions(ErrorHandling, MemberMap, customMap);
    }
}