using System;

namespace CrossTypeExpressionConverter;

/// <summary>
/// Provides configuration options for <see cref="ExpressionConverter"/>.
/// </summary>
public class ExpressionConverterOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the converter should throw an <see cref="InvalidOperationException"/>
    /// when a source member cannot be mapped to the destination type.
    /// The default value is <c>true</c>.
    /// </summary>
    public bool ThrowOnFailedMemberMapping { get; set; } = true;
}
