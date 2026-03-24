using ChillSharp.Dto;
using System.Collections.Concurrent;

namespace ChillSharp.Schema;

/// <summary>
/// Thread-safe in-memory cache for Chill schemas.
/// </summary>
public sealed class ChillSchemaCache : IChillSchemaCache
{
    private readonly ConcurrentDictionary<string, ChillDtoSchema> _cache = new();
    private readonly ConcurrentDictionary<string, ChillDtoEntityOptions> _entityOptionsCache = new();

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

    public bool TryGetEntityOptions(string chillType, out ChillDtoEntityOptions? entityOptions)
    {
        return _entityOptionsCache.TryGetValue(NormalizeKey(chillType), out entityOptions);
    }

    public ChillDtoEntityOptions SetEntityOptions(ChillDtoEntityOptions entityOptions)
    {
        if (entityOptions is null)
        {
            throw new ArgumentNullException(nameof(entityOptions));
        }

        var normalizedType = NormalizeKey(entityOptions.ChillType);
        entityOptions.ChillType = normalizedType;
        _entityOptionsCache.AddOrUpdate(normalizedType, entityOptions, (_, _) => entityOptions);
        return entityOptions;
    }

    public void Invalidate(string chillType, string chillViewCode, string? cultureName)
    {
        _cache.TryRemove(MakeKey(chillType, chillViewCode, cultureName), out _);
    }

    public void InvalidateEntityOptions(string chillType)
    {
        _entityOptionsCache.TryRemove(NormalizeKey(chillType), out _);
    }

    public void InvalidateAll()
    {
        _cache.Clear();
        _entityOptionsCache.Clear();
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
