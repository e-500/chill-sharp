using ChillSharp.Dto;
using System.Collections.Concurrent;

namespace ChillSharp.Schema;

/// <summary>
/// Thread-safe in-memory cache for Chill schemas.
/// </summary>
public sealed class ChillSchemaCache : IChillSchemaCache
{
    private readonly ConcurrentDictionary<string, ChillDtoSchema> _cache = new();

    public bool TryGet(string chillType, string chillViewCode, string? cultureName, out ChillDtoSchema? schema)
    {
        return _cache.TryGetValue(MakeKey(chillType, chillViewCode, cultureName), out schema);
    }

    public ChillDtoSchema SetSchema(ChillDtoSchema schema, string? cultureName)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        var key = MakeKey(schema.ChillType, schema.ChillViewCode, cultureName);
        _cache.AddOrUpdate(key, schema, (_, _) => schema);
        return schema;
    }

    public void Invalidate(string chillType, string chillViewCode, string? cultureName)
    {
        _cache.TryRemove(MakeKey(chillType, chillViewCode, cultureName), out _);
    }

    public void InvalidateAll()
    {
        _cache.Clear();
    }

    private static string MakeKey(string chillType, string chillViewCode, string? cultureName)
    {
        var normalizedType = NormalizeKey(chillType);
        var normalizedViewCode = NormalizeKey(chillViewCode);
        var normalizedCultureName = NormalizeKey(cultureName);
        return $"{normalizedType}|{normalizedViewCode}|{normalizedCultureName}";
    }

    private static string NormalizeKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "default" : value.Trim();
    }
}
