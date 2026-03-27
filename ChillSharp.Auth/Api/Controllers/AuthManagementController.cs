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

using ChillSharp.Auth.Contracts;
using ChillSharp.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChillSharp.Auth.Api.Controllers;

/// <summary>
/// Exposes the refactored auth management endpoints.
/// </summary>
[ApiController]
[Route("api/chill-auth")]
public class AuthManagementController : ControllerBase
{
    private readonly IChillAuthService _service;
    private readonly IChillAuthIdentityResolver _identityResolver;

    /// <summary>
    /// Initializes the controller with auth services.
    /// </summary>
    public AuthManagementController(IChillAuthService service, IChillAuthIdentityResolver identityResolver)
    {
        _service = service;
        _identityResolver = identityResolver;
    }

    /// <summary>
    /// Returns the current user's direct permissions, roles, and role permissions.
    /// </summary>
    [HttpGet("get-permissions")]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        if (HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return Ok(new GetAuthPermissionsResponse());
        }

        var externalId = _identityResolver.ResolveExternalId(HttpContext.User);
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return Forbid();
        }

        return Ok(await _service.GetPermissionsAsync(externalId, cancellationToken));
    }

    /// <summary>
    /// Returns the simplified user list used by management UIs.
    /// </summary>
    [HttpGet("get-user-list")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetUserList(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetUserListAsync(cancellationToken));
    }

    /// <summary>
    /// Returns the full managed user payload.
    /// </summary>
    [HttpGet("get-user")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetUser([FromQuery] Guid userGuid, CancellationToken cancellationToken)
    {
        var user = await _service.GetManagedUserAsync(userGuid, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Creates or updates a user together with roles and direct permissions.
    /// </summary>
    [HttpPost("set-user")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> SetUser([FromBody] SetAuthUserRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.SetUserAsync(request, cancellationToken));
    }

    /// <summary>
    /// Returns the simplified role list used by management UIs.
    /// </summary>
    [HttpGet("get-role-list")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetRoleList(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetRoleListAsync(cancellationToken));
    }

    /// <summary>
    /// Returns the distinct logical modules available from the current Chill context.
    /// </summary>
    [HttpGet("get-module-list")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetModuleList(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetModuleListAsync(cancellationToken));
    }

    /// <summary>
    /// Returns the distinct entities available for the specified logical module.
    /// </summary>
    [HttpGet("get-entity-list")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetEntityList([FromQuery] string? module, CancellationToken cancellationToken)
    {
        return Ok(await _service.GetEntityListAsync(module, cancellationToken));
    }

    /// <summary>
    /// Returns the distinct queries available for the specified logical module.
    /// </summary>
    [HttpGet("get-query-list")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetQueryList([FromQuery] string? module, CancellationToken cancellationToken)
    {
        return Ok(await _service.GetQueryListAsync(module, cancellationToken));
    }

    /// <summary>
    /// Returns the distinct properties available for the specified Chill type.
    /// </summary>
    [HttpGet("get-property-list")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetPropertyList([FromQuery] string? chillType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(chillType))
        {
            return BadRequest("ChillType is required.");
        }

        return Ok(await _service.GetPropertyListAsync(chillType, cancellationToken));
    }

    /// <summary>
    /// Returns the full managed role payload.
    /// </summary>
    [HttpGet("get-role")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetRole([FromQuery] Guid roleGuid, CancellationToken cancellationToken)
    {
        var role = await _service.GetManagedRoleAsync(roleGuid, cancellationToken);
        return role is null ? NotFound() : Ok(role);
    }

    /// <summary>
    /// Creates or updates a role together with users and direct permissions.
    /// </summary>
    [HttpPost("set-role")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> SetRole([FromBody] SetAuthRoleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.SetRoleAsync(request, cancellationToken));
    }
}
