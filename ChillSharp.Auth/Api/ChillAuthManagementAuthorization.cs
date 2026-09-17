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
    private readonly IChillAuthService _authService;
    private readonly IChillAuthIdentityResolver _identityResolver;

    public ChillAuthManagementAccessFilter(IChillAuthService authService, IChillAuthIdentityResolver identityResolver)
    {
        _authService = authService;
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

        var user = await _authService.GetUserByExternalIdAsync(externalId, context.HttpContext.RequestAborted);
        if (user is not { IsActive: true, CanManagePermissions: true })
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
