using System;

namespace CrossTypeExpressionConverter;

/// <summary>
/// Options to configure the behaviour of <see cref="ExpressionConverter"/>.
/// </summary>
public class ExpressionConverterOptions
{
    /// <summary>
    /// If true (default), an <see cref="InvalidOperationException"/> is thrown when
    /// a source member cannot be mapped to the destination type. If false, the
    /// converter substitutes <c>default(TMember)</c> for the unmapped member.
    /// </summary>
    public bool ThrowOnFailedMemberMapping { get; set; } = true;

    /// <summary>
    /// Optional validator invoked with the source type before conversion. Can be
    /// used to enforce custom rules about which models are allowed.
    /// </summary>
    public Func<Type, bool>? SourceModelValidator { get; set; }

    /// <summary>
    /// Optional validator invoked with the destination type before conversion.
    /// </summary>
    public Func<Type, bool>? DestinationModelValidator { get; set; }
}
