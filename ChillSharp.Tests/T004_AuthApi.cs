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
using Microsoft.EntityFrameworkCore;
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

        // Use a raw HttpClient because this test targets the auth-specific endpoints directly.
        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };

        // Create an auth user through the REST API.
        var createUserResponse = await client.PostAsJsonAsync("api/chill-auth/users", new CreateAuthUserRequest
        {
            ExternalId = "user-auth-test-001",
            UserName = "auth.test",
            DisplayName = "Auth Test",
            IsActive = true
        });

        createUserResponse.EnsureSuccessStatusCode();
        var user = await createUserResponse.Content.ReadFromJsonAsync<AuthUser>();
        Assert.IsNotNull(user);

        // Create an auth role through the REST API.
        var createRoleResponse = await client.PostAsJsonAsync("api/chill-auth/roles", new CreateAuthRoleRequest
        {
            Name = "TestRole",
            Description = "Role created by integration test",
            IsActive = true
        });

        createRoleResponse.EnsureSuccessStatusCode();
        var role = await createRoleResponse.Content.ReadFromJsonAsync<AuthRole>();
        Assert.IsNotNull(role);

        // Assign the created role to the created user.
        var assignRoleResponse = await client.PutAsync($"api/chill-auth/users/{user.Guid}/roles/{role.Guid}", null);
        assignRoleResponse.EnsureSuccessStatusCode();

        // Open the same DummyContext database directly and verify that all auth records were persisted there.
        await using var verificationContext = new EF.DummyContext();
        var persistedUser = await verificationContext.Users.FirstOrDefaultAsync(x => x.Guid == user.Guid);
        var persistedRole = await verificationContext.Roles.FirstOrDefaultAsync(x => x.Guid == role.Guid);
        var persistedMembership = await verificationContext.UserRoles.FirstOrDefaultAsync(x => x.UserGuid == user.Guid && x.RoleGuid == role.Guid);

        Assert.IsNotNull(persistedUser);
        Assert.IsNotNull(persistedRole);
        Assert.IsNotNull(persistedMembership);
    }
}
