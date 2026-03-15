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
/// Exposes endpoints for managing authorization roles.
/// </summary>
[ApiController]
[ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
[Route("api/chill-auth/roles")]
public class AuthRolesController : ControllerBase
{
    private readonly IChillAuthService _service;

    /// <summary>
    /// Initializes the controller with the auth service.
    /// </summary>
    /// <param name="service">The auth service handling role operations.</param>
    public AuthRolesController(IChillAuthService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns all authorization roles.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetRolesAsync(cancellationToken));
    }

    /// <summary>
    /// Returns a single authorization role by identifier.
    /// </summary>
    [HttpGet("{roleGuid:guid}")]
    public async Task<IActionResult> GetRole(Guid roleGuid, CancellationToken cancellationToken)
    {
        var role = await _service.GetRoleAsync(roleGuid, cancellationToken);
        return role is null ? NotFound() : Ok(role);
    }

    /// <summary>
    /// Creates a new authorization role.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateAuthRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await _service.CreateRoleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRole), new { roleGuid = role.Guid }, role);
    }

    /// <summary>
    /// Updates an existing authorization role.
    /// </summary>
    [HttpPut("{roleGuid:guid}")]
    public async Task<IActionResult> UpdateRole(Guid roleGuid, [FromBody] UpdateAuthRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await _service.UpdateRoleAsync(roleGuid, request, cancellationToken);
        return role is null ? NotFound() : Ok(role);
    }

    /// <summary>
    /// Deletes an authorization role.
    /// </summary>
    [HttpDelete("{roleGuid:guid}")]
    public async Task<IActionResult> DeleteRole(Guid roleGuid, CancellationToken cancellationToken)
    {
        return await _service.DeleteRoleAsync(roleGuid, cancellationToken) ? NoContent() : NotFound();
    }
}
