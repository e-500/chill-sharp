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

using ChillSharp.I18n.Contracts;
using System.Collections.Concurrent;

namespace ChillSharp.I18n.Services;

/// <summary>
/// Thread-safe in-memory cache for localized texts.
/// </summary>
public sealed class ChillI18nCache : IChillI18nCache
{
    private readonly ConcurrentDictionary<string, GetTextResponse> _cache = new();

    public bool TryGet(Guid labelGuid, string cultureName, out GetTextResponse? response)
    {
        return _cache.TryGetValue(MakeKey(labelGuid, cultureName), out response);
    }

    public GetTextResponse SetText(GetTextResponse response)
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        var key = MakeKey(response.LabelGuid, response.CultureName);
        _cache.AddOrUpdate(key, response, (_, _) => response);
        return response;
    }

    public void Invalidate(Guid labelGuid, string cultureName)
    {
        _cache.TryRemove(MakeKey(labelGuid, cultureName), out _);
    }

    public void InvalidateAll()
    {
        _cache.Clear();
    }

    private static string MakeKey(Guid labelGuid, string cultureName)
    {
        var normalizedCultureName = string.IsNullOrWhiteSpace(cultureName) ? "default" : cultureName.Trim();
        return $"{labelGuid:N}|{normalizedCultureName}";
    }
}
