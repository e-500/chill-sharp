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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChillSharp.Auth.Services;

internal interface IChillAuthOAuthClientRegistry
{
    OAuthClientRegistrationResponse Register(OAuthClientRegistrationRequest request);

    bool AllowsRedirectUri(string clientId, string redirectUri);
}

internal sealed class ChillAuthOAuthClientRegistry : IChillAuthOAuthClientRegistry
{
    private readonly ConcurrentDictionary<string, OAuthClientRegistrationResponse> _clients = new();

    public OAuthClientRegistrationResponse Register(OAuthClientRegistrationRequest request)
    {
        if (request.RedirectUris.Count == 0 ||
            request.RedirectUris.Any(x => !Uri.TryCreate(x, UriKind.Absolute, out _)))
        {
            throw new ArgumentException("At least one absolute redirect URI is required.");
        }

        var clientId = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var response = new OAuthClientRegistrationResponse
        {
            ClientId = clientId,
            ClientName = string.IsNullOrWhiteSpace(request.ClientName) ? "OAuth MCP client" : request.ClientName.Trim(),
            RedirectUris = request.RedirectUris.Select(x => x.Trim()).Distinct(StringComparer.Ordinal).ToList(),
            ClientIdIssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        _clients[clientId] = response;
        return response;
    }

    public bool AllowsRedirectUri(string clientId, string redirectUri)
    {
        return _clients.TryGetValue(clientId, out var client) &&
            client.RedirectUris.Any(x => string.Equals(x, redirectUri, StringComparison.Ordinal));
    }
}

public interface IChillAuthOAuthService
{
    OAuthClientRegistrationResponse RegisterClient(OAuthClientRegistrationRequest request);

    string BuildAuthorizationPage(OAuthAuthorizeRequest request, string? errorMessage = null);

    Task<Uri> AuthorizeAsync(OAuthAuthorizeRequest request, string userNameOrEmail, string password, CancellationToken cancellationToken = default);

    Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthTokenRequest request, CancellationToken cancellationToken = default);
}

internal sealed class ChillAuthOAuthService<TUser> : IChillAuthOAuthService
    where TUser : class
{
    private const string AuthorizationCodePurpose = "ChillSharp.Auth.OAuth.AuthorizationCode.v1";
    private static readonly ConcurrentDictionary<string, byte> UsedAuthorizationCodeHashes = new();

    private readonly UserManager<TUser> _userManager;
    private readonly IChillAuthTokenService _tokenService;
    private readonly IChillAuthOAuthClientRegistry _clientRegistry;
    private readonly IDataProtector _protector;
    private readonly ChillAuthIdentityApiOptions _options;
    private readonly TimeProvider _timeProvider;

    public ChillAuthOAuthService(
        UserManager<TUser> userManager,
        IChillAuthTokenService tokenService,
        IChillAuthOAuthClientRegistry clientRegistry,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<ChillAuthIdentityApiOptions> options,
        TimeProvider? timeProvider = null)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _clientRegistry = clientRegistry;
        _protector = dataProtectionProvider.CreateProtector(AuthorizationCodePurpose);
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public OAuthClientRegistrationResponse RegisterClient(OAuthClientRegistrationRequest request)
    {
        return _clientRegistry.Register(request);
    }

    public string BuildAuthorizationPage(OAuthAuthorizeRequest request, string? errorMessage = null)
    {
        ValidateAuthorizeRequest(request);

        var title = Html("Authorize ChillSharp MCP");
        var error = string.IsNullOrWhiteSpace(errorMessage)
            ? string.Empty
            : $"<p style=\"color:#9b1c1c\">{Html(errorMessage)}</p>";

        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{{title}}</title>
  <style>
    body { font-family: system-ui, sans-serif; margin: 2rem; max-width: 34rem; }
    label { display: block; margin-top: 1rem; font-weight: 600; }
    input { box-sizing: border-box; width: 100%; padding: .65rem; margin-top: .25rem; }
    button { margin-top: 1.25rem; padding: .7rem 1rem; }
  </style>
</head>
<body>
  <h1>{{title}}</h1>
  <p>Sign in to allow this MCP client to access ChillSharp using your account permissions.</p>
  {{error}}
  <form method="post" action="{{Html(NormalizeBasePath(_options.OAuthBasePath) + "/authorize")}}">
    {{Hidden("response_type", request.ResponseType)}}
    {{Hidden("client_id", request.ClientId)}}
    {{Hidden("redirect_uri", request.RedirectUri)}}
    {{Hidden("code_challenge", request.CodeChallenge)}}
    {{Hidden("code_challenge_method", request.CodeChallengeMethod)}}
    {{Hidden("scope", request.Scope)}}
    {{Hidden("state", request.State)}}
    <label>User name or email<input name="username" autocomplete="username" required></label>
    <label>Password<input name="password" type="password" autocomplete="current-password" required></label>
    <button type="submit">Authorize</button>
  </form>
</body>
</html>
""";
    }

    public async Task<Uri> AuthorizeAsync(OAuthAuthorizeRequest request, string userNameOrEmail, string password, CancellationToken cancellationToken = default)
    {
        ValidateAuthorizeRequest(request);

        var user = await FindUserAsync(userNameOrEmail.Trim());
        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var userId = await _userManager.GetUserIdAsync(user) ?? throw new InvalidOperationException("The authenticated Identity user did not expose a user id.");
        var userName = await _userManager.GetUserNameAsync(user) ?? userNameOrEmail.Trim();
        var now = _timeProvider.GetUtcNow();
        var payload = new AuthorizationCodePayload
        {
            ClientId = request.ClientId,
            RedirectUri = request.RedirectUri,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
            Scope = request.Scope,
            UserId = userId,
            UserName = userName,
            ExpiresUtc = now.Add(_options.OAuthAuthorizationCodeLifetime)
        };

        var code = _protector.Protect(JsonSerializer.Serialize(payload));
        var redirect = QueryHelpers.AddQueryString(request.RedirectUri, "code", code);
        if (!string.IsNullOrWhiteSpace(request.State))
        {
            redirect = QueryHelpers.AddQueryString(redirect, "state", request.State);
        }

        return new Uri(redirect);
    }

    public async Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.GrantType, "authorization_code", StringComparison.Ordinal))
        {
            throw new ArgumentException("Only the authorization_code grant is supported.");
        }

        AuthorizationCodePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<AuthorizationCodePayload>(_protector.Unprotect(request.Code));
        }
        catch
        {
            payload = null;
        }

        if (payload == null || payload.ExpiresUtc <= _timeProvider.GetUtcNow())
        {
            throw new UnauthorizedAccessException("The authorization code is invalid or expired.");
        }

        if (!string.Equals(payload.ClientId, request.ClientId, StringComparison.Ordinal) ||
            !string.Equals(payload.RedirectUri, request.RedirectUri, StringComparison.Ordinal) ||
            !ValidatePkce(payload.CodeChallenge, payload.CodeChallengeMethod, request.CodeVerifier))
        {
            throw new UnauthorizedAccessException("The authorization code could not be verified.");
        }

        if (!UsedAuthorizationCodeHashes.TryAdd(HashToken(request.Code), 0))
        {
            throw new UnauthorizedAccessException("The authorization code has already been used.");
        }

        var token = await _tokenService.IssueAsync(payload.UserId, payload.UserName, cancellationToken);
        return new OAuthTokenResponse
        {
            AccessToken = token.AccessToken,
            ExpiresIn = Math.Max(1, (int)(token.AccessTokenExpiresUtc - token.AccessTokenIssuedUtc).TotalSeconds),
            RefreshToken = token.RefreshToken,
            Scope = payload.Scope
        };
    }

    private void ValidateAuthorizeRequest(OAuthAuthorizeRequest request)
    {
        if (!string.Equals(request.ResponseType, "code", StringComparison.Ordinal))
        {
            throw new ArgumentException("Only response_type=code is supported.");
        }

        if (string.IsNullOrWhiteSpace(request.ClientId) ||
            string.IsNullOrWhiteSpace(request.RedirectUri) ||
            string.IsNullOrWhiteSpace(request.CodeChallenge))
        {
            throw new ArgumentException("client_id, redirect_uri, and code_challenge are required.");
        }

        if (!string.Equals(request.CodeChallengeMethod, "S256", StringComparison.Ordinal))
        {
            throw new ArgumentException("Only the S256 PKCE challenge method is supported.");
        }

        if (!_clientRegistry.AllowsRedirectUri(request.ClientId, request.RedirectUri))
        {
            throw new ArgumentException("The redirect_uri is not registered for this client.");
        }
    }

    private async Task<TUser?> FindUserAsync(string userNameOrEmail)
    {
        var user = await _userManager.FindByNameAsync(userNameOrEmail);
        if (user == null && userNameOrEmail.Contains('@') && _userManager.SupportsUserEmail)
        {
            user = await _userManager.FindByEmailAsync(userNameOrEmail);
        }

        return user;
    }

    private static bool ValidatePkce(string expectedChallenge, string challengeMethod, string verifier)
    {
        if (!string.Equals(challengeMethod, "S256", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(verifier))
        {
            return false;
        }

        var computed = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computed),
            Encoding.ASCII.GetBytes(expectedChallenge));
    }

    private static string HashToken(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string Hidden(string name, string? value)
    {
        return $"<input type=\"hidden\" name=\"{Html(name)}\" value=\"{Html(value ?? string.Empty)}\">";
    }

    private static string Html(string value)
    {
        return System.Net.WebUtility.HtmlEncode(value);
    }

    private static string NormalizeBasePath(string value)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "/api/chill-auth/oauth";
        }

        return normalized.StartsWith('/') ? normalized.TrimEnd('/') : "/" + normalized.TrimEnd('/');
    }

    private sealed class AuthorizationCodePayload
    {
        public string ClientId { get; set; } = string.Empty;

        public string RedirectUri { get; set; } = string.Empty;

        public string CodeChallenge { get; set; } = string.Empty;

        public string CodeChallengeMethod { get; set; } = string.Empty;

        public string Scope { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public DateTimeOffset ExpiresUtc { get; set; }
    }
}

public sealed class OAuthAuthorizeRequest
{
    public string ResponseType { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string CodeChallenge { get; set; } = string.Empty;

    public string CodeChallengeMethod { get; set; } = "S256";

    public string Scope { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;
}

public sealed class OAuthTokenRequest
{
    public string GrantType { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string CodeVerifier { get; set; } = string.Empty;
}
