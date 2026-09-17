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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;

namespace ChillSharp.Auth.Services;

internal interface IChillAuthTokenService
{
    Task<AuthTokenResponse> IssueAsync(string userId, string userName, CancellationToken cancellationToken = default);

    Task<AuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeAsync(Guid sessionGuid, CancellationToken cancellationToken = default);

    Task<ClaimsPrincipal?> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);
}

internal sealed class ChillAuthTokenService : IChillAuthTokenService
{
    private const string AccessTokenPurpose = "ChillSharp.Auth.AccessToken.v1";

    private readonly IChillAuthDbContext _context;
    private readonly IDataProtector _protector;
    private readonly ChillAuthIdentityApiOptions _options;
    private readonly IChillAuthAccessTokenValidationCache _validationCache;
    private readonly TimeProvider _timeProvider;

    public ChillAuthTokenService(
        IChillAuthDbContext context,
        IDataProtectionProvider dataProtectionProvider,
        IChillAuthAccessTokenValidationCache validationCache,
        IOptions<ChillAuthIdentityApiOptions> options,
        TimeProvider? timeProvider = null)
    {
        _context = context;
        _protector = dataProtectionProvider.CreateProtector(AccessTokenPurpose);
        _validationCache = validationCache;
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
        _validationCache.Set(ChillAuthAccessTokenValidationSnapshot.FromEntity(session), now.UtcDateTime);

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
        _validationCache.Set(ChillAuthAccessTokenValidationSnapshot.FromEntity(session), now.UtcDateTime);
        return BuildTokenResponse(session, rotatedRefreshToken, now);
    }

    public async Task RevokeAsync(Guid sessionGuid, CancellationToken cancellationToken = default)
    {
        var session = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Guid == sessionGuid, cancellationToken);
        if (session == null)
        {
            _validationCache.Remove(sessionGuid);
            return;
        }

        if (session.RevokedUtc.HasValue)
        {
            _validationCache.Set(ChillAuthAccessTokenValidationSnapshot.FromEntity(session), _timeProvider.GetUtcNow().UtcDateTime);
            return;
        }

        session.RevokedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await _context.SaveChangesAsync(cancellationToken);
        _validationCache.Set(ChillAuthAccessTokenValidationSnapshot.FromEntity(session), _timeProvider.GetUtcNow().UtcDateTime);
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

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        ChillAuthAccessTokenValidationSnapshot? snapshot;
        if (!_validationCache.TryGet(payload.SessionGuid, nowUtc, out snapshot) || snapshot == null)
        {
            var session = await _context.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Guid == payload.SessionGuid, cancellationToken);
            if (session == null)
            {
                return null;
            }

            snapshot = ChillAuthAccessTokenValidationSnapshot.FromEntity(session);
            _validationCache.Set(snapshot, nowUtc);
        }

        if (snapshot.RevokedUtc.HasValue || snapshot.ExpiresUtc <= nowUtc)
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
    private readonly IChillAuthPasswordResetEmailSender _passwordResetEmailSender;
    private readonly ILogger<ChillAuthIdentityService<TUser>> _logger;

    public ChillAuthIdentityService(
        UserManager<TUser> userManager,
        IUserStore<TUser> userStore,
        IChillAuthService authService,
        IChillAuthTokenService tokenService,
        IOptions<ChillAuthIdentityApiOptions> options,
        IChillAuthPasswordResetEmailSender passwordResetEmailSender,
        ILogger<ChillAuthIdentityService<TUser>> logger)
    {
        _userManager = userManager;
        _userStore = userStore;
        _authService = authService;
        _tokenService = tokenService;
        _options = options.Value;
        _passwordResetEmailSender = passwordResetEmailSender;
        _logger = logger;
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
            var displayPreferences = BuildDisplayPreferences(request.DisplayCultureName);
            await _authService.CreateUserAsync(new CreateAuthUserRequest
            {
                ExternalId = identityUserId,
                UserName = userName,
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? userName : request.DisplayName.Trim(),
                DisplayCultureName = displayPreferences.DisplayCultureName,
                DisplayTimeZone = displayPreferences.DisplayTimeZone,
                DisplayDateFormat = displayPreferences.DisplayDateFormat,
                DisplayNumberFormat = displayPreferences.DisplayNumberFormat,
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

    public async Task<string> CreateManagedIdentityUserAsync(CreateAuthUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!_userManager.SupportsUserEmail || _userStore is not IUserEmailStore<TUser> emailStore)
        {
            throw new InvalidOperationException("Managed user creation requires an Identity user store with email support.");
        }

        var userName = RequireValue(request.UserName, nameof(request.UserName));
        var email = RequireValue(request.Email, nameof(request.Email));

        if (await _userManager.FindByNameAsync(userName) != null)
        {
            throw new InvalidOperationException("An Identity account with the requested user name already exists.");
        }

        if (await _userManager.FindByEmailAsync(email) != null)
        {
            throw new InvalidOperationException("An Identity account with the requested email already exists.");
        }

        var user = Activator.CreateInstance<TUser>() ?? throw new InvalidOperationException($"Cannot create an instance of {typeof(TUser).Name}.");
        await _userStore.SetUserNameAsync(user, userName, cancellationToken);
        await emailStore.SetEmailAsync(user, email, cancellationToken);

        var createResult = await _userManager.CreateAsync(user, CreateUnknownPassword());
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(FormatIdentityErrors(createResult));
        }

        await SendPasswordResetForUserAsync(user, cancellationToken);

        return await _userManager.GetUserIdAsync(user)
            ?? throw new InvalidOperationException("The created Identity user did not expose a user id.");
    }

    public Task<AuthTokenResponse> RefreshAsync(RefreshAuthTokenRequest request, CancellationToken cancellationToken = default)
    {
        return _tokenService.RefreshAsync(request.RefreshToken, cancellationToken);
    }

    public async Task LogoutAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var sessionClaim = principal.FindFirst("chill_auth_session")?.Value;
        if (!Guid.TryParse(sessionClaim, out var sessionGuid))
        {
            return;
        }

        await _tokenService.RevokeAsync(sessionGuid, cancellationToken);
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

        return await SendPasswordResetForUserAsync(user, cancellationToken);
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

    private async Task<string?> GetUserEmailAsync(TUser user)
    {
        if (!_userManager.SupportsUserEmail)
        {
            return null;
        }

        var email = await _userManager.GetEmailAsync(user);
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }

    private async Task<PasswordResetTokenResponse> SendPasswordResetForUserAsync(TUser user, CancellationToken cancellationToken)
    {
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var userId = await _userManager.GetUserIdAsync(user);

        if (_options.SendPasswordResetEmails)
        {
            var emailAddress = await GetUserEmailAsync(user);
            if (!string.IsNullOrWhiteSpace(emailAddress) && !string.IsNullOrWhiteSpace(userId))
            {
                var userName = await _userManager.GetUserNameAsync(user) ?? emailAddress;
                await _passwordResetEmailSender.SendAsync(
                    emailAddress,
                    userName,
                    userId,
                    token,
                    cancellationToken);
            }
            else
            {
                _logger.LogWarning("Password-reset email delivery was requested but the target account does not expose a usable email address.");
            }
        }

        return new PasswordResetTokenResponse
        {
            IsAccepted = true,
            UserId = _options.ReturnPasswordResetTokensInResponse ? userId : null,
            ResetToken = _options.ReturnPasswordResetTokensInResponse ? token : null
        };
    }

    private static DisplayPreferences BuildDisplayPreferences(string? displayCultureName)
    {
        var normalizedCultureName = displayCultureName?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCultureName))
        {
            return new DisplayPreferences(string.Empty, string.Empty, string.Empty, string.Empty);
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(normalizedCultureName);
            return new DisplayPreferences(
                culture.Name,
                ResolveTimeZone(culture),
                ResolveDateFormat(culture),
                ResolveNumberFormat(culture));
        }
        catch (CultureNotFoundException)
        {
            return new DisplayPreferences(normalizedCultureName, string.Empty, string.Empty, string.Empty);
        }
    }

    private static string ResolveTimeZone(CultureInfo culture)
    {
        var regionCode = TryGetRegionCode(culture);
        var preferredId = regionCode switch
        {
            "IT" or "FR" or "DE" or "ES" or "NL" or "BE" or "AT" => "W. Europe Standard Time",
            "GB" or "IE" => "GMT Standard Time",
            "US" => "Eastern Standard Time",
            "CA" => "Eastern Standard Time",
            "AU" => "AUS Eastern Standard Time",
            "NZ" => "New Zealand Standard Time",
            "JP" => "Tokyo Standard Time",
            "CN" => "China Standard Time",
            "IN" => "India Standard Time",
            "BR" => "E. South America Standard Time",
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(preferredId))
        {
            var match = TimeZoneInfo.GetSystemTimeZones()
                .FirstOrDefault(x => string.Equals(x.Id, preferredId, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return match.Id;
            }
        }

        return TimeZoneInfo.Local.Id;
    }

    private static string ResolveDateFormat(CultureInfo culture)
    {
        var pattern = culture.DateTimeFormat.ShortDatePattern;
        var builder = new StringBuilder(pattern.Length * 2);

        for (var index = 0; index < pattern.Length;)
        {
            var current = pattern[index];
            if (char.IsLetter(current))
            {
                var start = index;
                while (index < pattern.Length && pattern[index] == current)
                {
                    index++;
                }

                builder.Append(char.ToUpperInvariant(current) switch
                {
                    'D' => "DD",
                    'M' => "MM",
                    'Y' => "YYYY",
                    _ => pattern[start..index].ToUpperInvariant()
                });

                continue;
            }

            builder.Append(current);
            index++;
        }

        return builder.ToString();
    }

    private static string ResolveNumberFormat(CultureInfo culture)
    {
        var groupSeparator = culture.NumberFormat.NumberGroupSeparator;
        var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
        return $"1{groupSeparator}000{decimalSeparator}00";
    }

    private static string? TryGetRegionCode(CultureInfo culture)
    {
        try
        {
            return new RegionInfo(culture.Name).TwoLetterISORegionName;
        }
        catch
        {
            return null;
        }
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

    private static string CreateUnknownPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@$?_+-=";
        const string all = upper + lower + digits + symbols;

        Span<char> buffer = stackalloc char[24];
        buffer[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        buffer[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        buffer[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        buffer[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];

        for (var index = 4; index < buffer.Length; index++)
        {
            buffer[index] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        for (var index = buffer.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (buffer[index], buffer[swapIndex]) = (buffer[swapIndex], buffer[index]);
        }

        return new string(buffer);
    }

    private sealed record DisplayPreferences(
        string DisplayCultureName,
        string DisplayTimeZone,
        string DisplayDateFormat,
        string DisplayNumberFormat);
}

internal interface IChillAuthPasswordResetEmailSender
{
    Task SendAsync(string recipientEmail, string recipientName, string userId, string resetToken, CancellationToken cancellationToken = default);
}

internal sealed class ChillAuthPasswordResetEmailSender : IChillAuthPasswordResetEmailSender
{
    private readonly ChillAuthIdentityApiOptions _options;
    private readonly ILogger<ChillAuthPasswordResetEmailSender> _logger;

    public ChillAuthPasswordResetEmailSender(
        IOptions<ChillAuthIdentityApiOptions> options,
        ILogger<ChillAuthPasswordResetEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string recipientEmail, string recipientName, string userId, string resetToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_options.SendPasswordResetEmails)
        {
            return;
        }

        var smtpHost = RequireValue(_options.SmtpHost, nameof(_options.SmtpHost));
        var fromEmail = RequireValue(_options.PasswordResetFromEmail, nameof(_options.PasswordResetFromEmail));

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, _options.PasswordResetFromDisplayName),
            Subject = _options.PasswordResetEmailSubject,
            Body = BuildMessageBody(recipientName, userId, resetToken),
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(recipientEmail, recipientName));

        using var smtpClient = new SmtpClient(smtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.SmtpEnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.SmtpUserName))
        {
            smtpClient.Credentials = new NetworkCredential(
                _options.SmtpUserName,
                _options.SmtpPassword ?? string.Empty);
        }

        await smtpClient.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Sent ChillSharp password-reset email to '{RecipientEmail}'.", recipientEmail);
    }

    private string BuildMessageBody(string recipientName, string userId, string resetToken)
    {
        var lines = new List<string>
        {
            $"Hello {recipientName},",
            string.Empty,
            "A password reset was requested for your account."
        };

        var resetLink = BuildResetLink(userId, resetToken);
        if (!string.IsNullOrWhiteSpace(resetLink))
        {
            lines.Add(string.Empty);
            lines.Add("Open this link to continue:");
            lines.Add(resetLink);
        }

        lines.Add(string.Empty);
        lines.Add("If your client needs the raw values, use:");
        lines.Add($"UserId: {userId}");
        lines.Add($"ResetToken: {resetToken}");
        lines.Add(string.Empty);
        lines.Add("If you did not request this change, you can ignore this message.");

        return string.Join(Environment.NewLine, lines);
    }

    private string? BuildResetLink(string userId, string resetToken)
    {
        if (string.IsNullOrWhiteSpace(_options.PasswordResetUrlBase))
        {
            return null;
        }

        var baseUri = _options.PasswordResetUrlBase.Trim();
        var separator = baseUri.Contains('?') ? '&' : '?';
        return $"{baseUri}{separator}userId={Uri.EscapeDataString(userId)}&resetToken={Uri.EscapeDataString(resetToken)}";
    }

    private static string RequireValue(string? value, string optionName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException($"{optionName} must be configured when password-reset email delivery is enabled.");
        }

        return normalized;
    }
}
