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
/// EF Core context containing the reusable authorization model for ChillSharp host applications.
/// </summary>
public class ChillAuthDbContext : DbContext, IChillAuthDbContext, IChillContext
{
    /// <summary>
    /// Initializes a new auth context instance.
    /// </summary>
    /// <param name="options">The EF Core options for the context.</param>
    public ChillAuthDbContext(DbContextOptions<ChillAuthDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets the set of authorization users.
    /// </summary>
    public DbSet<AuthUser> Users => Set<AuthUser>();

    /// <summary>
    /// Gets the set of authorization roles.
    /// </summary>
    public DbSet<AuthRole> Roles => Set<AuthRole>();

    /// <summary>
    /// Gets the set of user-role memberships.
    /// </summary>
    public DbSet<AuthUserRole> UserRoles => Set<AuthUserRole>();

    /// <summary>
    /// Gets the set of permission rules.
    /// </summary>
    public DbSet<AuthPermissionRule> PermissionRules => Set<AuthPermissionRule>();

    /// <summary>
    /// Gets the set of refresh-token sessions.
    /// </summary>
    public DbSet<AuthRefreshToken> RefreshTokens => Set<AuthRefreshToken>();

    /// <summary>
    /// Gets the set of dynamically registered OAuth clients.
    /// </summary>
    public DbSet<AuthOAuthClient> OAuthClients => Set<AuthOAuthClient>();

    /// <summary>
    /// Returns the namespace prefix used by ChillSharp to resolve auth entity types dynamically.
    /// </summary>
    public string GetChillTypePrefix()
    {
        return "ChillSharp.Auth.Model";
    }

    /// <summary>
    /// Returns the culture name associated with PrimaryLanguageLabel metadata.
    /// </summary>
    public string GetPrimaryCultureName()
    {
        return "en-GB";
    }

    /// <summary>
    /// Returns the culture name associated with SecondaryLanguageLabel metadata.
    /// </summary>
    public string GetSecondaryCultureName()
    {
        return "it-IT";
    }

    /// <summary>
    /// Configures indexes, key definitions, and relationships for the auth model.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure EF Core metadata.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddChillAuthModel();
    }
}
