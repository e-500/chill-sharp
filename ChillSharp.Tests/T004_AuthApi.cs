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
using ChillSharp.Auth.Services;
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
        TestApiHost.EnsureStarted(6002);

        // Use the ChillSharp client auth methods against the sibling auth API endpoints.
        var client = new ChillSharpClient("http://localhost:6002/api/chill");

        // Create an auth user through the REST API.
        var user = client.CreateAuthUser(new CreateAuthUserRequest
        {
            ExternalId = "user-auth-test-001",
            UserName = "auth.test",
            Email = "dummy@example.com",
            DisplayName = "Auth Test",
            DisplayCultureName = "it-IT",
            DisplayTimeZone = "W. Europe Standard Time",
            DisplayDateFormat = "DD/MM/YYYY",
            DisplayNumberFormat = "1.000,00",
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
        Assert.AreEqual("it-IT", persistedUser.DisplayCultureName);
        Assert.AreEqual("W. Europe Standard Time", persistedUser.DisplayTimeZone);
        Assert.AreEqual("DD/MM/YYYY", persistedUser.DisplayDateFormat);
        Assert.AreEqual("1.000,00", persistedUser.DisplayNumberFormat);
    }

    /// <summary>
    /// Verifies that an anonymous caller cannot create auth users, create roles, or assign privileges when the auth API is protected by ASP.NET Core authorization.
    /// </summary>
    [TestMethod]
    public void Step002_AnonymousUserCannotRegisterAndGrantPrivileges()
    {
        SecuredAuthApiHost.EnsureStarted();

        var client = new ChillSharpClient("http://localhost:5001/api/chill");

        // Attempt to create an auth user without providing any authenticated identity.
        try
        {
            client.CreateAuthUser(new CreateAuthUserRequest
            {
                ExternalId = "anonymous-user",
                UserName = "anonymous.user",
                Email = "dummy@example.com",
                DisplayName = "Anonymous User",
                IsActive = true
            });
            Assert.Fail("Anonymous user creation should have failed.");
        }
        catch (ChillClientException ex)
        {
            Assert.IsTrue(ex.Message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
                          ex.Message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase));
        }

        // Attempt to create a privileged role anonymously.
        try
        {
            client.CreateAuthRole(new CreateAuthRoleRequest
            {
                Name = "Administrators",
                Description = "Anonymous escalation attempt",
                IsActive = true
            });
            Assert.Fail("Anonymous role creation should have failed.");
        }
        catch (ChillClientException ex)
        {
            Assert.IsTrue(ex.Message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
                          ex.Message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase));
        }

        // Attempt to self-assign a role anonymously.
        try
        {
            client.AssignAuthRole(Guid.NewGuid(), Guid.NewGuid());
            Assert.Fail("Anonymous role assignment should have failed.");
        }
        catch (ChillClientException ex)
        {
            Assert.IsTrue(ex.Message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
                          ex.Message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase));
        }
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
    /// Verifies that the refactored account endpoints can register, authenticate, refresh, logout, and complete password flows.
    /// </summary>
    [TestMethod]
    public async Task Step004_IdentityAccountEndpointsSupportLifecycleFlows()
    {
        IdentityAuthApiHost.EnsureStarted();

        var anonymousClient = new ChillSharpClient("http://localhost:5002/api/chill");
        var registerResponse = anonymousClient.RegisterAuthAccount(new RegisterAuthIdentityRequest
        {
            UserName = "identity.user",
            Email = "identity.user@test.local",
            Password = "Pass123$",
            DisplayName = "Identity User",
            DisplayCultureName = "it-IT",
            CreateChillAuthUser = true
        });

        Assert.IsFalse(string.IsNullOrWhiteSpace(registerResponse.AccessToken));
        Assert.IsFalse(string.IsNullOrWhiteSpace(registerResponse.RefreshToken));

        var authenticatedClient = new ChillSharpClient("http://localhost:5002/api/chill", AuthToken: registerResponse.AccessToken);
        try
        {
            authenticatedClient.GetAuthUsers();
            Assert.Fail("A non-manager should not be allowed to read the auth user list.");
        }
        catch (ChillClientException ex)
        {
            Assert.IsTrue(ex.Message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase));
        }

        var refreshResponse = anonymousClient.RefreshAuthAccount();
        Assert.IsFalse(string.IsNullOrWhiteSpace(refreshResponse.AccessToken));
        Assert.AreNotEqual(registerResponse.RefreshToken, refreshResponse.RefreshToken);

        var refreshedClient = new ChillSharpClient("http://localhost:5002/api/chill", AuthToken: refreshResponse.AccessToken);
        var changePasswordResponse = refreshedClient.ChangeAuthPassword(new ChangePasswordRequest
        {
            CurrentPassword = "Pass123$",
            NewPassword = "Pass456$"
        });

        Assert.IsTrue(changePasswordResponse.Succeeded);

        var loginResponse = anonymousClient.LoginAuthAccount(new LoginAuthIdentityRequest
        {
            UserNameOrEmail = "identity.user",
            Password = "Pass456$"
        });

        Assert.IsFalse(string.IsNullOrWhiteSpace(loginResponse.AccessToken));

        var resetTokenResponse = anonymousClient.RequestAuthPasswordReset(new RequestPasswordResetRequest
        {
            UserNameOrEmail = "identity.user"
        });

        Assert.IsTrue(resetTokenResponse.IsAccepted);
        Assert.IsFalse(string.IsNullOrWhiteSpace(resetTokenResponse.UserId));
        Assert.IsFalse(string.IsNullOrWhiteSpace(resetTokenResponse.ResetToken));

        var resetPasswordResponse = anonymousClient.ResetAuthPassword(new ResetPasswordRequest
        {
            UserId = resetTokenResponse.UserId!,
            ResetToken = resetTokenResponse.ResetToken!,
            NewPassword = "Pass789$"
        });

        Assert.IsTrue(resetPasswordResponse.Succeeded);

        var finalClient = new ChillSharpClient("http://localhost:5002/api/chill");
        var finalLoginResponse = finalClient.LoginAuthAccount(new LoginAuthIdentityRequest
        {
            UserNameOrEmail = "identity.user",
            Password = "Pass789$"
        });

        Assert.IsFalse(string.IsNullOrWhiteSpace(finalLoginResponse.AccessToken));

        finalClient.LogoutAuthAccount();

        await using var verificationContext = IdentityAuthApiHost.CreateDbContext();
        var persistedAuthUser = await verificationContext.Users.FirstOrDefaultAsync(x => x.ExternalId == finalLoginResponse.UserId);
        var revokedRefreshToken = await verificationContext.RefreshTokens
            .Where(x => x.IdentityUserId == finalLoginResponse.UserId)
            .OrderByDescending(x => x.CreatedUtc)
            .FirstOrDefaultAsync();

        Assert.IsNotNull(persistedAuthUser);
        Assert.IsNotNull(revokedRefreshToken);
        Assert.IsTrue(revokedRefreshToken.RevokedUtc.HasValue);
        Assert.AreEqual("it-IT", persistedAuthUser.DisplayCultureName);
        Assert.AreEqual("W. Europe Standard Time", persistedAuthUser.DisplayTimeZone);
        Assert.AreEqual("DD/MM/YYYY", persistedAuthUser.DisplayDateFormat);
        Assert.AreEqual("1.000,00", persistedAuthUser.DisplayNumberFormat);
    }

    /// <summary>
    /// Verifies that the Identity integration can bootstrap a root account and create managed users that reset their password through the refactored endpoints.
    /// </summary>
    [TestMethod]
    public async Task Step005_RootAccountCanBootstrapAndProvisionManagedUsers()
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

            var anonymousClient = new ChillSharpClient("http://localhost:5003/api/chill");
            var rootClient = new ChillSharpClient("http://localhost:5003/api/chill");
            var loginResponse = rootClient.LoginAuthAccount(new LoginAuthIdentityRequest
            {
                UserNameOrEmail = rootUserName,
                Password = rootPassword
            });

            Assert.AreEqual(rootUserName, loginResponse.UserName);
            Assert.IsFalse(string.IsNullOrWhiteSpace(loginResponse.AccessToken));

            var usersResponse = rootClient.GetAuthUsers();
            Assert.AreEqual(1, usersResponse.Count);

            var managedUserResponse = rootClient.CreateAuthUser(new CreateAuthUserRequest
            {
                UserName = "invited.user",
                Email = "invited.user@test.local",
                DisplayName = "Invited User",
                DisplayCultureName = "en-US",
                DisplayTimeZone = "Eastern Standard Time",
                DisplayDateFormat = "MM/DD/YYYY",
                DisplayNumberFormat = "1,000.00",
                IsActive = true
            });

            Assert.IsFalse(string.IsNullOrWhiteSpace(managedUserResponse.ExternalId));

            await using var verificationContext = RootBootstrapAuthApiHost.CreateDbContext();
            var persistedIdentityUser = await verificationContext.Set<IdentityUser>().FirstOrDefaultAsync(x => x.UserName == rootUserName);

            Assert.IsNotNull(persistedIdentityUser);

            var persistedAuthUser = await verificationContext.Users.FirstOrDefaultAsync(x => x.ExternalId == persistedIdentityUser.Id);
            Assert.IsNotNull(persistedAuthUser);
            Assert.AreEqual(rootDisplayName, persistedAuthUser.DisplayName);
            Assert.IsTrue(persistedAuthUser.CanManagePermissions);
            Assert.IsTrue(persistedAuthUser.CanManageSchema);

            var invitedIdentityUser = await verificationContext.Set<IdentityUser>().FirstOrDefaultAsync(x => x.Id == managedUserResponse.ExternalId);
            Assert.IsNotNull(invitedIdentityUser);
            Assert.AreEqual("invited.user", invitedIdentityUser.UserName);
            Assert.AreEqual("invited.user@test.local", invitedIdentityUser.Email);

            var invitedResetResponse = anonymousClient.RequestAuthPasswordReset(new RequestPasswordResetRequest
            {
                UserNameOrEmail = "invited.user@test.local"
            });

            Assert.IsTrue(invitedResetResponse.IsAccepted);
            Assert.AreEqual(managedUserResponse.ExternalId, invitedResetResponse.UserId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(invitedResetResponse.ResetToken));
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
    /// Verifies the merged management controller keeps CRUD routes stable and that updates do not duplicate memberships or permission rules.
    /// </summary>
    [TestMethod]
    public async Task Step006_RefactoredManagementEndpointsReturnStructuredPayloads()
    {
        MergedManagementApiHost.EnsureStarted();

        var client = new ChillSharpClient("http://localhost:6012/api/chill");

        var role = client.CreateAuthRole(new CreateAuthRoleRequest
        {
            Name = $"ManagedRole-{Guid.NewGuid():N}",
            Description = "Role created by CRUD route",
            IsActive = true,
            MenuHierarchy = "BLOG.ADMIN"
        });

        var user = client.CreateAuthUser(new CreateAuthUserRequest
        {
            ExternalId = $"managed-user-{Guid.NewGuid():N}",
            UserName = $"managed.user.{Guid.NewGuid():N}",
            Email = "custom@example.com",
            DisplayName = "Managed User",
            DisplayCultureName = "en-US",
            DisplayTimeZone = "Eastern Standard Time",
            DisplayDateFormat = "MM/DD/YYYY",
            DisplayNumberFormat = "1,000.00",
            IsActive = true,
            CanManageSchema = true,
            MenuHierarchy = "BLOG.USER"
        });

        client.AssignAuthRole(user.Guid, role.Guid);
        var userRule = client.CreateAuthPermissionRule(new CreateAuthPermissionRuleRequest
        {
            UserGuid = user.Guid,
            Effect = PermissionEffect.Allow,
            Action = PermissionAction.Modify,
            Scope = PermissionScope.Property,
            Module = "Blog",
            EntityName = "Post",
            PropertyName = "Title",
            Description = "Allow title edits"
        });
        var roleRule = client.CreateAuthPermissionRule(new CreateAuthPermissionRuleRequest
        {
            RoleGuid = role.Guid,
            Effect = PermissionEffect.Allow,
            Action = PermissionAction.Query,
            Scope = PermissionScope.Entity,
            Module = "Blog",
            EntityName = "Post",
            Description = "Allow blog post query"
        });

        var fetchedUser = client.GetAuthUser(user.Guid);
        var fetchedRole = client.GetAuthRole(role.Guid);
        var fetchedUserRoles = client.GetAuthUserRoles(user.Guid);
        var fetchedUserRules = client.GetAuthPermissionRules(userGuid: user.Guid);
        var fetchedRoleRules = client.GetAuthPermissionRules(roleGuid: role.Guid);
        var userList = client.GetAuthUserList();
        var roleList = client.GetAuthRoleList();

        Assert.IsNotNull(fetchedUser);
        Assert.IsTrue(fetchedUser.CanManageSchema);
        Assert.AreEqual("en-US", fetchedUser.DisplayCultureName);
        Assert.AreEqual("Eastern Standard Time", fetchedUser.DisplayTimeZone);
        Assert.AreEqual("MM/DD/YYYY", fetchedUser.DisplayDateFormat);
        Assert.AreEqual("1,000.00", fetchedUser.DisplayNumberFormat);
        Assert.AreEqual("BLOG.USER", fetchedUser.MenuHierarchy);
        Assert.HasCount(1, fetchedUserRoles);
        Assert.AreEqual(role.Guid, fetchedUserRoles[0].Guid);
        Assert.AreEqual("BLOG.ADMIN", fetchedUserRoles[0].MenuHierarchy);
        Assert.HasCount(1, fetchedUserRules);
        Assert.AreEqual(userRule.Guid, fetchedUserRules[0].Guid);
        Assert.IsNotNull(fetchedRole);
        Assert.AreEqual("BLOG.ADMIN", fetchedRole.MenuHierarchy);
        Assert.HasCount(1, fetchedRoleRules);
        Assert.AreEqual(roleRule.Guid, fetchedRoleRules[0].Guid);
        Assert.IsTrue(userList!.Any(x => x.Guid == user.Guid));
        Assert.IsTrue(userList!.Single(x => x.Guid == user.Guid).CanManageSchema);
        Assert.AreEqual("en-US", userList!.Single(x => x.Guid == user.Guid).DisplayCultureName);
        Assert.AreEqual("Eastern Standard Time", userList!.Single(x => x.Guid == user.Guid).DisplayTimeZone);
        Assert.AreEqual("MM/DD/YYYY", userList!.Single(x => x.Guid == user.Guid).DisplayDateFormat);
        Assert.AreEqual("1,000.00", userList!.Single(x => x.Guid == user.Guid).DisplayNumberFormat);
        Assert.AreEqual("BLOG.USER", userList!.Single(x => x.Guid == user.Guid).MenuHierarchy);
        Assert.IsTrue(roleList!.Any(x => x.Guid == role.Guid));
        Assert.AreEqual("BLOG.ADMIN", roleList!.Single(x => x.Guid == role.Guid).MenuHierarchy);

        var updatedRole = client.UpdateAuthRole(role.Guid, new UpdateAuthRoleRequest
        {
            Name = role.Name,
            Description = "Role updated by CRUD route",
            IsActive = true,
            MenuHierarchy = "BLOG.EDITOR"
        });
        Assert.IsNotNull(updatedRole);

        var updatedRoleRule = client.UpdateAuthPermissionRule(roleRule.Guid, new UpdateAuthPermissionRuleRequest
        {
            RoleGuid = role.Guid,
            Effect = PermissionEffect.Allow,
            Action = PermissionAction.Update,
            Scope = PermissionScope.Entity,
            Module = "Blog",
            EntityName = "Post",
            Description = "Allow blog post update"
        });
        Assert.IsNotNull(updatedRoleRule);

        Assert.AreEqual("BLOG.EDITOR", updatedRole.MenuHierarchy);

        await using var verificationContext = MergedManagementApiHost.CreateDbContext();
        var persistedMemberships = await verificationContext.UserRoles
            .Where(x => x.UserGuid == user.Guid && x.RoleGuid == role.Guid)
            .CountAsync();
        var persistedRoleRules = await verificationContext.PermissionRules
            .Where(x => x.RoleGuid == role.Guid)
            .CountAsync();

        Assert.AreEqual(1, persistedMemberships);
        Assert.AreEqual(1, persistedRoleRules);
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

        var client = CreateTestHeaderClient("http://localhost:5001/api/chill", externalId);

        var permissions = client.GetAuthPermissions();
        Assert.IsNotNull(permissions.User);
        Assert.AreEqual(userGuid, permissions.User.Guid);
        Assert.HasCount(1, permissions.Permissions);
        Assert.HasCount(1, permissions.Roles);
        Assert.HasCount(1, permissions.Roles[0].Permissions);

        try
        {
            client.GetAuthUserList();
            Assert.Fail("A non-manager should not be allowed to access auth management endpoints.");
        }
        catch (ChillClientException ex)
        {
            Assert.IsTrue(ex.Message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Verifies that FullControl is used as a fallback and loses precedence to an exact action rule.
    /// </summary>
    [TestMethod]
    public async Task Step008_FullControlActsAsFallbackBehindExactActions()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "ChillSharpTestContext", $"full-control-{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        var options = new DbContextOptionsBuilder<EF.DummyContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using var context = new EF.DummyContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var userGuid = Guid.NewGuid();
        context.Users.Add(new ChillSharp.Auth.Model.AuthUser
        {
            Guid = userGuid,
            ExternalId = $"full-control-{Guid.NewGuid():N}",
            UserName = $"full.control.{Guid.NewGuid():N}",
            DisplayName = "Full Control User",
            IsActive = true
        });
        context.PermissionRules.Add(new ChillSharp.Auth.Model.AuthPermissionRule
        {
            Guid = Guid.NewGuid(),
            UserGuid = userGuid,
            Effect = PermissionEffect.Allow,
            Action = PermissionAction.FullControl,
            Scope = PermissionScope.Entity,
            Module = "Blog",
            EntityName = "Post",
            Description = "Fallback full control",
            CreatedUtc = DateTime.UtcNow
        });
        context.PermissionRules.Add(new ChillSharp.Auth.Model.AuthPermissionRule
        {
            Guid = Guid.NewGuid(),
            UserGuid = userGuid,
            Effect = PermissionEffect.Deny,
            Action = PermissionAction.Delete,
            Scope = PermissionScope.Entity,
            Module = "Blog",
            EntityName = "Post",
            Description = "Delete denied explicitly",
            CreatedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new ChillAuthService(context, context, new ChillAuthManagementAccessCache());

        var createResult = await service.EvaluateEntityPermissionAsync(new EvaluateEntityPermissionRequest
        {
            UserGuid = userGuid,
            Action = PermissionAction.Create,
            Module = "Blog",
            EntityName = "Post"
        });

        var deleteResult = await service.EvaluateEntityPermissionAsync(new EvaluateEntityPermissionRequest
        {
            UserGuid = userGuid,
            Action = PermissionAction.Delete,
            Module = "Blog",
            EntityName = "Post"
        });

        Assert.IsTrue(createResult.IsAllowed);
        Assert.AreEqual(PermissionEffect.Allow, createResult.MatchedEffect);
        Assert.IsFalse(deleteResult.IsAllowed);
        Assert.AreEqual(PermissionEffect.Deny, deleteResult.MatchedEffect);
    }

    /// <summary>
    /// Verifies that an authenticated permission manager cannot change their own management flags or active state through the legacy update endpoint.
    /// </summary>
    [TestMethod]
    public async Task Step009_SelfUpdateCannotChangeSensitiveFlags()
    {
        SecuredAuthApiHost.EnsureStarted();

        var externalId = $"self-update-{Guid.NewGuid():N}";
        var userGuid = Guid.NewGuid();

        await using (var seedContext = SecuredAuthApiHost.CreateDbContext())
        {
            seedContext.Users.Add(new ChillSharp.Auth.Model.AuthUser
            {
                Guid = userGuid,
                ExternalId = externalId,
                UserName = $"self.update.{Guid.NewGuid():N}",
                DisplayName = "Self Update User",
                IsActive = true,
                CanManagePermissions = true,
                CanManageSchema = false
            });
            await seedContext.SaveChangesAsync();
        }

        var client = CreateTestHeaderClient("http://localhost:5001/api/chill", externalId);
        var response = client.UpdateAuthUser(userGuid, new UpdateAuthUserRequest
        {
            ExternalId = externalId,
            UserName = $"self.update.changed.{Guid.NewGuid():N}",
            DisplayName = "Self Update User Changed",
            DisplayCultureName = "en-US",
            DisplayTimeZone = "Eastern Standard Time",
            DisplayDateFormat = "MM/DD/YYYY",
            DisplayNumberFormat = "1,000.00",
            IsActive = false,
            CanManagePermissions = false,
            CanManageSchema = true,
            MenuHierarchy = "SELF.TEST"
        });

        Assert.IsNotNull(response);

        await using var verificationContext = SecuredAuthApiHost.CreateDbContext();
        var persistedUser = await verificationContext.Users.FirstAsync(x => x.Guid == userGuid);
        Assert.IsTrue(persistedUser.IsActive);
        Assert.IsTrue(persistedUser.CanManagePermissions);
        Assert.IsFalse(persistedUser.CanManageSchema);
        Assert.AreEqual("SELF.TEST", persistedUser.MenuHierarchy);
        Assert.AreEqual("Self Update User Changed", persistedUser.DisplayName);
    }

    /// <summary>
    /// Verifies that an authenticated permission manager cannot grant themselves roles or direct permission rules through the merged CRUD routes.
    /// </summary>
    [TestMethod]
    public async Task Step010_SelfCrudCannotGrantRolesOrDirectPermissions()
    {
        SecuredAuthApiHost.EnsureStarted();

        var externalId = $"self-set-{Guid.NewGuid():N}";
        var userGuid = Guid.NewGuid();
        var roleGuid = Guid.NewGuid();

        await using (var seedContext = SecuredAuthApiHost.CreateDbContext())
        {
            seedContext.Users.Add(new ChillSharp.Auth.Model.AuthUser
            {
                Guid = userGuid,
                ExternalId = externalId,
                UserName = $"self.set.{Guid.NewGuid():N}",
                DisplayName = "Self Set User",
                IsActive = true,
                CanManagePermissions = true,
                CanManageSchema = false
            });
            seedContext.Roles.Add(new ChillSharp.Auth.Model.AuthRole
            {
                Guid = roleGuid,
                Name = $"SelfSetRole-{Guid.NewGuid():N}",
                Description = "Role used to test self assignment",
                IsActive = true
            });
            await seedContext.SaveChangesAsync();
        }

        var client = CreateTestHeaderClient("http://localhost:5001/api/chill", externalId);

        try
        {
            client.AssignAuthRole(userGuid, roleGuid);
            Assert.Fail("Self role assignment should have failed.");
        }
        catch (ChillClientException ex)
        {
            Assert.IsTrue(ex.Message.Contains("BadRequest", StringComparison.OrdinalIgnoreCase) ||
                          ex.Message.Contains("400", StringComparison.OrdinalIgnoreCase));
        }

        try
        {
            client.CreateAuthPermissionRule(new CreateAuthPermissionRuleRequest
            {
                UserGuid = userGuid,
                Effect = PermissionEffect.Allow,
                Action = PermissionAction.Update,
                Scope = PermissionScope.Entity,
                Module = "Blog",
                EntityName = "Post",
                Description = "Attempt to self-grant direct permission"
            });
            Assert.Fail("Self permission assignment should have failed.");
        }
        catch (ChillClientException ex)
        {
            Assert.IsTrue(ex.Message.Contains("BadRequest", StringComparison.OrdinalIgnoreCase) ||
                          ex.Message.Contains("400", StringComparison.OrdinalIgnoreCase));
        }

        await using var verificationContext = SecuredAuthApiHost.CreateDbContext();
        var persistedUser = await verificationContext.Users.FirstAsync(x => x.Guid == userGuid);
        var persistedMemberships = await verificationContext.UserRoles
            .Where(x => x.UserGuid == userGuid)
            .CountAsync();
        var persistedRules = await verificationContext.PermissionRules
            .Where(x => x.UserGuid == userGuid)
            .CountAsync();

        Assert.IsFalse(persistedUser.CanManageSchema);
        Assert.AreEqual(0, persistedMemberships);
        Assert.AreEqual(0, persistedRules);
    }

    /// <summary>
    /// Verifies that set-user rejects invalid display culture casing and non-IANA time-zone identifiers.
    /// </summary>
    [TestMethod]
    public async Task Step011_SetUserRejectsInvalidCultureAndTimeZoneFormats()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "ChillSharpTestContext", $"set-user-validation-{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        var options = new DbContextOptionsBuilder<EF.DummyContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using var context = new EF.DummyContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var service = new ChillAuthService(context, context, new ChillAuthManagementAccessCache());

        try
        {
            await service.SetUserAsync(new SetAuthUserRequest
            {
                ExternalId = $"set-user-{Guid.NewGuid():N}",
                UserName = $"set.user.{Guid.NewGuid():N}",
                DisplayName = "Invalid Culture User",
                DisplayCultureName = "en-gb",
                DisplayTimeZone = "Europe/London"
            });
            Assert.Fail("set-user should reject invalid DisplayCultureName casing.");
        }
        catch (ArgumentException ex)
        {
            StringAssert.Contains(ex.Message, "DisplayCultureName");
        }

        try
        {
            await service.SetUserAsync(new SetAuthUserRequest
            {
                ExternalId = $"set-user-{Guid.NewGuid():N}",
                UserName = $"set.user.{Guid.NewGuid():N}",
                DisplayName = "Invalid Time Zone User",
                DisplayCultureName = "en-GB",
                DisplayTimeZone = "Eastern Standard Time"
            });
            Assert.Fail("set-user should reject non-IANA DisplayTimeZone values.");
        }
        catch (ArgumentException ex)
        {
            StringAssert.Contains(ex.Message, "DisplayTimeZone");
        }
    }

    /// <summary>
    /// Verifies that set-user accepts specific culture names and IANA time-zone identifiers.
    /// </summary>
    [TestMethod]
    public async Task Step012_SetUserAcceptsSpecificCultureAndIanaTimeZone()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "ChillSharpTestContext", $"set-user-valid-{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        var options = new DbContextOptionsBuilder<EF.DummyContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using var context = new EF.DummyContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var service = new ChillAuthService(context, context, new ChillAuthManagementAccessCache());

        var response = await service.SetUserAsync(new SetAuthUserRequest
        {
            ExternalId = $"set-user-{Guid.NewGuid():N}",
            UserName = $"set.user.{Guid.NewGuid():N}",
            DisplayName = "Valid Preferences User",
            DisplayCultureName = "it-IT",
            DisplayTimeZone = "Europe/Rome",
            DisplayDateFormat = "DD/MM/YYYY",
            DisplayNumberFormat = "1.000,00",
            IsActive = true
        });

        Assert.AreEqual("it-IT", response.DisplayCultureName);
        Assert.AreEqual("Europe/Rome", response.DisplayTimeZone);

        var persistedUser = await context.Users.FirstAsync(x => x.Guid == response.Guid);
        Assert.AreEqual("it-IT", persistedUser.DisplayCultureName);
        Assert.AreEqual("Europe/Rome", persistedUser.DisplayTimeZone);
    }

    private static ChillSharpClient CreateTestHeaderClient(string baseUrl, string externalId)
    {
        return new ChillSharpClient(baseUrl, () =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Test-User", externalId);
            return client;
        });
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

    private static class MergedManagementApiHost
    {
        private static readonly object SyncRoot = new();
        private static readonly string DatabasePath = Path.Combine(Path.GetTempPath(), "ChillSharpTestContext", "merged-management-auth-api-host.db");
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
                    builder.WebHost.UseUrls("http://localhost:6012");
                    builder.Services.AddDbContext<EF.DummyContext>(options =>
                        options.UseSqlite($"Data Source={DatabasePath}"));
                    builder.Services.AddChillApi<EF.DummyContext>();

                    var app = builder.Build();
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
                    builder.Services.AddChillApi<EF.DummyContext, IdentityUser>(options =>
                    {
                        options.ProtectedApi = true;
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
                    builder.Services.AddChillApi<EF.DummyContext, IdentityUser>(options =>
                    {
                        options.ProtectedApi = true;
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
