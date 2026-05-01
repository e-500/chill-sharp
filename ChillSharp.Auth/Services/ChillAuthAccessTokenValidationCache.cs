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

using ChillSharp.Auth.Model;
using System.Collections.Concurrent;
using System.Threading;

namespace ChillSharp.Auth.Services;

internal interface IChillAuthAccessTokenValidationCache
{
    bool TryGet(Guid sessionGuid, DateTime utcNow, out ChillAuthAccessTokenValidationSnapshot? snapshot);
    void Set(ChillAuthAccessTokenValidationSnapshot snapshot, DateTime utcNow);
    void Remove(Guid sessionGuid);
}

internal sealed class ChillAuthAccessTokenValidationCache : IChillAuthAccessTokenValidationCache
{
    private const int CleanupInterval = 128;
    private readonly ConcurrentDictionary<Guid, ChillAuthAccessTokenValidationSnapshot> _cache = new();
    private long _operationCount;

    public bool TryGet(Guid sessionGuid, DateTime utcNow, out ChillAuthAccessTokenValidationSnapshot? snapshot)
    {
        CleanupExpiredEntriesIfNeeded(utcNow);

        if (!_cache.TryGetValue(sessionGuid, out snapshot))
        {
            return false;
        }

        if (snapshot.ExpiresUtc <= utcNow)
        {
            _cache.TryRemove(sessionGuid, out _);
            snapshot = null;
            return false;
        }

        return true;
    }

    public void Set(ChillAuthAccessTokenValidationSnapshot snapshot, DateTime utcNow)
    {
        CleanupExpiredEntriesIfNeeded(utcNow);

        if (snapshot.ExpiresUtc <= utcNow)
        {
            _cache.TryRemove(snapshot.SessionGuid, out _);
            return;
        }

        _cache[snapshot.SessionGuid] = snapshot;
    }

    public void Remove(Guid sessionGuid)
    {
        _cache.TryRemove(sessionGuid, out _);
    }

    private void CleanupExpiredEntriesIfNeeded(DateTime utcNow)
    {
        if (Interlocked.Increment(ref _operationCount) % CleanupInterval != 0)
        {
            return;
        }

        foreach (var entry in _cache)
        {
            if (entry.Value.ExpiresUtc <= utcNow)
            {
                _cache.TryRemove(entry.Key, out _);
            }
        }
    }
}

internal sealed record ChillAuthAccessTokenValidationSnapshot(Guid SessionGuid, DateTime ExpiresUtc, DateTime? RevokedUtc)
{
    public static ChillAuthAccessTokenValidationSnapshot FromEntity(AuthRefreshToken session)
    {
        return new ChillAuthAccessTokenValidationSnapshot(session.Guid, session.ExpiresUtc, session.RevokedUtc);
    }
}
