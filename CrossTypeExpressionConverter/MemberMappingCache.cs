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
    /// Retrieves the destination member name mapped to the specified source member using a cache.
    /// </summary>
    /// <param name="member">The source member for which to retrieve the mapped destination member name.</param>
    public static string? GetMapping(MemberInfo member)
    {
        return Cache.GetOrAdd(member, static m => 
            m.GetCustomAttribute<MapsToAttribute>(true)?.DestinationMemberName);
    }
}