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

    /// <summary>
    /// Gets or sets whether the startup initializer should create a root Identity account when credentials are configured.
    /// </summary>
    public bool InitializeRootUserOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets the root user name to initialize. When empty, the value can be resolved from environment variables.
    /// </summary>
    public string? RootUserName { get; set; }

    /// <summary>
    /// Gets or sets the root password to initialize. When empty, the value can be resolved from environment variables.
    /// </summary>
    public string? RootPassword { get; set; }

    /// <summary>
    /// Gets or sets the optional root email address. When empty, the value can be resolved from environment variables.
    /// </summary>
    public string? RootEmail { get; set; }

    /// <summary>
    /// Gets or sets the display name copied into the matching ChillSharp auth user.
    /// </summary>
    public string RootDisplayName { get; set; } = "Root";

    /// <summary>
    /// Gets or sets whether the startup initializer should also create the matching ChillSharp auth user.
    /// </summary>
    public bool CreateChillAuthUserForRoot { get; set; } = true;

    /// <summary>
    /// Gets or sets the environment-variable name used to resolve the root user name.
    /// </summary>
    public string RootUserNameEnvironmentVariable { get; set; } = "CHILLSHARP_AUTH_ROOT_USERNAME";

    /// <summary>
    /// Gets or sets the environment-variable name used to resolve the root password.
    /// </summary>
    public string RootPasswordEnvironmentVariable { get; set; } = "CHILLSHARP_AUTH_ROOT_PASSWORD";

    /// <summary>
    /// Gets or sets the environment-variable name used to resolve the optional root email.
    /// </summary>
    public string RootEmailEnvironmentVariable { get; set; } = "CHILLSHARP_AUTH_ROOT_EMAIL";

    /// <summary>
    /// Gets or sets the environment-variable name used to resolve the optional root display name.
    /// </summary>
    public string RootDisplayNameEnvironmentVariable { get; set; } = "CHILLSHARP_AUTH_ROOT_DISPLAY_NAME";
}
