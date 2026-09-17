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

using ChillSharp.Auth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace ChillSharp.Auth.Api;

/// <summary>
/// Defines the default authentication scheme names used by the ChillSharp auth bearer integration.
/// </summary>
public static class ChillAuthIdentityDefaults
{
    /// <summary>
    /// Gets the default HTTP bearer authentication scheme name.
    /// </summary>
    public const string AuthenticationScheme = "Bearer";
}

internal sealed class ChillAuthBearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IChillAuthTokenService _tokenService;

    public ChillAuthBearerAuthenticationHandler(
        IChillAuthTokenService tokenService,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
        _tokenService = tokenService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ResolveBearerToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        var principal = await _tokenService.ValidateAccessTokenAsync(token, Context.RequestAborted);
        if (principal == null)
        {
            return AuthenticateResult.Fail("The bearer token is invalid or expired.");
        }

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private string? ResolveBearerToken()
    {
        if (Request.Headers.TryGetValue("Authorization", out var headerValue))
        {
            var authorizationHeader = headerValue.ToString();
            const string bearerPrefix = "Bearer ";
            if (authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var headerToken = authorizationHeader.Substring(bearerPrefix.Length).Trim();
                if (!string.IsNullOrWhiteSpace(headerToken))
                {
                    return headerToken;
                }
            }
        }

        if (Request.Query.TryGetValue("access_token", out var queryTokenValue))
        {
            var queryToken = queryTokenValue.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(queryToken))
            {
                return queryToken;
            }
        }

        return null;
    }
}

/// <summary>
/// Provides authentication registration helpers for the ChillSharp opaque bearer-token scheme.
/// </summary>
public static class ChillAuthAuthenticationExtensions
{
    /// <summary>
    /// Registers the ChillSharp opaque bearer-token authentication handler on the current authentication builder.
    /// </summary>
    /// <param name="builder">The authentication builder receiving the ChillSharp bearer handler.</param>
    /// <param name="authenticationScheme">The scheme name used by the host application. Defaults to <c>Bearer</c>.</param>
    /// <returns>The updated authentication builder.</returns>
    public static AuthenticationBuilder AddChillAuthBearer(this AuthenticationBuilder builder, string authenticationScheme = ChillAuthIdentityDefaults.AuthenticationScheme)
    {
        return builder.AddScheme<AuthenticationSchemeOptions, ChillAuthBearerAuthenticationHandler>(authenticationScheme, _ => { });
    }
}
