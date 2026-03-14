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
using ChillSharp.Client;
using Microsoft.EntityFrameworkCore;

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
        await using var verificationContext = new EF.DummyContext();
        var persistedUser = await verificationContext.Users.FirstOrDefaultAsync(x => x.Guid == user.Guid);
        var persistedRole = await verificationContext.Roles.FirstOrDefaultAsync(x => x.Guid == role.Guid);
        var persistedMembership = await verificationContext.UserRoles.FirstOrDefaultAsync(x => x.UserGuid == user.Guid && x.RoleGuid == role.Guid);

        Assert.IsNotNull(persistedUser);
        Assert.IsNotNull(persistedRole);
        Assert.IsNotNull(persistedMembership);
    }
}
