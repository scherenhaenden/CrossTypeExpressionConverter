namespace CrossTypeExpressionConverter;

/// <summary>
/// Options controlling the behaviour of <see cref="ExpressionConverter"/>.
/// Additional options will be added in future versions.
/// </summary>
public class ExpressionConverterOptions
{
    /// <summary>
    /// How missing member mappings are handled during conversion.
    /// </summary>
    public MemberMappingErrorHandling ErrorHandling { get; set; } = MemberMappingErrorHandling.Throw;

    /// <summary>
    /// Optional mapping of source member names to destination member names.
    /// </summary>
    public IDictionary<string, string>? MemberMap { get; set; }

    /// <summary>
    /// Optional callback to provide custom expressions for member accesses.
    /// </summary>
    public Func<MemberExpression, ParameterExpression, Expression?>? CustomMap { get; set; }

    /// <summary>
    /// Whether to validate source/destination models before conversion.
    /// This is a placeholder for future functionality.
    /// </summary>
    public bool ValidateModels { get; set; }

    /// <summary>
    /// Whether to validate custom mapping rules before conversion.
    /// This is a placeholder for future functionality.
    /// </summary>
    public bool ValidateRules { get; set; }
}

/// <summary>
/// Specifies how the converter reacts when a member cannot be mapped to the destination type.
/// </summary>
public enum MemberMappingErrorHandling
{
    /// <summary>Throw an <see cref="InvalidOperationException"/> (default).</summary>
    Throw,
    /// <summary>Ignore the missing member and substitute a default value.</summary>
    Ignore
}
