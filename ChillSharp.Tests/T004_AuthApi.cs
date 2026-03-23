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
using ChillSharp.Auth.Model;
using ChillSharp.Api;
using ChillSharp.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Extensions;
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

        try
        {
            client.GetAuthUsers();
            Assert.Fail("A normal authenticated user should not be allowed to manage the auth API.");
        }
        catch (ChillClientException)
        {
        }

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

    /// <summary>
    /// Verifies that the Identity integration can bootstrap a root account from environment variables during startup.
    /// </summary>
    [TestMethod]
    public async Task Step005_RootAccountCanBeInitializedFromEnvironment()
    {
        const string rootUserName = "root";
        const string rootPassword = "Pass123$";
        const string rootEmail = "root@test.local";
        const string rootDisplayName = "Root User";

        Environment.SetEnvironmentVariable("CHILLSHARP_AUTH_ROOT_USERNAME", rootUserName);
        Environment.SetEnvironmentVariable("CHILLSHARP_AUTH_ROOT_PASSWORD", rootPassword);
        Environment.SetEnvironmentVariable("CHILLSHARP_AUTH_ROOT_EMAIL", rootEmail);
        Environment.SetEnvironmentVariable("CHILLSHARP_AUTH_ROOT_DISPLAY_NAME", rootDisplayName);

        try
        {
            RootBootstrapAuthApiHost.EnsureStarted();

            var client = new ChillSharpClient("http://localhost:5003/api/chill");
            var loginResponse = client.LoginAuthAccount(new LoginAuthIdentityRequest
            {
                UserNameOrEmail = rootUserName,
                Password = rootPassword
            });

            Assert.AreEqual(rootUserName, loginResponse.UserName);
            Assert.IsFalse(string.IsNullOrWhiteSpace(loginResponse.AccessToken));
            Assert.AreEqual(1, client.GetAuthUsers().Count);

            await using var verificationContext = RootBootstrapAuthApiHost.CreateDbContext();
            var persistedIdentityUser = await verificationContext.Set<IdentityUser>().FirstOrDefaultAsync(x => x.UserName == rootUserName);

            Assert.IsNotNull(persistedIdentityUser);

            var persistedAuthUser = await verificationContext.Users.FirstOrDefaultAsync(x => x.ExternalId == persistedIdentityUser.Id);
            Assert.IsNotNull(persistedAuthUser);
            Assert.AreEqual(rootDisplayName, persistedAuthUser.DisplayName);
            Assert.IsTrue(persistedAuthUser.CanManagePermissions);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CHILLSHARP_AUTH_ROOT_USERNAME", null);
            Environment.SetEnvironmentVariable("CHILLSHARP_AUTH_ROOT_PASSWORD", null);
            Environment.SetEnvironmentVariable("CHILLSHARP_AUTH_ROOT_EMAIL", null);
            Environment.SetEnvironmentVariable("CHILLSHARP_AUTH_ROOT_DISPLAY_NAME", null);
        }
    }

    /// <summary>
    /// Verifies the refactored get/set management endpoints and that updates do not duplicate memberships or permission rules.
    /// </summary>
    [TestMethod]
    public async Task Step006_RefactoredManagementEndpointsReturnStructuredPayloads()
    {
        TestApiHost.EnsureStarted();

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };

        var role = await (await client.PostAsJsonAsync("api/chill-auth/set-role", new SetAuthRoleRequest
        {
            Name = $"ManagedRole-{Guid.NewGuid():N}",
            Description = "Role created by set-role",
            IsActive = true,
            Permissions =
            [
                new AuthPermissionRuleItem
                {
                    Effect = PermissionEffect.Allow,
                    Action = PermissionAction.Query,
                    Scope = PermissionScope.Entity,
                    Module = "Blog",
                    EntityName = "Post",
                    Description = "Allow blog post query"
                }
            ]
        })).EnsureSuccess().Content.ReadAsAsync<AuthRoleDetailsResponse>();

        var user = await (await client.PostAsJsonAsync("api/chill-auth/set-user", new SetAuthUserRequest
        {
            ExternalId = $"managed-user-{Guid.NewGuid():N}",
            UserName = $"managed.user.{Guid.NewGuid():N}",
            DisplayName = "Managed User",
            IsActive = true,
            RoleGuids = [role.Guid],
            Permissions =
            [
                new AuthPermissionRuleItem
                {
                    Effect = PermissionEffect.Allow,
                    Action = PermissionAction.Modify,
                    Scope = PermissionScope.Property,
                    Module = "Blog",
                    EntityName = "Post",
                    PropertyName = "Title",
                    Description = "Allow title edits"
                }
            ]
        })).EnsureSuccess().Content.ReadAsAsync<AuthUserDetailsResponse>();

        var fetchedUser = await (await client.GetAsync($"api/chill-auth/get-user?userGuid={user.Guid}")).EnsureSuccess().Content.ReadAsAsync<AuthUserDetailsResponse>();
        var fetchedRole = await (await client.GetAsync($"api/chill-auth/get-role?roleGuid={role.Guid}")).EnsureSuccess().Content.ReadAsAsync<AuthRoleDetailsResponse>();
        var userList = await (await client.GetAsync("api/chill-auth/get-user-list")).EnsureSuccess().Content.ReadAsAsync<List<AuthUserListItemResponse>>();
        var roleList = await (await client.GetAsync("api/chill-auth/get-role-list")).EnsureSuccess().Content.ReadAsAsync<List<AuthRoleListItemResponse>>();

        Assert.IsNotNull(fetchedUser);
        Assert.AreEqual(1, fetchedUser.Roles.Count);
        Assert.AreEqual(role.Guid, fetchedUser.Roles[0].Guid);
        Assert.AreEqual(1, fetchedUser.Permissions.Count);
        Assert.IsNotNull(fetchedRole);
        Assert.AreEqual(1, fetchedRole.Users.Count);
        Assert.AreEqual(user.Guid, fetchedRole.Users[0].Guid);
        Assert.AreEqual(1, fetchedRole.Permissions.Count);
        Assert.IsTrue(userList!.Any(x => x.Guid == user.Guid));
        Assert.IsTrue(roleList!.Any(x => x.Guid == role.Guid));

        var existingRolePermissionGuid = fetchedRole.Permissions[0].Guid;
        var updatedRole = await (await client.PostAsJsonAsync("api/chill-auth/set-role", new SetAuthRoleRequest
        {
            Guid = role.Guid,
            Name = role.Name,
            Description = "Role updated by set-role",
            IsActive = true,
            UserGuids = [user.Guid],
            Permissions =
            [
                new AuthPermissionRuleItem
                {
                    Guid = existingRolePermissionGuid,
                    Effect = PermissionEffect.Allow,
                    Action = PermissionAction.Query,
                    Scope = PermissionScope.Entity,
                    Module = "Blog",
                    EntityName = "Post",
                    Description = "Allow blog post query"
                },
                new AuthPermissionRuleItem
                {
                    Effect = PermissionEffect.Allow,
                    Action = PermissionAction.Update,
                    Scope = PermissionScope.Entity,
                    Module = "Blog",
                    EntityName = "Post",
                    Description = "Allow blog post update"
                }
            ]
        })).EnsureSuccess().Content.ReadAsAsync<AuthRoleDetailsResponse>();

        Assert.AreEqual(2, updatedRole.Permissions.Count);
        Assert.IsTrue(updatedRole.Permissions.Any(x => x.Guid == existingRolePermissionGuid));

        await using var verificationContext = TestApiHost.CreateDbContext();
        var persistedMemberships = await verificationContext.UserRoles
            .Where(x => x.UserGuid == user.Guid && x.RoleGuid == role.Guid)
            .CountAsync();
        var persistedRoleRules = await verificationContext.PermissionRules
            .Where(x => x.RoleGuid == role.Guid)
            .CountAsync();

        Assert.AreEqual(1, persistedMemberships);
        Assert.AreEqual(2, persistedRoleRules);
    }

    /// <summary>
    /// Verifies that get-permissions is available to a normal authenticated user while management endpoints remain forbidden.
    /// </summary>
    [TestMethod]
    public async Task Step007_GetPermissionsIsAvailableButManagementRequiresPrivilege()
    {
        SecuredAuthApiHost.EnsureStarted();

        var externalId = $"viewer-{Guid.NewGuid():N}";
        var roleGuid = Guid.NewGuid();
        var userGuid = Guid.NewGuid();

        await using (var seedContext = SecuredAuthApiHost.CreateDbContext())
        {
            seedContext.Users.Add(new ChillSharp.Auth.Model.AuthUser
            {
                Guid = userGuid,
                ExternalId = externalId,
                UserName = $"viewer.{Guid.NewGuid():N}",
                DisplayName = "Viewer User",
                IsActive = true,
                CanManagePermissions = false
            });
            seedContext.Roles.Add(new ChillSharp.Auth.Model.AuthRole
            {
                Guid = roleGuid,
                Name = $"ViewerRole-{Guid.NewGuid():N}",
                Description = "Role for get-permissions test",
                IsActive = true
            });
            seedContext.UserRoles.Add(new ChillSharp.Auth.Model.AuthUserRole
            {
                UserGuid = userGuid,
                RoleGuid = roleGuid,
                AssignedUtc = DateTime.UtcNow
            });
            seedContext.PermissionRules.Add(new ChillSharp.Auth.Model.AuthPermissionRule
            {
                Guid = Guid.NewGuid(),
                UserGuid = userGuid,
                Effect = PermissionEffect.Allow,
                Action = PermissionAction.Update,
                Scope = PermissionScope.Entity,
                Module = "Blog",
                EntityName = "Post",
                CreatedUtc = DateTime.UtcNow
            });
            seedContext.PermissionRules.Add(new ChillSharp.Auth.Model.AuthPermissionRule
            {
                Guid = Guid.NewGuid(),
                RoleGuid = roleGuid,
                Effect = PermissionEffect.Allow,
                Action = PermissionAction.Query,
                Scope = PermissionScope.Entity,
                Module = "Blog",
                EntityName = "Post",
                CreatedUtc = DateTime.UtcNow
            });
            await seedContext.SaveChangesAsync();
        }

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5001/")
        };
        client.DefaultRequestHeaders.Add("X-Test-User", externalId);

        var getPermissions = await client.GetAsync("api/chill-auth/get-permissions");
        Assert.AreEqual(HttpStatusCode.OK, getPermissions.StatusCode);
        var permissions = await getPermissions.Content.ReadFromJsonAsync<GetAuthPermissionsResponse>();
        Assert.IsNotNull(permissions);
        Assert.IsNotNull(permissions.User);
        Assert.AreEqual(userGuid, permissions.User.Guid);
        Assert.AreEqual(1, permissions.Permissions.Count);
        Assert.AreEqual(1, permissions.Roles.Count);
        Assert.AreEqual(1, permissions.Roles[0].Permissions.Count);

        var getUserList = await client.GetAsync("api/chill-auth/get-user-list");
        Assert.AreEqual(HttpStatusCode.Forbidden, getUserList.StatusCode);
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
                    ctx.Database.EnsureDeleted();
                    ctx.Database.EnsureCreated();

                    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
                    builder.WebHost.UseUrls("http://localhost:5001");
                    builder.Logging.ClearProviders();
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
                    builder.Logging.ClearProviders();
                    builder.Services.AddDbContext<EF.DummyContext>(options =>
                        options.UseSqlite($"Data Source={DatabasePath}"));
                    builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
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

    private static class RootBootstrapAuthApiHost
    {
        private static readonly object SyncRoot = new();
        private static readonly string DatabasePath = Path.Combine(Path.GetTempPath(), "ChillSharpTestContext", "root-bootstrap-auth-api-host.db");
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
                    builder.WebHost.UseUrls("http://localhost:5003");
                    builder.Logging.ClearProviders();
                    builder.Services.AddDbContext<EF.DummyContext>(options =>
                        options.UseSqlite($"Data Source={DatabasePath}"));
                    builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                    builder.Services.AddIdentityCore<IdentityUser>()
                        .AddEntityFrameworkStores<EF.DummyContext>()
                        .AddSignInManager()
                        .AddDefaultTokenProviders();
                    builder.Services.AddAuthentication(ChillAuthIdentityDefaults.AuthenticationScheme)
                        .AddChillAuthBearer();
                    builder.Services.AddAuthorization();
                    builder.Services.AddChillApi<EF.DummyContext>(options => options.ProtectedApi = true);
                    builder.Services.AddChillAuthIdentityApi<EF.DummyContext, IdentityUser>();

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

internal static class HttpResponseMessageTestExtensions
{
    public static HttpResponseMessage EnsureSuccess(this HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return response;
    }

    public static async Task<T> ReadAsAsync<T>(this HttpContent content)
    {
        var result = await content.ReadFromJsonAsync<T>();
        Assert.IsNotNull(result);
        return result!;
    }
}
