namespace CrossTypeExpressionConverter;

/// <summary>
/// Specifies how the converter reacts when a member cannot be mapped.
/// </summary>
public enum MemberMappingErrorHandling
{
    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> (default behavior).
    /// Use this option to fail fast and ensure all members are mapped correctly.
    /// </summary>
    Throw,

    /// <summary>
    /// Ignores the missing member and substitutes an expression that returns the default value for the member type (e.g., <c>null</c> for reference types).
    /// Be aware that this may cause predicates to evaluate unexpectedly (for example, a condition may silently become false).
    /// This is useful for mapping types that only partially match without halting the application.
    /// </summary>
    ReturnDefault
}