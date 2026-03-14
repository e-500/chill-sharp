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

namespace ChillSharp.Auth.Contracts;

/// <summary>
/// Response payload returned after a successful register, login, or refresh operation.
/// </summary>
public class AuthTokenResponse
{
    /// <summary>
    /// Gets or sets the short-lived bearer access token used on authenticated API calls.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the access token was issued.
    /// </summary>
    public DateTimeOffset AccessTokenIssuedUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the access token expires.
    /// </summary>
    public DateTimeOffset AccessTokenExpiresUtc { get; set; }

    /// <summary>
    /// Gets or sets the long-lived refresh token used to renew the access token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the refresh token expires.
    /// </summary>
    public DateTimeOffset RefreshTokenExpiresUtc { get; set; }

    /// <summary>
    /// Gets or sets the authenticated ASP.NET Core Identity user identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authenticated user name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// Response payload returned after a successful password-change operation.
/// </summary>
public class ChangePasswordResponse
{
    /// <summary>
    /// Gets or sets whether the password was updated successfully.
    /// </summary>
    public bool Succeeded { get; set; }
}

/// <summary>
/// Response payload returned when the server generates a password-reset token.
/// </summary>
public class PasswordResetTokenResponse
{
    /// <summary>
    /// Gets or sets whether the reset request was accepted.
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user for whom the reset token was generated.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the generated reset token.
    /// </summary>
    public string? ResetToken { get; set; }
}

/// <summary>
/// Response payload returned after a successful password-reset confirmation.
/// </summary>
public class ResetPasswordResponse
{
    /// <summary>
    /// Gets or sets whether the reset was completed successfully.
    /// </summary>
    public bool Succeeded { get; set; }
}
