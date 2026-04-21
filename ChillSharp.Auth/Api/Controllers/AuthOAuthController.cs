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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace ChillSharp.Auth.Api.Controllers;

/// <summary>
/// Exposes OAuth 2.1 endpoints used by ChatGPT and remote MCP clients.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/chill-auth/oauth")]
public sealed class AuthOAuthController : ControllerBase
{
    private readonly IChillAuthOAuthService _service;
    private readonly ChillAuthIdentityApiOptions _options;

    public AuthOAuthController(IChillAuthOAuthService service, IOptions<ChillAuthIdentityApiOptions> options)
    {
        _service = service;
        _options = options.Value;
    }

    [HttpGet("/.well-known/oauth-authorization-server")]
    public IActionResult AuthorizationServerMetadata()
    {
        if (!_options.EnableOAuthEndpoints)
        {
            return NotFound();
        }

        var issuer = BuildOrigin();
        var oauthBase = BuildAbsoluteUrl(_options.OAuthBasePath);
        return Ok(new
        {
            issuer,
            authorization_endpoint = $"{oauthBase}/authorize",
            token_endpoint = $"{oauthBase}/token",
            registration_endpoint = $"{oauthBase}/register",
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code" },
            code_challenge_methods_supported = new[] { "S256" },
            token_endpoint_auth_methods_supported = new[] { "none" },
            scopes_supported = new[] { "mcp" }
        });
    }

    [HttpGet("/.well-known/oauth-protected-resource")]
    public IActionResult ProtectedResourceMetadata()
    {
        if (!_options.EnableOAuthEndpoints)
        {
            return NotFound();
        }

        return Ok(new
        {
            resource = BuildAbsoluteUrl(_options.OAuthProtectedResourcePath),
            authorization_servers = new[] { BuildOrigin() },
            bearer_methods_supported = new[] { "header" },
            resource_documentation = BuildAbsoluteUrl("/api")
        });
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] OAuthClientRegistrationRequest request)
    {
        if (!_options.EnableOAuthEndpoints)
        {
            return NotFound();
        }

        try
        {
            return StatusCode(StatusCodes.Status201Created, _service.RegisterClient(request));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "invalid_client_metadata", error_description = ex.Message });
        }
    }

    [HttpGet("authorize")]
    public IActionResult Authorize(
        [FromQuery(Name = "response_type")] string responseType,
        [FromQuery(Name = "client_id")] string clientId,
        [FromQuery(Name = "redirect_uri")] string redirectUri,
        [FromQuery(Name = "code_challenge")] string codeChallenge,
        [FromQuery(Name = "code_challenge_method")] string codeChallengeMethod,
        [FromQuery] string? scope,
        [FromQuery] string? state)
    {
        if (!_options.EnableOAuthEndpoints)
        {
            return NotFound();
        }

        try
        {
            return Content(_service.BuildAuthorizationPage(new OAuthAuthorizeRequest
            {
                ResponseType = responseType,
                ClientId = clientId,
                RedirectUri = redirectUri,
                CodeChallenge = codeChallenge,
                CodeChallengeMethod = string.IsNullOrWhiteSpace(codeChallengeMethod) ? "S256" : codeChallengeMethod,
                Scope = scope ?? string.Empty,
                State = state ?? string.Empty
            }), "text/html");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "invalid_request", error_description = ex.Message });
        }
    }

    [HttpPost("authorize")]
    public async Task<IActionResult> AuthorizePost(CancellationToken cancellationToken)
    {
        if (!_options.EnableOAuthEndpoints)
        {
            return NotFound();
        }

        var form = await Request.ReadFormAsync(cancellationToken);
        var request = new OAuthAuthorizeRequest
        {
            ResponseType = form["response_type"].ToString(),
            ClientId = form["client_id"].ToString(),
            RedirectUri = form["redirect_uri"].ToString(),
            CodeChallenge = form["code_challenge"].ToString(),
            CodeChallengeMethod = string.IsNullOrWhiteSpace(form["code_challenge_method"]) ? "S256" : form["code_challenge_method"].ToString(),
            Scope = form["scope"].ToString(),
            State = form["state"].ToString()
        };

        try
        {
            var redirect = await _service.AuthorizeAsync(
                request,
                form["username"].ToString(),
                form["password"].ToString(),
                cancellationToken);
            return Redirect(redirect.ToString());
        }
        catch (UnauthorizedAccessException ex)
        {
            return Content(_service.BuildAuthorizationPage(request, ex.Message), "text/html");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "invalid_request", error_description = ex.Message });
        }
    }

    [HttpPost("token")]
    public async Task<IActionResult> Token(CancellationToken cancellationToken)
    {
        if (!_options.EnableOAuthEndpoints)
        {
            return NotFound();
        }

        var form = await Request.ReadFormAsync(cancellationToken);
        try
        {
            var response = await _service.ExchangeCodeAsync(new OAuthTokenRequest
            {
                GrantType = form["grant_type"].ToString(),
                Code = form["code"].ToString(),
                RedirectUri = form["redirect_uri"].ToString(),
                ClientId = form["client_id"].ToString(),
                CodeVerifier = form["code_verifier"].ToString()
            }, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "invalid_request", error_description = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = "invalid_grant", error_description = ex.Message });
        }
    }

    private string BuildOrigin()
    {
        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}".TrimEnd('/');
    }

    private string BuildAbsoluteUrl(string path)
    {
        var normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path.Trim();
        if (!normalizedPath.StartsWith('/'))
        {
            normalizedPath = "/" + normalizedPath;
        }

        return QueryHelpers.AddQueryString($"{BuildOrigin()}{normalizedPath}", new Dictionary<string, string?>());
    }
}
