using System;

namespace CrossTypeExpressionConverter;

/// <summary>
/// Options to customize behaviour of <see cref="ExpressionConverter"/>.
/// </summary>
public sealed class ExpressionConverterOptions
{
    /// <summary>
    /// Specifies how the converter reacts when a member from the source
    /// expression cannot be mapped to the destination type.
    /// </summary>
    public MemberNotFoundBehavior MemberNotFoundBehavior { get; init; } = MemberNotFoundBehavior.Throw;
}

/// <summary>
/// Behaviour when a member mapping fails.
/// </summary>
public enum MemberNotFoundBehavior
{
    /// <summary>Throw an <see cref="InvalidOperationException"/>.</summary>
    Throw,
    /// <summary>Suppress the error and return an expression evaluating to <c>false</c>.</summary>
    ReturnFalse
}
