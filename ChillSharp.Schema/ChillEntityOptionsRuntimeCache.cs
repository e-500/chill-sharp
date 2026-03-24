using ChillSharp.Dto;
using System.Collections.Concurrent;

namespace ChillSharp.Schema;

/// <summary>
/// Shared runtime cache for entity options resolved from a specific Chill context type.
/// </summary>
public static class ChillEntityOptionsRuntimeCache
{
    private static readonly ConcurrentDictionary<string, ChillDtoEntityOptions> Cache = new();

    public static ChillDtoEntityOptions GetOrAdd(IChillContext context, string chillType, Func<ChillDtoEntityOptions> factory)
    {
        return Cache.GetOrAdd(MakeKey(context, chillType), _ => factory());
    }

    public static void Invalidate(IChillContext context, string chillType)
    {
        Cache.TryRemove(MakeKey(context, chillType), out _);
    }

    public static void InvalidateAll()
    {
        Cache.Clear();
    }

    private static string MakeKey(IChillContext context, string chillType)
    {
        var normalizedType = string.IsNullOrWhiteSpace(chillType) ? "default" : chillType.Trim();
        return $"{context.GetType().FullName}|{normalizedType}";
    }
}
