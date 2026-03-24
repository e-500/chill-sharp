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

    [Column("label-format-string")]
    public string? LabelFormatString { get; set; }

    [Column("short-label-format-string")]
    public string? ShortLabelFormatString { get; set; }

    [Column("full-text-content-format-string")]
    public string? FullTextContentFormatString { get; set; }

    [Column("change-log-enabled")]
    public bool ChangeLogEnabled { get; set; }

    [Column("updated-utc")]
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
