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

using System.ComponentModel.DataAnnotations;

namespace ChillSharp.Auth.Contracts;

/// <summary>
/// Request payload used to create a new ASP.NET Core Identity account and the matching ChillSharp auth user.
/// </summary>
public class RegisterAuthIdentityRequest
{
    /// <summary>
    /// Gets or sets the unique user name for the new account.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional email address associated with the account.
    /// </summary>
    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the initial password for the account.
    /// </summary>
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name copied into the ChillSharp auth user.
    /// </summary>
    [MaxLength(256)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the preferred culture name used to preset user display preferences.
    /// </summary>
    [MaxLength(64)]
    public string DisplayCultureName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the corresponding ChillSharp auth user should be created automatically.
    /// </summary>
    public bool CreateChillAuthUser { get; set; } = true;
}

/// <summary>
/// Request payload used to authenticate an existing Identity account with user name and password.
/// </summary>
public class LoginAuthIdentityRequest
{
    /// <summary>
    /// Gets or sets the user name or email used to locate the account.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string UserNameOrEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password used to authenticate the account.
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request payload used to exchange a refresh token for a new access token.
/// </summary>
public class RefreshAuthTokenRequest
{
    /// <summary>
    /// Gets or sets the refresh token previously issued by the auth account endpoint.
    /// </summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Request payload used by an authenticated user to change the current password.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Gets or sets the current password.
    /// </summary>
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new password that will replace the current one.
    /// </summary>
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request payload used to ask the server for a password-reset token.
/// </summary>
public class RequestPasswordResetRequest
{
    /// <summary>
    /// Gets or sets the user name or email used to locate the account that needs a reset token.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string UserNameOrEmail { get; set; } = string.Empty;
}

/// <summary>
/// Request payload used to confirm a password reset with a reset token.
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// Gets or sets the Identity user identifier returned during reset-token generation.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password-reset token.
    /// </summary>
    [Required]
    public string ResetToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new password that will be applied to the account.
    /// </summary>
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
