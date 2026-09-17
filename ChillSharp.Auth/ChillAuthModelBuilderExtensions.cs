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
/// Provides shared EF Core model configuration for the ChillSharp authorization entities.
/// </summary>
public static class ChillAuthModelBuilderExtensions
{
    /// <summary>
    /// Applies the ChillSharp authorization model to an existing <see cref="ModelBuilder"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The same model builder instance.</returns>
    public static ModelBuilder AddChillAuthModel(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthUser>(builder =>
        {
            builder.Property(x => x.ExternalId).HasMaxLength(256);
            builder.Property(x => x.UserName).HasMaxLength(256);
            builder.Property(x => x.DisplayName).HasMaxLength(256);
            builder.Property(x => x.MenuHierarchy).HasMaxLength(512);
            builder.HasIndex(x => x.ExternalId).IsUnique();
            builder.HasIndex(x => x.UserName).IsUnique();
        });

        modelBuilder.Entity<AuthRole>(builder =>
        {
            builder.Property(x => x.Name).HasMaxLength(128);
            builder.Property(x => x.Description).HasMaxLength(1024);
            builder.Property(x => x.MenuHierarchy).HasMaxLength(512);
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

        modelBuilder.Entity<AuthRefreshToken>(builder =>
        {
            builder.Property(x => x.IdentityUserId).HasMaxLength(256);
            builder.Property(x => x.UserName).HasMaxLength(256);
            builder.Property(x => x.TokenHash).HasMaxLength(256);

            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.HasIndex(x => x.IdentityUserId);
            builder.HasIndex(x => x.ExpiresUtc);
        });

        return modelBuilder;
    }
}
