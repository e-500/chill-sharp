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
using System.Security.Claims;

namespace ChillSharp.Auth.Services;

/// <summary>
/// Exposes ASP.NET Core Identity account flows integrated with ChillSharp.Auth users and refresh-token sessions.
/// </summary>
public interface IChillAuthIdentityService
{
    /// <summary>
    /// Registers a new Identity account and optionally creates the matching ChillSharp auth user.
    /// </summary>
    Task<AuthTokenResponse> RegisterAsync(RegisterAuthIdentityRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates an existing account and issues a new access-token pair.
    /// </summary>
    Task<AuthTokenResponse> LoginAsync(LoginAuthIdentityRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new Identity-backed account for auth management flows and returns the generated external identity identifier.
    /// </summary>
    Task<string> CreateManagedIdentityUserAsync(CreateAuthUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a refresh token for a rotated access-token pair.
    /// </summary>
    Task<AuthTokenResponse> RefreshAsync(RefreshAuthTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the current authenticated refresh-token session.
    /// </summary>
    Task LogoutAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the password of the currently authenticated user.
    /// </summary>
    Task<ChangePasswordResponse> ChangePasswordAsync(ClaimsPrincipal principal, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a password-reset token for a user located by user name or email.
    /// </summary>
    Task<PasswordResetTokenResponse> RequestPasswordResetAsync(RequestPasswordResetRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a password by using a previously generated reset token.
    /// </summary>
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
