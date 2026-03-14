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
    /// Returns the namespace prefix used by ChillSharp to resolve auth entity types dynamically.
    /// </summary>
    /// <returns>The model namespace prefix.</returns>
    public string GetChillTypePrefix()
    {
        return "ChillSharp.Auth.Model";
    }

    /// <summary>
    /// Configures indexes, key definitions, and relationships for the auth model.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure EF Core metadata.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuthUser>(builder =>
        {
            builder.Property(x => x.ExternalId).HasMaxLength(256);
            builder.Property(x => x.UserName).HasMaxLength(256);
            builder.Property(x => x.DisplayName).HasMaxLength(256);
            builder.HasIndex(x => x.ExternalId).IsUnique();
            builder.HasIndex(x => x.UserName).IsUnique();
        });

        modelBuilder.Entity<AuthRole>(builder =>
        {
            builder.Property(x => x.Name).HasMaxLength(128);
            builder.Property(x => x.Description).HasMaxLength(1024);
            builder.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<AuthUserRole>(builder =>
        {
            builder.HasKey(x => new { x.UserGuid, x.RoleGuid });

            builder.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserGuid)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleGuid)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuthPermissionRule>(builder =>
        {
            builder.Property(x => x.Effect).HasConversion<string>().HasMaxLength(16);
            builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(16);
            builder.Property(x => x.Scope).HasConversion<string>().HasMaxLength(16);
            builder.Property(x => x.Module).HasMaxLength(256);
            builder.Property(x => x.EntityName).HasMaxLength(128);
            builder.Property(x => x.PropertyName).HasMaxLength(128);
            builder.Property(x => x.Description).HasMaxLength(1024);

            builder.HasIndex(x => x.UserGuid);
            builder.HasIndex(x => x.RoleGuid);
            builder.HasIndex(x => new { x.UserGuid, x.Scope, x.Action, x.Module, x.EntityName, x.PropertyName });
            builder.HasIndex(x => new { x.RoleGuid, x.Scope, x.Action, x.Module, x.EntityName, x.PropertyName });

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserGuid)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleGuid)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
