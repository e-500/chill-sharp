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
/// Defines the persistence contract required by the ChillSharp schema service.
/// </summary>
public interface IChillSchemaDbContext
{
    /// <summary>
    /// Gets the persisted schema rows.
    /// </summary>
    DbSet<ChillSchemaEntry> SchemaEntries { get; }

    /// <summary>
    /// Gets the persisted entity-options rows.
    /// </summary>
    DbSet<ChillEntityOptionsEntry> EntityOptionsEntries { get; }

    /// <summary>
    /// Persists changes to the underlying store.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
