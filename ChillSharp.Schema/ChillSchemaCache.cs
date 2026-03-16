using ChillSharp.Dto;
using System.Collections.Concurrent;

namespace ChillSharp.Schema;

/// <summary>
/// Thread-safe in-memory cache for Chill schemas.
/// </summary>
public sealed class ChillSchemaCache : IChillSchemaCache
{
    private readonly ConcurrentDictionary<string, ChillDtoSchema> _cache = new();

    public bool TryGet(string chillType, string chillViewCode, out ChillDtoSchema? schema)
    {
        return _cache.TryGetValue(MakeKey(chillType, chillViewCode), out schema);
    }

    public ChillDtoSchema SetSchema(ChillDtoSchema schema)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        var key = MakeKey(schema.ChillType, schema.ChillViewCode);
        _cache.AddOrUpdate(key, schema, (_, _) => schema);
        return schema;
    }

    public void Invalidate(string chillType, string chillViewCode)
    {
        _cache.TryRemove(MakeKey(chillType, chillViewCode), out _);
    }

    public void InvalidateAll()
    {
        _cache.Clear();
    }

    private static string MakeKey(string chillType, string chillViewCode)
    {
        var normalizedType = string.IsNullOrWhiteSpace(chillType) ? "default" : chillType.Trim();
        var normalizedViewCode = string.IsNullOrWhiteSpace(chillViewCode) ? "default" : chillViewCode.Trim();
        return $"{normalizedType}|{normalizedViewCode}";
    }
}
