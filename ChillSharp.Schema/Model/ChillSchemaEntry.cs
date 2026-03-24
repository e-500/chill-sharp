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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChillSharp.Schema.Model;

/// <summary>
/// Persists a serialized Chill DTO schema for a specific Chill type and view code.
/// </summary>
[Table("schema-entry")]
public class ChillSchemaEntry
{
    /// <summary>
    /// Unique identifier of the persisted schema row.
    /// </summary>
    [Key]
    [Column("guid")]
    public Guid Guid { get; set; }

    /// <summary>
    /// Logical Chill type identifier.
    /// </summary>
    [Column("chill-type")]
    public string ChillType { get; set; } = string.Empty;

    /// <summary>
    /// Logical Chill view code.
    /// </summary>
    [Column("chill-view-code")]
    public string ChillViewCode { get; set; } = string.Empty;

    /// <summary>
    /// Serialized schema payload.
    /// </summary>
    [Column("json")]
    public string Json { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp indicating when the schema row was last updated.
    /// </summary>
    [Column("updated-utc")]
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
