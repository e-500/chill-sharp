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
/// EF Core context containing the persisted Chill DTO schemas.
/// </summary>
public class ChillSchemaDbContext : DbContext, IChillSchemaDbContext
{
    /// <summary>
    /// Initializes a new schema context instance.
    /// </summary>
    public ChillSchemaDbContext(DbContextOptions<ChillSchemaDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets the set of persisted schema rows.
    /// </summary>
    public DbSet<ChillSchemaEntry> SchemaEntries => Set<ChillSchemaEntry>();

    /// <summary>
    /// Gets the set of persisted runtime entity-options rows.
    /// </summary>
    public DbSet<ChillEntityOptionsEntry> EntityOptionsEntries => Set<ChillEntityOptionsEntry>();

    /// <summary>
    /// Configures indexes and constraints for the schema persistence model.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddChillSchemaModel();
    }
}
