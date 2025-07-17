using System.Collections.Concurrent;
using System.Reflection;

namespace CrossTypeExpressionConverter;

/// <summary>
/// Provides a thread-safe, static cache for attribute-based member mappings.
/// This is an internal detail to improve performance by avoiding repeated reflection.
/// </summary>
internal sealed class MemberMappingCache
{
    private static readonly ConcurrentDictionary<MemberInfo, string?> Cache = new();

    /// <summary>
    /// Gets the mapped destination member name for a given source member, using the cache.
    /// <summary>
    /// Retrieves the destination member name mapped to the specified member, using a cached value if available.
    /// </summary>
    /// <param name="member">The source member for which to retrieve the mapped destination member name.</param>
    /// <returns>The destination member name if a mapping exists; otherwise, null.</returns>
    public static string? GetMapping(MemberInfo member)
    {
        return Cache.GetOrAdd(member, static m => 
            m.GetCustomAttribute<MapsToAttribute>(true)?.DestinationMemberName);
    }
}