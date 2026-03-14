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

namespace ChillSharp.Auth.Services;

/// <summary>
/// Configures the lifetime and endpoint behavior of the ChillSharp Identity integration.
/// </summary>
public class ChillAuthIdentityApiOptions
{
    /// <summary>
    /// Gets or sets the lifetime of issued access tokens.
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Gets or sets the lifetime of issued refresh tokens.
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(14);

    /// <summary>
    /// Gets or sets whether register should also create the matching ChillSharp auth user.
    /// </summary>
    public bool CreateChillAuthUserOnRegister { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the reset-token endpoint returns the generated token in the HTTP response.
    /// </summary>
    public bool ReturnPasswordResetTokensInResponse { get; set; } = true;
}
