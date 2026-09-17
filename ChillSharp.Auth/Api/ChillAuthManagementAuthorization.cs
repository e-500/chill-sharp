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

using ChillSharp.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChillSharp.Auth.Api;

/// <summary>
/// MVC filter that allows anonymous access on unsecured hosts but requires the auth-management flag for authenticated users.
/// </summary>
internal sealed class ChillAuthManagementAccessFilter : IAsyncActionFilter
{
    private readonly IChillAuthManagementAccessService _managementAccessService;
    private readonly IChillAuthIdentityResolver _identityResolver;

    public ChillAuthManagementAccessFilter(IChillAuthManagementAccessService managementAccessService, IChillAuthIdentityResolver identityResolver)
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
            ChillAuthManagementCapability.Permissions,
            context.HttpContext.RequestAborted);
        if (!isAllowed)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
