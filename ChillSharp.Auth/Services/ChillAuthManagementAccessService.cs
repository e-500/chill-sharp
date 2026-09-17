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

namespace ChillSharp.Auth.Services;

public enum ChillAuthManagementCapability
{
    Permissions,
    Schema
}

public interface IChillAuthManagementAccessCache
{
    bool TryGet(string externalId, out AuthManagementAccessSnapshot? snapshot);
    void Set(string externalId, AuthManagementAccessSnapshot snapshot);
    void Invalidate(string externalId);
    void InvalidateAll();
}

public sealed class ChillAuthManagementAccessCache : IChillAuthManagementAccessCache
{
    private readonly ConcurrentDictionary<string, AuthManagementAccessSnapshot> _cache = new(StringComparer.Ordinal);

    public bool TryGet(string externalId, out AuthManagementAccessSnapshot? snapshot)
    {
        return _cache.TryGetValue(Normalize(externalId), out snapshot);
    }

    public void Set(string externalId, AuthManagementAccessSnapshot snapshot)
    {
        _cache[Normalize(externalId)] = snapshot;
    }

    public void Invalidate(string externalId)
    {
        _cache.TryRemove(Normalize(externalId), out _);
    }

    public void InvalidateAll()
    {
        _cache.Clear();
    }

    private static string Normalize(string externalId)
    {
        return string.IsNullOrWhiteSpace(externalId) ? string.Empty : externalId.Trim();
    }
}

public interface IChillAuthManagementAccessService
{
    Task<bool> HasCapabilityAsync(string externalId, ChillAuthManagementCapability capability, CancellationToken cancellationToken = default);
    void Invalidate(string externalId);
    void InvalidateAll();
}

public sealed class ChillAuthManagementAccessService : IChillAuthManagementAccessService
{
    private readonly IChillAuthService _authService;
    private readonly IChillAuthManagementAccessCache _cache;

    public ChillAuthManagementAccessService(IChillAuthService authService, IChillAuthManagementAccessCache cache)
    {
        _authService = authService;
        _cache = cache;
    }

    public async Task<bool> HasCapabilityAsync(string externalId, ChillAuthManagementCapability capability, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return false;

        var normalized = externalId.Trim();
        if (!_cache.TryGet(normalized, out var snapshot) || snapshot == null)
        {
            var user = await _authService.GetUserByExternalIdAsync(normalized, cancellationToken);
            snapshot = user == null
                ? AuthManagementAccessSnapshot.Denied
                : AuthManagementAccessSnapshot.FromUser(user);
            _cache.Set(normalized, snapshot);
        }

        if (!snapshot.IsActive)
            return false;

        return capability switch
        {
            ChillAuthManagementCapability.Permissions => snapshot.CanManagePermissions,
            ChillAuthManagementCapability.Schema => snapshot.CanManageSchema,
            _ => false
        };
    }

    public void Invalidate(string externalId)
    {
        _cache.Invalidate(externalId);
    }

    public void InvalidateAll()
    {
        _cache.InvalidateAll();
    }
}

public sealed record AuthManagementAccessSnapshot(bool IsActive, bool CanManagePermissions, bool CanManageSchema)
{
    public static readonly AuthManagementAccessSnapshot Denied = new(false, false, false);

    public static AuthManagementAccessSnapshot FromUser(AuthUser user)
    {
        return new AuthManagementAccessSnapshot(user.IsActive, user.CanManagePermissions, user.CanManageSchema);
    }
}
