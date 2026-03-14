using ChillSharp.Api;
using ChillSharp.Auth.Contracts;
using ChillSharp.Auth.Model;
using ChillSharp.Auth.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

internal sealed class ChillAuthEntityAclService : IChillEntityAclService
{
    private readonly IChillAuthService _authService;
    private readonly IChillAuthIdentityResolver _identityResolver;

    public ChillAuthEntityAclService(IChillAuthService authService, IChillAuthIdentityResolver identityResolver)
    {
        _authService = authService;
        _identityResolver = identityResolver;
    }

    public async Task<bool> AuthorizeAsync(ClaimsPrincipal principal, string module, string entityName, ChillEntityAclAction action, CancellationToken cancellationToken = default)
    {
        var externalId = _identityResolver.ResolveExternalId(principal);
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return false;
        }

        var user = await _authService.GetUserByExternalIdAsync(externalId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return false;
        }

        var result = await _authService.EvaluateEntityPermissionAsync(new EvaluateEntityPermissionRequest
        {
            UserGuid = user.Guid,
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
        services.AddScoped<IChillEntityAclService, ChillAuthEntityAclService>();
        return services;
    }
}
