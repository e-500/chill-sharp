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
using ChillSharp.Auth.Contracts;
using ChillSharp.Auth.Model;
using ChillSharp.Auth.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ChillSharp.Auth.Api;

/// <summary>
/// Options controlling how an authenticated principal is mapped to a ChillSharp auth user.
/// </summary>
public class ChillAuthIdentityOptions
{
    /// <summary>
    /// Gets or sets the ordered list of claim types used to resolve the external auth user identifier.
    /// </summary>
    public IList<string> ExternalIdClaimTypes { get; set; } = new List<string>
    {
        ClaimTypes.NameIdentifier,
        "sub"
    };
}

/// <summary>
/// Resolves the external identity identifier for the current authenticated principal.
/// </summary>
public interface IChillAuthIdentityResolver
{
    /// <summary>
    /// Returns the external identifier used to locate the matching <see cref="AuthUser"/>.
    /// </summary>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The external identifier, or <see langword="null"/> when it cannot be resolved.</returns>
    string? ResolveExternalId(ClaimsPrincipal principal);
}

internal sealed class ChillAuthClaimsIdentityResolver : IChillAuthIdentityResolver
{
    private readonly ChillAuthIdentityOptions _options;

    public ChillAuthClaimsIdentityResolver(IOptions<ChillAuthIdentityOptions> options)
    {
        _options = options.Value;
    }

    public string? ResolveExternalId(ClaimsPrincipal principal)
    {
        foreach (var claimType in _options.ExternalIdClaimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}

internal sealed class ChillAuthUserPreferencesAccessor : IChillAuthUserPreferencesAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IChillAuthIdentityResolver _identityResolver;
    private readonly IChillAuthUserPreferencesCache _cache;

    public ChillAuthUserPreferencesAccessor(
        IHttpContextAccessor httpContextAccessor,
        IChillAuthIdentityResolver identityResolver,
        IChillAuthUserPreferencesCache cache)
    {
        _httpContextAccessor = httpContextAccessor;
        _identityResolver = identityResolver;
        _cache = cache;
    }

    public ChillUserPreferences Current
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return ChillUserPreferences.Empty;
            }

            var externalId = _identityResolver.ResolveExternalId(principal);
            return !string.IsNullOrWhiteSpace(externalId) && _cache.TryGet(externalId, out var preferences)
                ? preferences!
                : ChillUserPreferences.Empty;
        }
    }
}

internal sealed class ChillAuthEntityAclService : IChillEntityAclService
{
    private readonly IChillAuthService _authService;
    private readonly IChillAuthEntityAclCache _cache;
    private readonly IChillAuthIdentityResolver _identityResolver;

    public ChillAuthEntityAclService(IChillAuthService authService, IChillAuthEntityAclCache cache, IChillAuthIdentityResolver identityResolver)
    {
        _authService = authService;
        _cache = cache;
        _identityResolver = identityResolver;
    }

    public async Task<bool> AuthorizeAsync(ClaimsPrincipal principal, string module, string entityName, ChillEntityAclAction action, CancellationToken cancellationToken = default)
    {
        var externalId = _identityResolver.ResolveExternalId(principal);
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return false;
        }

        if (!_cache.TryGetUser(externalId, out var userSnapshot) || userSnapshot == null)
        {
            var user = await _authService.GetUserByExternalIdAsync(externalId, cancellationToken);
            userSnapshot = user == null
                ? ChillAuthEntityAclUserSnapshot.Denied
                : new ChillAuthEntityAclUserSnapshot(user.Guid, user.IsActive);
            _cache.SetUser(externalId, userSnapshot);
        }

        if (!userSnapshot.IsActive || !userSnapshot.UserGuid.HasValue)
        {
            return false;
        }

        if (_cache.TryGetDecision(externalId, module, entityName, action, out var isAllowed))
        {
            return isAllowed;
        }

        var result = await _authService.EvaluateEntityPermissionAsync(new EvaluateEntityPermissionRequest
        {
            UserGuid = userSnapshot.UserGuid.Value,
            Action = action switch
            {
                ChillEntityAclAction.Query => PermissionAction.Query,
                ChillEntityAclAction.Create => PermissionAction.Create,
                ChillEntityAclAction.Update => PermissionAction.Update,
                ChillEntityAclAction.Delete => PermissionAction.Delete,
                _ => throw new ArgumentOutOfRangeException(nameof(action))
            },
            Module = module,
            EntityName = entityName
        }, cancellationToken);

        _cache.SetDecision(externalId, module, entityName, action, result.IsAllowed);
        return result.IsAllowed;
    }
}

/// <summary>
/// Provides DI registration helpers for integrating ASP.NET Core Identity with ChillSharp.Auth.
/// </summary>
public static class ChillAuthIdentityIntegrationExtensions
{
    /// <summary>
    /// Registers the services required to translate authenticated principals into ChillSharp auth users and apply entity ACL checks.
    /// </summary>
    /// <param name="services">The service collection receiving the identity integration services.</param>
    /// <param name="configureOptions">Optional configuration for external-id claim resolution.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddChillAuthIdentityIntegration(this IServiceCollection services, Action<ChillAuthIdentityOptions>? configureOptions = null)
    {
        services.AddOptions<ChillAuthIdentityOptions>();
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddScoped<IChillAuthIdentityResolver, ChillAuthClaimsIdentityResolver>();
        services.AddScoped<IChillAuthUserPreferencesAccessor, ChillAuthUserPreferencesAccessor>();
        services.AddSingleton<IChillAuthEntityAclCache, ChillAuthEntityAclCache>();
        services.AddScoped<IChillEntityAclService, ChillAuthEntityAclService>();
        return services;
    }
}
