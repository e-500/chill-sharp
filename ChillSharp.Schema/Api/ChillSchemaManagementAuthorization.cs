using ChillSharp.Auth.Api;
using ChillSharp.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChillSharp.Schema.Api;

/// <summary>
/// Allows anonymous access on unsecured hosts but requires internal schema-management access for authenticated users.
/// </summary>
public sealed class ChillSchemaManagementAccessFilter : IAsyncActionFilter
{
    private readonly IChillAuthManagementAccessService _managementAccessService;
    private readonly IChillAuthIdentityResolver _identityResolver;

    public ChillSchemaManagementAccessFilter(IChillAuthManagementAccessService managementAccessService, IChillAuthIdentityResolver identityResolver)
    {
        _managementAccessService = managementAccessService;
        _identityResolver = identityResolver;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        var externalId = _identityResolver.ResolveExternalId(context.HttpContext.User);
        if (string.IsNullOrWhiteSpace(externalId))
        {
            context.Result = new ForbidResult();
            return;
        }

        var isAllowed = await _managementAccessService.HasCapabilityAsync(
            externalId,
            ChillAuthManagementCapability.Schema,
            context.HttpContext.RequestAborted);
        if (!isAllowed)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
