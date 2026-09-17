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

using ChillSharp.Api;
using System.Collections.Concurrent;

namespace ChillSharp.Auth.Services;

public interface IChillAuthEntityAclCache
{
    bool TryGetUser(string externalId, out ChillAuthEntityAclUserSnapshot? snapshot);
    void SetUser(string externalId, ChillAuthEntityAclUserSnapshot snapshot);
    bool TryGetDecision(string externalId, string module, string entityName, ChillEntityAclAction action, out bool isAllowed);
    void SetDecision(string externalId, string module, string entityName, ChillEntityAclAction action, bool isAllowed);
    void Invalidate(string externalId);
    void InvalidateAll();
}

public sealed class ChillAuthEntityAclCache : IChillAuthEntityAclCache
{
    private readonly ConcurrentDictionary<string, ChillAuthEntityAclUserSnapshot> _userCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<DecisionCacheKey, bool> _decisionCache = new();

    public bool TryGetUser(string externalId, out ChillAuthEntityAclUserSnapshot? snapshot)
    {
        return _userCache.TryGetValue(Normalize(externalId), out snapshot);
    }

    public void SetUser(string externalId, ChillAuthEntityAclUserSnapshot snapshot)
    {
        _userCache[Normalize(externalId)] = snapshot;
    }

    public bool TryGetDecision(string externalId, string module, string entityName, ChillEntityAclAction action, out bool isAllowed)
    {
        return _decisionCache.TryGetValue(DecisionCacheKey.Create(externalId, module, entityName, action), out isAllowed);
    }

    public void SetDecision(string externalId, string module, string entityName, ChillEntityAclAction action, bool isAllowed)
    {
        _decisionCache[DecisionCacheKey.Create(externalId, module, entityName, action)] = isAllowed;
    }

    public void Invalidate(string externalId)
    {
        var normalizedExternalId = Normalize(externalId);
        _userCache.TryRemove(normalizedExternalId, out _);

        foreach (var key in _decisionCache.Keys.Where(x => x.ExternalId == normalizedExternalId))
        {
            _decisionCache.TryRemove(key, out _);
        }
    }

    public void InvalidateAll()
    {
        _userCache.Clear();
        _decisionCache.Clear();
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private readonly record struct DecisionCacheKey(string ExternalId, string Module, string EntityName, ChillEntityAclAction Action)
    {
        public static DecisionCacheKey Create(string externalId, string module, string entityName, ChillEntityAclAction action)
        {
            return new DecisionCacheKey(
                Normalize(externalId),
                Normalize(module),
                Normalize(entityName),
                action);
        }
    }
}

public sealed record ChillAuthEntityAclUserSnapshot(Guid? UserGuid, bool IsActive)
{
    public static readonly ChillAuthEntityAclUserSnapshot Denied = new(null, false);
}
