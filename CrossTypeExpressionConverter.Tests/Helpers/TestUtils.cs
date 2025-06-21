using System.Linq.Expressions;

namespace CrossTypeExpressionConverter.Tests.Helpers;

/// <summary>
/// Provides common utility methods for unit tests.
/// </summary>
public static class TestUtils
{
    /// <summary>
    /// Compiles and executes a predicate against the provided item.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <param name="item">The item to test.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>The boolean result of the predicate.</returns>
    public static bool Evaluate<T>(Expression<Func<T, bool>> predicate, T item)
    {
        return predicate.Compile()(item);
    }
}
