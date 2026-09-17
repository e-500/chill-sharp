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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChillSharp.Auth.Api.Controllers;

/// <summary>
/// Exposes account-oriented endpoints that integrate ASP.NET Core Identity with ChillSharp.Auth token issuance.
/// </summary>
[ApiController]
[Route("api/chill-auth")]
public class AuthAccountController : ControllerBase
{
    #region Fields
    private readonly IChillAuthIdentityService? _service;
    #endregion

    #region Construction
    /// <summary>
    /// Initializes the controller with the Identity-backed auth-account service.
    /// </summary>
    /// <param name="service">The service handling register, login, refresh, and password flows.</param>
    public AuthAccountController(IChillAuthIdentityService? service = null)
    {
        _service = service;
    }
    #endregion

    #region Account Lifecycle
    /// <summary>
    /// Registers a new Identity account and returns the first access-token pair.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterAuthIdentityRequest request, CancellationToken cancellationToken)
    {
        if (_service == null)
        {
            return NotFound("Identity-backed auth endpoints are not enabled for this host.");
        }

        try
        {
            return Ok(await _service.RegisterAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Authenticates an existing account and returns a fresh access-token pair.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginAuthIdentityRequest request, CancellationToken cancellationToken)
    {
        if (_service == null)
        {
            return NotFound("Identity-backed auth endpoints are not enabled for this host.");
        }

        try
        {
            return Ok(await _service.LoginAsync(request, cancellationToken));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Exchanges a refresh token for a new access-token pair.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshAuthTokenRequest request, CancellationToken cancellationToken)
    {
        if (_service == null)
        {
            return NotFound("Identity-backed auth endpoints are not enabled for this host.");
        }

        try
        {
            return Ok(await _service.RefreshAsync(request, cancellationToken));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Revokes the current authenticated session.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (_service == null)
        {
            return NotFound("Identity-backed auth endpoints are not enabled for this host.");
        }

        await _service.LogoutAsync(User, cancellationToken);
        return NoContent();
    }
    #endregion

    #region Password Management
    /// <summary>
    /// Changes the password of the authenticated user.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (_service == null)
        {
            return NotFound("Identity-backed auth endpoints are not enabled for this host.");
        }

        try
        {
            return Ok(await _service.ChangePasswordAsync(User, request, cancellationToken));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Generates a password-reset token for a user.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetRequest request, CancellationToken cancellationToken)
    {
        if (_service == null)
        {
            return NotFound("Identity-backed auth endpoints are not enabled for this host.");
        }

        return Ok(await _service.RequestPasswordResetAsync(request, cancellationToken));
    }

    /// <summary>
    /// Resets a password by using a previously generated reset token.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (_service == null)
        {
            return NotFound("Identity-backed auth endpoints are not enabled for this host.");
        }

        try
        {
            return Ok(await _service.ResetPasswordAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    #endregion
}
