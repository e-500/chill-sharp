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
/// Exposes endpoints for managing authorization users and their role assignments.
/// </summary>
[ApiController]
[ServiceFilter(typeof(ChillAuthManagementAccessFilter))]
[Route("api/chill-auth/users")]
public class AuthUsersController : ControllerBase
{
    private readonly IChillAuthService _service;

    /// <summary>
    /// Initializes the controller with the auth service.
    /// </summary>
    /// <param name="service">The auth service handling user operations.</param>
    public AuthUsersController(IChillAuthService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns all authorization users.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetUsersAsync(cancellationToken));
    }

    /// <summary>
    /// Returns a single authorization user by identifier.
    /// </summary>
    [HttpGet("{userGuid:guid}")]
    public async Task<IActionResult> GetUser(Guid userGuid, CancellationToken cancellationToken)
    {
        var user = await _service.GetUserAsync(userGuid, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Creates a new authorization user.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateAuthUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _service.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { userGuid = user.Guid }, user);
    }

    /// <summary>
    /// Updates an existing authorization user.
    /// </summary>
    [HttpPut("{userGuid:guid}")]
    public async Task<IActionResult> UpdateUser(Guid userGuid, [FromBody] UpdateAuthUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _service.UpdateUserAsync(userGuid, request, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Deletes an authorization user.
    /// </summary>
    [HttpDelete("{userGuid:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userGuid, CancellationToken cancellationToken)
    {
        return await _service.DeleteUserAsync(userGuid, cancellationToken) ? NoContent() : NotFound();
    }

    /// <summary>
    /// Returns the roles assigned to a user.
    /// </summary>
    [HttpGet("{userGuid:guid}/roles")]
    public async Task<IActionResult> GetUserRoles(Guid userGuid, CancellationToken cancellationToken)
    {
        return Ok(await _service.GetUserRolesAsync(userGuid, cancellationToken));
    }

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    [HttpPut("{userGuid:guid}/roles/{roleGuid:guid}")]
    public async Task<IActionResult> AssignRole(Guid userGuid, Guid roleGuid, CancellationToken cancellationToken)
    {
        return await _service.AssignRoleAsync(userGuid, roleGuid, cancellationToken) ? NoContent() : NotFound();
    }

    /// <summary>
    /// Removes a role assignment from a user.
    /// </summary>
    [HttpDelete("{userGuid:guid}/roles/{roleGuid:guid}")]
    public async Task<IActionResult> RemoveRole(Guid userGuid, Guid roleGuid, CancellationToken cancellationToken)
    {
        return await _service.RemoveRoleAsync(userGuid, roleGuid, cancellationToken) ? NoContent() : NotFound();
    }
}
