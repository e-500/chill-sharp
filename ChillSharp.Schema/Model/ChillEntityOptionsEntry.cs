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
/// Persists runtime entity options for a specific Chill type.
/// </summary>
[Table("entity-options-entry")]
public class ChillEntityOptionsEntry
{
    [Key]
    [Column("guid")]
    public Guid Guid { get; set; }

    [Column("chill-type")]
    public string ChillType { get; set; } = string.Empty;

    [Column("checksum-enabled")]
    public bool ChecksumEnabled { get; set; } = true;

    [Column("handle-attachments")]
    public bool HandleAttachments { get; set; }

    [Column("label-format-string")]
    public string? LabelFormatString { get; set; }

    [Column("short-label-format-string")]
    public string? ShortLabelFormatString { get; set; }

    [Column("full-text-content-format-string")]
    public string? FullTextContentFormatString { get; set; }

    [Column("enable-mcp")]
    public bool EnableMCP { get; set; }

    [Column("mcp-description")]
    public string? MCPDescription { get; set; }

    [Column("change-log-enabled")]
    public bool ChangeLogEnabled { get; set; }

    [Column("updated-utc")]
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
