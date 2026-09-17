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
/// Exposes auth-management endpoints for users, roles, permission rules, and management metadata.
/// </summary>
[ApiController]
[Route("api/chill-auth")]
public class AuthManagementController : ControllerBase
{
    #region Fields
    private readonly IChillAuthService _service;
    private readonly IChillAuthIdentityResolver _identityResolver;
    private readonly IChillAuthIdentityService? _identityService;
    #endregion

    #region Construction
    /// <summary>
    /// Initializes the controller with auth services.
    /// </summary>
    public AuthManagementController(
        IChillAuthService service,
        IChillAuthIdentityResolver identityResolver,
        IChillAuthIdentityService? identityService = null)
    {
        _service = service;
        _identityResolver = identityResolver;
        _identityService = identityService;
    }
    #endregion

    #region Current User
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
    #endregion

    #region Management UI
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
    #endregion

    #region Users
    /// <summary>
    /// Returns all authorization users.
    /// </summary>
    [HttpGet("users")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetUsersAsync(cancellationToken));
    }

    /// <summary>
    /// Returns a single authorization user by identifier.
    /// </summary>
    [HttpGet("users/{userGuid:guid}")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetUser(Guid userGuid, CancellationToken cancellationToken)
    {
        var user = await _service.GetUserAsync(userGuid, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Creates a new authorization user.
    /// </summary>
    [HttpPost("users")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> CreateUser([FromBody] CreateAuthUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (_identityService != null)
            {
                request.ExternalId = await _identityService.CreateManagedIdentityUserAsync(request, cancellationToken);
            }

            var user = await _service.CreateUserAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetUser), new { userGuid = user.Guid }, user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing authorization user.
    /// </summary>
    [HttpPut("users/{userGuid:guid}")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> UpdateUser(Guid userGuid, [FromBody] UpdateAuthUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _service.UpdateUserAsync(userGuid, request, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deletes an authorization user.
    /// </summary>
    [HttpDelete("users/{userGuid:guid}")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> DeleteUser(Guid userGuid, CancellationToken cancellationToken)
    {
        return await _service.DeleteUserAsync(userGuid, cancellationToken) ? NoContent() : NotFound();
    }

    /// <summary>
    /// Returns the roles assigned to a user.
    /// </summary>
    [HttpGet("users/{userGuid:guid}/roles")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetUserRoles(Guid userGuid, CancellationToken cancellationToken)
    {
        return Ok(await _service.GetUserRolesAsync(userGuid, cancellationToken));
    }

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    [HttpPut("users/{userGuid:guid}/roles/{roleGuid:guid}")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> AssignRole(Guid userGuid, Guid roleGuid, CancellationToken cancellationToken)
    {
        try
        {
            return await _service.AssignRoleAsync(userGuid, roleGuid, cancellationToken) ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Removes a role assignment from a user.
    /// </summary>
    [HttpDelete("users/{userGuid:guid}/roles/{roleGuid:guid}")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> RemoveRole(Guid userGuid, Guid roleGuid, CancellationToken cancellationToken)
    {
        try
        {
            return await _service.RemoveRoleAsync(userGuid, roleGuid, cancellationToken) ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    #endregion

    #region Roles
    /// <summary>
    /// Returns all authorization roles.
    /// </summary>
    [HttpGet("roles")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetRolesAsync(cancellationToken));
    }

    /// <summary>
    /// Returns a single authorization role by identifier.
    /// </summary>
    [HttpGet("roles/{roleGuid:guid}")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetRole(Guid roleGuid, CancellationToken cancellationToken)
    {
        var role = await _service.GetRoleAsync(roleGuid, cancellationToken);
        return role is null ? NotFound() : Ok(role);
    }

    /// <summary>
    /// Creates a new authorization role.
    /// </summary>
    [HttpPost("roles")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> CreateRole([FromBody] CreateAuthRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var role = await _service.CreateRoleAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetRole), new { roleGuid = role.Guid }, role);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing authorization role.
    /// </summary>
    [HttpPut("roles/{roleGuid:guid}")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> UpdateRole(Guid roleGuid, [FromBody] UpdateAuthRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var role = await _service.UpdateRoleAsync(roleGuid, request, cancellationToken);
            return role is null ? NotFound() : Ok(role);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deletes an authorization role.
    /// </summary>
    [HttpDelete("roles/{roleGuid:guid}")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> DeleteRole(Guid roleGuid, CancellationToken cancellationToken)
    {
        return await _service.DeleteRoleAsync(roleGuid, cancellationToken) ? NoContent() : NotFound();
    }
    #endregion

    #region Permission Rules
    /// <summary>
    /// Returns permission rules filtered by optional user or role identifiers.
    /// </summary>
    [HttpGet("permissions")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetRules([FromQuery] Guid? userGuid, [FromQuery] Guid? roleGuid, CancellationToken cancellationToken)
    {
        return Ok(await _service.GetPermissionRulesAsync(userGuid, roleGuid, cancellationToken));
    }

    /// <summary>
    /// Returns a single permission rule by identifier.
    /// </summary>
    [HttpGet("permissions/{ruleGuid:guid}")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> GetRule(Guid ruleGuid, CancellationToken cancellationToken)
    {
        var rule = await _service.GetPermissionRuleAsync(ruleGuid, cancellationToken);
        return rule is null ? NotFound() : Ok(rule);
    }

    /// <summary>
    /// Creates a new permission rule.
    /// </summary>
    [HttpPost("permissions")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> CreateRule([FromBody] CreateAuthPermissionRuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _service.CreatePermissionRuleAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetRule), new { ruleGuid = rule.Guid }, rule);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing permission rule.
    /// </summary>
    [HttpPut("permissions/{ruleGuid:guid}")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> UpdateRule(Guid ruleGuid, [FromBody] UpdateAuthPermissionRuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _service.UpdatePermissionRuleAsync(ruleGuid, request, cancellationToken);
            return rule is null ? NotFound() : Ok(rule);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deletes a permission rule.
    /// </summary>
    [HttpDelete("permissions/{ruleGuid:guid}")]
    [ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
    public async Task<IActionResult> DeleteRule(Guid ruleGuid, CancellationToken cancellationToken)
    {
        try
        {
            return await _service.DeletePermissionRuleAsync(ruleGuid, cancellationToken) ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    #endregion
}
