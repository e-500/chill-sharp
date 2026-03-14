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
using ChillSharp.Auth.Model;
using ChillSharp.Auth.Api;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChillSharp.Auth.Services;

internal interface IChillAuthTokenService
{
    Task<AuthTokenResponse> IssueAsync(string userId, string userName, CancellationToken cancellationToken = default);

    Task<AuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<ClaimsPrincipal?> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);
}

internal sealed class ChillAuthTokenService : IChillAuthTokenService
{
    private const string AccessTokenPurpose = "ChillSharp.Auth.AccessToken.v1";

    private readonly IChillAuthDbContext _context;
    private readonly IDataProtector _protector;
    private readonly ChillAuthIdentityApiOptions _options;
    private readonly TimeProvider _timeProvider;

    public ChillAuthTokenService(
        IChillAuthDbContext context,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<ChillAuthIdentityApiOptions> options,
        TimeProvider? timeProvider = null)
    {
        _context = context;
        _protector = dataProtectionProvider.CreateProtector(AccessTokenPurpose);
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<AuthTokenResponse> IssueAsync(string userId, string userName, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var refreshToken = CreateClientToken();
        var session = new AuthRefreshToken
        {
            Guid = Guid.NewGuid(),
            IdentityUserId = userId.Trim(),
            UserName = userName.Trim(),
            TokenHash = HashToken(refreshToken),
            CreatedUtc = now.UtcDateTime,
            ExpiresUtc = now.Add(_options.RefreshTokenLifetime).UtcDateTime
        };

        _context.RefreshTokens.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return BuildTokenResponse(session, refreshToken, now);
    }

    public async Task<AuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var session = await FindValidRefreshSessionAsync(refreshToken, now, cancellationToken);
        if (session == null)
        {
            throw new UnauthorizedAccessException("The refresh token is invalid or expired.");
        }

        var rotatedRefreshToken = CreateClientToken();
        session.TokenHash = HashToken(rotatedRefreshToken);
        session.LastUsedUtc = now.UtcDateTime;
        session.ExpiresUtc = now.Add(_options.RefreshTokenLifetime).UtcDateTime;

        await _context.SaveChangesAsync(cancellationToken);
        return BuildTokenResponse(session, rotatedRefreshToken, now);
    }

    public async Task<ClaimsPrincipal?> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        AccessTokenPayload? payload;
        try
        {
            var json = _protector.Unprotect(accessToken);
            payload = JsonSerializer.Deserialize<AccessTokenPayload>(json);
        }
        catch
        {
            return null;
        }

        if (payload == null || payload.ExpiresUtc <= _timeProvider.GetUtcNow())
        {
            return null;
        }

        var session = await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Guid == payload.SessionGuid, cancellationToken);

        if (session == null || session.RevokedUtc.HasValue || session.ExpiresUtc <= _timeProvider.GetUtcNow().UtcDateTime)
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, payload.UserId),
            new(ClaimTypes.Name, payload.UserName),
            new("sub", payload.UserId),
            new("chill_auth_session", payload.SessionGuid.ToString())
        };

        var identity = new ClaimsIdentity(claims, ChillAuthIdentityDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private AuthTokenResponse BuildTokenResponse(AuthRefreshToken session, string refreshToken, DateTimeOffset issuedUtc)
    {
        var accessTokenExpiresUtc = issuedUtc.Add(_options.AccessTokenLifetime);
        var payload = new AccessTokenPayload
        {
            SessionGuid = session.Guid,
            UserId = session.IdentityUserId,
            UserName = session.UserName,
            IssuedUtc = issuedUtc,
            ExpiresUtc = accessTokenExpiresUtc
        };

        return new AuthTokenResponse
        {
            AccessToken = _protector.Protect(JsonSerializer.Serialize(payload)),
            AccessTokenIssuedUtc = issuedUtc,
            AccessTokenExpiresUtc = accessTokenExpiresUtc,
            RefreshToken = refreshToken,
            RefreshTokenExpiresUtc = new DateTimeOffset(session.ExpiresUtc, TimeSpan.Zero),
            UserId = session.IdentityUserId,
            UserName = session.UserName
        };
    }

    private async Task<AuthRefreshToken?> FindValidRefreshSessionAsync(string refreshToken, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var normalizedToken = refreshToken?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            return null;
        }

        var tokenHash = HashToken(normalizedToken);
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(x =>
                x.TokenHash == tokenHash &&
                !x.RevokedUtc.HasValue &&
                x.ExpiresUtc > now.UtcDateTime,
                cancellationToken);
    }

    private static string CreateClientToken()
    {
        return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
    }

    private static string HashToken(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash);
    }

    private sealed class AccessTokenPayload
    {
        public Guid SessionGuid { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public DateTimeOffset IssuedUtc { get; set; }

        public DateTimeOffset ExpiresUtc { get; set; }
    }
}

internal sealed class ChillAuthIdentityService<TUser> : IChillAuthIdentityService
    where TUser : class
{
    private readonly UserManager<TUser> _userManager;
    private readonly IUserStore<TUser> _userStore;
    private readonly IChillAuthService _authService;
    private readonly IChillAuthTokenService _tokenService;
    private readonly ChillAuthIdentityApiOptions _options;

    public ChillAuthIdentityService(
        UserManager<TUser> userManager,
        IUserStore<TUser> userStore,
        IChillAuthService authService,
        IChillAuthTokenService tokenService,
        IOptions<ChillAuthIdentityApiOptions> options)
    {
        _userManager = userManager;
        _userStore = userStore;
        _authService = authService;
        _tokenService = tokenService;
        _options = options.Value;
    }

    public async Task<AuthTokenResponse> RegisterAsync(RegisterAuthIdentityRequest request, CancellationToken cancellationToken = default)
    {
        var user = Activator.CreateInstance<TUser>() ?? throw new InvalidOperationException($"Cannot create an instance of {typeof(TUser).Name}.");
        var normalizedUserName = RequireValue(request.UserName, nameof(request.UserName));

        await _userStore.SetUserNameAsync(user, normalizedUserName, cancellationToken);
        if (_userStore is IUserEmailStore<TUser> emailStore && !string.IsNullOrWhiteSpace(request.Email))
        {
            await emailStore.SetEmailAsync(user, request.Email.Trim(), cancellationToken);
        }

        var createResult = await _userManager.CreateAsync(user, RequireValue(request.Password, nameof(request.Password)));
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(FormatIdentityErrors(createResult));
        }

        var identityUserId = await _userManager.GetUserIdAsync(user) ?? throw new InvalidOperationException("The created Identity user did not expose a user id.");
        var userName = await _userManager.GetUserNameAsync(user) ?? normalizedUserName;

        if ((_options.CreateChillAuthUserOnRegister || request.CreateChillAuthUser) &&
            await _authService.GetUserByExternalIdAsync(identityUserId, cancellationToken) == null)
        {
            await _authService.CreateUserAsync(new CreateAuthUserRequest
            {
                ExternalId = identityUserId,
                UserName = userName,
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? userName : request.DisplayName.Trim(),
                IsActive = true
            }, cancellationToken);
        }

        return await _tokenService.IssueAsync(identityUserId, userName, cancellationToken);
    }

    public async Task<AuthTokenResponse> LoginAsync(LoginAuthIdentityRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(request.UserNameOrEmail.Trim());
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var userId = await _userManager.GetUserIdAsync(user) ?? throw new InvalidOperationException("The authenticated Identity user did not expose a user id.");
        var userName = await _userManager.GetUserNameAsync(user) ?? request.UserNameOrEmail.Trim();
        return await _tokenService.IssueAsync(userId, userName, cancellationToken);
    }

    public Task<AuthTokenResponse> RefreshAsync(RefreshAuthTokenRequest request, CancellationToken cancellationToken = default)
    {
        return _tokenService.RefreshAsync(request.RefreshToken, cancellationToken);
    }

    public async Task<ChangePasswordResponse> ChangePasswordAsync(ClaimsPrincipal principal, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
        {
            throw new UnauthorizedAccessException("The current principal is not associated with an Identity user.");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(FormatIdentityErrors(result));
        }

        return new ChangePasswordResponse
        {
            Succeeded = true
        };
    }

    public async Task<PasswordResetTokenResponse> RequestPasswordResetAsync(RequestPasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(request.UserNameOrEmail.Trim());
        if (user == null)
        {
            return new PasswordResetTokenResponse
            {
                IsAccepted = true
            };
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        return new PasswordResetTokenResponse
        {
            IsAccepted = true,
            UserId = _options.ReturnPasswordResetTokensInResponse ? await _userManager.GetUserIdAsync(user) : null,
            ResetToken = _options.ReturnPasswordResetTokensInResponse ? token : null
        };
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(RequireValue(request.UserId, nameof(request.UserId)));
        if (user == null)
        {
            throw new InvalidOperationException("The requested user was not found.");
        }

        var result = await _userManager.ResetPasswordAsync(user, RequireValue(request.ResetToken, nameof(request.ResetToken)), RequireValue(request.NewPassword, nameof(request.NewPassword)));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(FormatIdentityErrors(result));
        }

        return new ResetPasswordResponse
        {
            Succeeded = true
        };
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

    private static string FormatIdentityErrors(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(x => $"{x.Code}: {x.Description}"));
    }

    private static string RequireValue(string? value, string argumentName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException($"{argumentName} is required.", argumentName);
        }

        return normalized;
    }
}
