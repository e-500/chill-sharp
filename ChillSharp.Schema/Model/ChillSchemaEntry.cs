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
