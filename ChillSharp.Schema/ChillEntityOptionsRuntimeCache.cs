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

using ChillSharp.Schema.Contracts;
using System.Collections.Concurrent;

namespace ChillSharp.Schema;

/// <summary>
/// Shared runtime cache for entity options resolved from a specific Chill context type.
/// </summary>
public static class ChillEntityOptionsRuntimeCache
{
    private static readonly ConcurrentDictionary<string, ChillDtoEntityOptions> Cache = new();

    public static ChillDtoEntityOptions GetOrAdd(string runtimeContextKey, string chillType, Func<ChillDtoEntityOptions> factory)
    {
        return Cache.GetOrAdd(MakeKey(runtimeContextKey, chillType), _ => factory());
    }

    public static void Invalidate(string runtimeContextKey, string chillType)
    {
        Cache.TryRemove(MakeKey(runtimeContextKey, chillType), out _);
    }

    public static void InvalidateAll()
    {
        Cache.Clear();
    }

    private static string MakeKey(string runtimeContextKey, string chillType)
    {
        var normalizedType = string.IsNullOrWhiteSpace(chillType) ? "default" : chillType.Trim();
        var normalizedContextKey = string.IsNullOrWhiteSpace(runtimeContextKey) ? "default" : runtimeContextKey.Trim();
        return $"{normalizedContextKey}|{normalizedType}";
    }
}
