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

using ChillSharp.I18n.Model;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.I18n;

/// <summary>
/// Provides shared EF Core model configuration for the ChillSharp i18n persistence model.
/// </summary>
public static class ChillI18nModelBuilderExtensions
{
    /// <summary>
    /// Applies the ChillSharp i18n persistence model to an existing <see cref="ModelBuilder"/>.
    /// </summary>
    public static ModelBuilder AddChillI18nModel(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Text>(builder =>
        {
            builder.Property(x => x.CultureCode).HasMaxLength(16);
            builder.Property(x => x.Value).HasMaxLength(int.MaxValue);
            builder.HasIndex(x => new { x.LabelGuid, x.CultureCode }).IsUnique();
        });

        return modelBuilder;
    }
}
