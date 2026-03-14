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

using ChillSharp.Auth.Model;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Auth;

/// <summary>
/// Defines the persistence contract required by the auth services.
/// </summary>
public interface IChillAuthDbContext
{
    /// <summary>
    /// Gets the users managed by the authorization store.
    /// </summary>
    DbSet<AuthUser> Users { get; }

    /// <summary>
    /// Gets the roles managed by the authorization store.
    /// </summary>
    DbSet<AuthRole> Roles { get; }

    /// <summary>
    /// Gets the user-to-role memberships.
    /// </summary>
    DbSet<AuthUserRole> UserRoles { get; }

    /// <summary>
    /// Gets the permission rules assigned to users or roles.
    /// </summary>
    DbSet<AuthPermissionRule> PermissionRules { get; }

    /// <summary>
    /// Gets the refresh-token sessions issued for authenticated clients.
    /// </summary>
    DbSet<AuthRefreshToken> RefreshTokens { get; }

    /// <summary>
    /// Persists changes to the underlying store.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the save operation.</param>
    /// <returns>The number of state entries written to the store.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
