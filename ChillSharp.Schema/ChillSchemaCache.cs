/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core 
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

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
