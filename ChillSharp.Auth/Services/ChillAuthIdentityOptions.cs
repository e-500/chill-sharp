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
    public const string AccessTokenLifetimeMinutesEnvironmentVariable = "CHILLSHARP_AUTH_ACCESS_TOKEN_MINUTES";

    public const string RefreshTokenLifetimeDaysEnvironmentVariable = "CHILLSHARP_AUTH_REFRESH_TOKEN_DAYS";

    public const string OAuthAuthorizationCodeLifetimeMinutesEnvironmentVariable = "CHILLSHARP_AUTH_OAUTH_CODE_MINUTES";

    /// <summary>
    /// Gets or sets the lifetime of issued access tokens. Defaults to
    /// <c>CHILLSHARP_AUTH_ACCESS_TOKEN_MINUTES</c> when present, otherwise 20 minutes.
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = ReadPositiveEnvironmentTimeSpan(
        AccessTokenLifetimeMinutesEnvironmentVariable,
        value => TimeSpan.FromMinutes(value),
        TimeSpan.FromMinutes(20));

    /// <summary>
    /// Gets or sets the lifetime of issued refresh tokens. Defaults to
    /// <c>CHILLSHARP_AUTH_REFRESH_TOKEN_DAYS</c> when present, otherwise 14 days.
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = ReadPositiveEnvironmentTimeSpan(
        RefreshTokenLifetimeDaysEnvironmentVariable,
        value => TimeSpan.FromDays(value),
        TimeSpan.FromDays(14));

    /// <summary>
    /// Gets or sets whether OAuth endpoints for ChatGPT and remote MCP clients are enabled.
    /// </summary>
    public bool EnableOAuthEndpoints { get; set; } = true;

    /// <summary>
    /// Gets or sets the route prefix used by the OAuth endpoints. Defaults to <c>/api/chill-auth/oauth</c>.
    /// </summary>
    public string OAuthBasePath { get; set; } = "/api/chill-auth/oauth";

    /// <summary>
    /// Gets or sets the relative MCP resource path advertised to MCP OAuth clients.
    /// </summary>
    public string OAuthProtectedResourcePath { get; set; } = "/api/chill-mcp";

    /// <summary>
    /// Gets or sets the lifetime of one-time authorization codes issued by the OAuth authorize endpoint.
    /// </summary>
    public TimeSpan OAuthAuthorizationCodeLifetime { get; set; } = ReadPositiveEnvironmentTimeSpan(
        OAuthAuthorizationCodeLifetimeMinutesEnvironmentVariable,
        value => TimeSpan.FromMinutes(value),
        TimeSpan.FromMinutes(5));

    /// <summary>
    /// Gets or sets whether register should also create the matching ChillSharp auth user.
    /// </summary>
    public bool CreateChillAuthUserOnRegister { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the reset-token endpoint returns the generated token in the HTTP response.
    /// </summary>
    public bool ReturnPasswordResetTokensInResponse { get; set; } = true;

    /// <summary>
    /// Gets or sets whether password-reset requests should send an email when the target account exposes an email address.
    /// </summary>
    public bool SendPasswordResetEmails { get; set; }

    /// <summary>
    /// Gets or sets the SMTP host used to send password-reset emails.
    /// </summary>
    public string? SmtpHost { get; set; }

    /// <summary>
    /// Gets or sets the SMTP port used to send password-reset emails.
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Gets or sets whether the SMTP client should use SSL/TLS.
    /// </summary>
    public bool SmtpEnableSsl { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional SMTP user name used for authenticated delivery.
    /// </summary>
    public string? SmtpUserName { get; set; }

    /// <summary>
    /// Gets or sets the optional SMTP password used for authenticated delivery.
    /// </summary>
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// Gets or sets the sender email address used for password-reset messages.
    /// </summary>
    public string? PasswordResetFromEmail { get; set; }

    /// <summary>
    /// Gets or sets the optional sender display name used for password-reset messages.
    /// </summary>
    public string? PasswordResetFromDisplayName { get; set; }

    /// <summary>
    /// Gets or sets the subject line used for password-reset emails.
    /// </summary>
    public string PasswordResetEmailSubject { get; set; } = "Reset your password";

    /// <summary>
    /// Gets or sets the optional base URL used to build a clickable password-reset link.
    /// </summary>
    public string? PasswordResetUrlBase { get; set; }

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

    private static TimeSpan ReadPositiveEnvironmentTimeSpan(string variableName, Func<int, TimeSpan> convert, TimeSpan fallback)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (int.TryParse(rawValue, out var value) && value > 0)
        {
            return convert(value);
        }

        return fallback;
    }
}
