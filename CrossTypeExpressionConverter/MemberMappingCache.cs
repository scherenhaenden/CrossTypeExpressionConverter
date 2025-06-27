using Microsoft.Extensions.Caching.Memory;
using System;
using System.Reflection;

namespace CrossTypeExpressionConverter;

/// <summary>
/// Provides a thread-safe, static cache for attribute-based member mappings.
/// This cache uses MemoryCache to prevent unbounded growth and potential memory leaks
/// in long-running applications by setting a size limit and an eviction policy.
/// </summary>
internal static class MemberMappingCache
{
    private static readonly MemoryCache Cache = new(
        new MemoryCacheOptions
        {
            // Sets a limit on the number of entries in the cache.
            // Adjust this value based on the expected number of unique MemberInfo entries.
            SizeLimit = 1000,

            // When the size limit is reached, this percentage of the cache is removed.
            CompactionPercentage = 0.2
        });

    private static readonly MemoryCacheEntryOptions CacheEntryOptions = new MemoryCacheEntryOptions()
        // Each entry is given a size of 1. This allows the SizeLimit to function as an item count.
        .SetSize(1)
        // Sets a sliding expiration policy. The entry will be evicted if it hasn't been accessed
        // for this duration. This helps remove stale entries that are no longer in use.
        .SetSlidingExpiration(TimeSpan.FromMinutes(30));

    /// <summary>
    /// Proactively finds all [MapsTo] attributes for a given type's members
    /// and populates the cache. This is a performance optimization to avoid
    /// repeated, individual reflection lookups for each member during expression conversion.
    /// </summary>
    /// <param name="type">The type whose members' mappings will be cached.</param>
    public static void PrimeCacheForType(Type type)
    {
        // Use a simple flag in the cache to ensure we only process each type once.
        string typeProcessedKey = $"processed_{type.FullName}";

        if (!Cache.TryGetValue(typeProcessedKey, out _))
        {
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            foreach (var member in members)
            {
                // This call will populate the cache for each member of the type.
                // The actual reflection only happens if the member is not already cached.
                GetMapping(member);
            }
            
            // Mark this type as processed so we don't repeat the work.
            // The expiration for this flag can be shorter than the member entries themselves.
            Cache.Set(typeProcessedKey, true, TimeSpan.FromHours(1));
        }
    }
    
    /// <summary>
    /// Retrieves the destination member name mapped to the specified member via the MapsToAttribute,
    /// using a cached value if available.
    /// </summary>
    /// <param name="member">The source member for which to retrieve the mapped destination member name.</param>
    /// <returns>The destination member name if a mapping exists; otherwise, null.</returns>
    public static string? GetMapping(MemberInfo member)
    {
        // GetOrCreate attempts to retrieve the value from the cache.
        // If the key is not found, the factory delegate is executed to create the value,
        // which is then added to the cache with the specified options and returned.
        return Cache.GetOrCreate(member, entry =>
        {
            entry.SetOptions(CacheEntryOptions);
            return member.GetCustomAttribute<MapsToAttribute>(true)?.DestinationMemberName;
        });
    }
}