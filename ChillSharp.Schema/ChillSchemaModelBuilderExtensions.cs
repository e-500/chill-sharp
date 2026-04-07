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

using ChillSharp.Schema.Model;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Schema;

/// <summary>
/// Provides shared EF Core model configuration for the ChillSharp schema persistence model.
/// </summary>
public static class ChillSchemaModelBuilderExtensions
{
    /// <summary>
    /// Applies the ChillSharp schema persistence model to an existing <see cref="ModelBuilder"/>.
    /// </summary>
    public static ModelBuilder AddChillSchemaModel(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChillSchemaEntry>(builder =>
        {
            builder.Property(x => x.ChillType).HasMaxLength(512);
            builder.Property(x => x.ChillViewCode).HasMaxLength(128);
            builder.Property(x => x.Json).HasMaxLength(int.MaxValue);
            builder.HasIndex(x => new { x.ChillType, x.ChillViewCode }).IsUnique();
        });

        modelBuilder.Entity<ChillEntityOptionsEntry>(builder =>
        {
            builder.Property(x => x.ChillType).HasMaxLength(512);
            builder.Property(x => x.LabelFormatString).HasMaxLength(2048);
            builder.Property(x => x.ShortLabelFormatString).HasMaxLength(2048);
            builder.Property(x => x.FullTextContentFormatString).HasMaxLength(4096);
            builder.Property(x => x.MCPDescription).HasMaxLength(4096);
            builder.HasIndex(x => x.ChillType).IsUnique();
        });


        modelBuilder.Entity<ChillMenuItemEntry>(builder =>
        {
            builder.Property(x => x.Title).HasMaxLength(255);
            builder.Property(x => x.Description).HasMaxLength(int.MaxValue);
            builder.Property(x => x.ComponentName).HasMaxLength(255);
            builder.Property(x => x.ComponentConfigurationJson).HasMaxLength(int.MaxValue);
            builder.Property(x => x.MenuHierarchy).HasMaxLength(512);

            builder.HasIndex(x => x.ParentGuid);
            builder.HasIndex(x => x.MenuHierarchy);

            builder.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentGuid)
                .OnDelete(DeleteBehavior.Restrict);
        });
        return modelBuilder;
    }
}


