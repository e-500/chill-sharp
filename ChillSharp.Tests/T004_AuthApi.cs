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
using ChillSharp.Auth.Api;
using ChillSharp.Api;
using ChillSharp.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Net.Http.Json;

namespace ChillSharp.Tests;

[TestClass]
public sealed class AuthApi
{
    /// <summary>
    /// Verifies that the auth endpoints run beside ChillApi and persist data into the same DummyContext database.
    /// </summary>
    [TestMethod]
    public async Task Step001_CreateAuthContextAndEndpoints()
    {
        // Start the shared API host with both ChillApi and ChillAuthApi mapped.
        TestApiHost.EnsureStarted();

        // Use the ChillSharp client auth methods against the sibling auth API endpoints.
        var client = new ChillSharpClient("http://localhost:5000/api/chill");

        // Create an auth user through the REST API.
        var user = client.CreateAuthUser(new CreateAuthUserRequest
        {
            ExternalId = "user-auth-test-001",
            UserName = "auth.test",
            DisplayName = "Auth Test",
            IsActive = true
        });

        Assert.IsNotNull(user);

        // Create an auth role through the REST API.
        var role = client.CreateAuthRole(new CreateAuthRoleRequest
        {
            Name = "TestRole",
            Description = "Role created by integration test",
            IsActive = true
        });

        Assert.IsNotNull(role);

        // Assign the created role to the created user.
        client.AssignAuthRole(user.Guid, role.Guid);

        // Open the same DummyContext database directly and verify that all auth records were persisted there.
        await using var verificationContext = TestApiHost.CreateDbContext();
        var persistedUser = await verificationContext.Users.FirstOrDefaultAsync(x => x.Guid == user.Guid);
        var persistedRole = await verificationContext.Roles.FirstOrDefaultAsync(x => x.Guid == role.Guid);
        var persistedMembership = await verificationContext.UserRoles.FirstOrDefaultAsync(x => x.UserGuid == user.Guid && x.RoleGuid == role.Guid);

        Assert.IsNotNull(persistedUser);
        Assert.IsNotNull(persistedRole);
        Assert.IsNotNull(persistedMembership);
    }

    /// <summary>
    /// Verifies that an anonymous caller cannot create auth users, create roles, or assign privileges when the auth API is protected by ASP.NET Core authorization.
    /// </summary>
    [TestMethod]
    public async Task Step002_AnonymousUserCannotRegisterAndGrantPrivileges()
    {
        SecuredAuthApiHost.EnsureStarted();

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5001/")
        };

        // Attempt to create an auth user without providing any authenticated identity.
        var createUserResponse = await client.PostAsJsonAsync("api/chill-auth/users", new CreateAuthUserRequest
        {
            ExternalId = "anonymous-user",
            UserName = "anonymous.user",
            DisplayName = "Anonymous User",
            IsActive = true
        });
        Assert.IsTrue(createUserResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);

        // Attempt to create a privileged role anonymously.
        var createRoleResponse = await client.PostAsJsonAsync("api/chill-auth/roles", new CreateAuthRoleRequest
        {
            Name = "Administrators",
            Description = "Anonymous escalation attempt",
            IsActive = true
        });
        Assert.IsTrue(createRoleResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);

        // Attempt to self-assign a role anonymously.
        var assignRoleResponse = await client.PutAsync($"api/chill-auth/users/{Guid.NewGuid()}/roles/{Guid.NewGuid()}", null);
        Assert.IsTrue(assignRoleResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Verifies through the ChillSharp client library that an anonymous caller cannot create users, create roles, or assign privileges on the protected auth API.
    /// </summary>
    [TestMethod]
    public void Step003_AnonymousUserCannotRegisterAndGrantPrivilegesWithClient()
    {
        SecuredAuthApiHost.EnsureStarted();

        var client = new ChillSharpClient("http://localhost:5001/api/chill");

        // Attempt to create an auth user through the client library without authentication.
        try
        {
            client.CreateAuthUser(new CreateAuthUserRequest
            {
                ExternalId = "anonymous-client-user",
                UserName = "anonymous.client.user",
                DisplayName = "Anonymous Client User",
                IsActive = true
            });
            Assert.Fail("Anonymous user creation should have failed.");
        }
        catch (ChillClientException)
        {
        }

        // Attempt to create a role through the client library without authentication.
        try
        {
            client.CreateAuthRole(new CreateAuthRoleRequest
            {
                Name = "ClientAdministrators",
                Description = "Anonymous client escalation attempt",
                IsActive = true
            });
            Assert.Fail("Anonymous role creation should have failed.");
        }
        catch (ChillClientException)
        {
        }

        // Attempt to assign a role through the client library without authentication.
        try
        {
            client.AssignAuthRole(Guid.NewGuid(), Guid.NewGuid());
            Assert.Fail("Anonymous role assignment should have failed.");
        }
        catch (ChillClientException)
        {
        }
    }

    /// <summary>
    /// Verifies that the client can register, authenticate, automatically refresh the access token, and complete password flows against the Identity-backed auth endpoints.
    /// </summary>
    [TestMethod]
    public async Task Step004_ClientCanUseIdentityAccountEndpointsAndRefreshToken()
    {
        IdentityAuthApiHost.EnsureStarted();

        var client = new ChillSharpClient("http://localhost:5002/api/chill");

        // Register a new Identity account and obtain the first access-token pair.
        var registerResponse = client.RegisterAuthAccount(new RegisterAuthIdentityRequest
        {
            UserName = "identity.user",
            Email = "identity.user@test.local",
            Password = "Pass123$",
            DisplayName = "Identity User",
            CreateChillAuthUser = true
        });

        Assert.IsFalse(string.IsNullOrWhiteSpace(registerResponse.AccessToken));
        Assert.IsFalse(string.IsNullOrWhiteSpace(registerResponse.RefreshToken));

        // Wait long enough to cross the 75% refresh threshold and force the client to refresh automatically on the next authenticated call.
        await Task.Delay(TimeSpan.FromSeconds(4));

        // Use an authenticated endpoint so the client must attach a valid bearer token and rotate it through refresh-token flow.
        var changePasswordResponse = client.ChangeAuthPassword(new ChangePasswordRequest
        {
            CurrentPassword = "Pass123$",
            NewPassword = "Pass456$"
        });

        Assert.IsTrue(changePasswordResponse.Succeeded);

        // Explicit login with the new password must succeed and return a fresh token pair.
        var loginResponse = client.LoginAuthAccount(new LoginAuthIdentityRequest
        {
            UserNameOrEmail = "identity.user",
            Password = "Pass456$"
        });

        Assert.IsFalse(string.IsNullOrWhiteSpace(loginResponse.AccessToken));

        // Request and consume a password-reset token exposed by the test host configuration.
        var resetTokenResponse = client.RequestAuthPasswordReset(new RequestPasswordResetRequest
        {
            UserNameOrEmail = "identity.user"
        });

        Assert.IsTrue(resetTokenResponse.IsAccepted);
        Assert.IsFalse(string.IsNullOrWhiteSpace(resetTokenResponse.UserId));
        Assert.IsFalse(string.IsNullOrWhiteSpace(resetTokenResponse.ResetToken));

        var resetPasswordResponse = client.ResetAuthPassword(new ResetPasswordRequest
        {
            UserId = resetTokenResponse.UserId!,
            ResetToken = resetTokenResponse.ResetToken!,
            NewPassword = "Pass789$"
        });

        Assert.IsTrue(resetPasswordResponse.Succeeded);

        // Final login with the reset password confirms that the complete account lifecycle works.
        var finalLoginResponse = client.LoginAuthAccount(new LoginAuthIdentityRequest
        {
            UserNameOrEmail = "identity.user",
            Password = "Pass789$"
        });

        Assert.IsFalse(string.IsNullOrWhiteSpace(finalLoginResponse.AccessToken));

        // Verify that the linked ChillSharp auth user and refresh-token session were both persisted in the shared database.
        await using var verificationContext = IdentityAuthApiHost.CreateDbContext();
        var persistedAuthUser = await verificationContext.Users.FirstOrDefaultAsync(x => x.ExternalId == finalLoginResponse.UserId);
        var persistedRefreshToken = await verificationContext.RefreshTokens.FirstOrDefaultAsync(x => x.IdentityUserId == finalLoginResponse.UserId);

        Assert.IsNotNull(persistedAuthUser);
        Assert.IsNotNull(persistedRefreshToken);
    }

    private static class SecuredAuthApiHost
    {
        private static readonly object SyncRoot = new();
        private static readonly string DatabasePath = Path.Combine(Path.GetTempPath(), "ChillSharpTestContext", "secured-auth-api-host.db");
        private static bool _apiServiceUpAndRunning;

        public static void EnsureStarted()
        {
            if (_apiServiceUpAndRunning)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (_apiServiceUpAndRunning)
                {
                    return;
                }

                var apiServer = Task.Run(() =>
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
                    var ctx = CreateDbContext();
                    ctx.Database.EnsureCreated();

                    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
                    builder.WebHost.UseUrls("http://localhost:5001");
                    builder.Services.AddDbContext<EF.DummyContext>(options =>
                        options.UseSqlite($"Data Source={DatabasePath}"));
                    builder.Services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestHeaderAuthenticationHandler>("Test", _ => { });
                    builder.Services.AddAuthorization();
                    builder.Services.AddChillAuthApi<EF.DummyContext>();

                    var app = builder.Build();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.MapControllers().RequireAuthorization();
                    app.Run();
                });

                apiServer.Wait(5000);
                _apiServiceUpAndRunning = true;
            }
        }

        public static EF.DummyContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={DatabasePath}")
                .Options;
            return new EF.DummyContext(options);
        }
    }

    private static class IdentityAuthApiHost
    {
        private static readonly object SyncRoot = new();
        private static readonly string DatabasePath = Path.Combine(Path.GetTempPath(), "ChillSharpTestContext", "identity-auth-api-host.db");
        private static bool _apiServiceUpAndRunning;

        public static void EnsureStarted()
        {
            if (_apiServiceUpAndRunning)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (_apiServiceUpAndRunning)
                {
                    return;
                }

                var apiServer = Task.Run(() =>
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
                    var ctx = CreateDbContext();
                    ctx.Database.EnsureDeleted();
                    ctx.Database.EnsureCreated();

                    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
                    builder.WebHost.UseUrls("http://localhost:5002");
                    builder.Services.AddDbContext<EF.DummyContext>(options =>
                        options.UseSqlite($"Data Source={DatabasePath}"));
                    builder.Services.AddIdentityCore<IdentityUser>()
                        .AddEntityFrameworkStores<EF.DummyContext>()
                        .AddSignInManager()
                        .AddDefaultTokenProviders();
                    builder.Services.AddAuthentication(ChillAuthIdentityDefaults.AuthenticationScheme)
                        .AddChillAuthBearer();
                    builder.Services.AddAuthorization();
                    builder.Services.AddChillApi<EF.DummyContext>(options => options.ProtectedApi = true);
                    builder.Services.AddChillAuthIdentityApi<EF.DummyContext, IdentityUser>(options =>
                    {
                        options.AccessTokenLifetime = TimeSpan.FromSeconds(4);
                        options.RefreshTokenLifetime = TimeSpan.FromMinutes(5);
                        options.ReturnPasswordResetTokensInResponse = true;
                    });

                    var app = builder.Build();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.MapChillApi();
                    app.Run();
                });

                apiServer.Wait(5000);
                _apiServiceUpAndRunning = true;
            }
        }

        public static EF.DummyContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={DatabasePath}")
                .Options;
            return new EF.DummyContext(options);
        }
    }

    private sealed class TestHeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestHeaderAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-User", out var userId) || string.IsNullOrWhiteSpace(userId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
