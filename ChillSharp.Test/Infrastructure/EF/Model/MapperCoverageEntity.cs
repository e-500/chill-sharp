using ChillSharp.Annotations;
using ChillSharp.EF;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChillSharp.Tests.EF.Model;

[ChillEntity(
    UniquePropertyKeyString: "F62A59A4-C7CE-4AEE-9081-5A21C989B88B",
    PrimaryLanguageLabel: "Mapper coverage",
    SecondaryLanguageLabel: "Mapper coverage")]
public class MapperCoverageEntity : ChillEntity
{
    [Key]
    public override Guid Guid { get; set; }

    [ChillProperty] public string MappedString { get; set; } = string.Empty;
    [ChillProperty] public Guid MappedGuid { get; set; }
    [ChillProperty] public int MappedInteger { get; set; }
    [ChillProperty] public decimal MappedDecimal { get; set; }
    [ChillProperty] public DateOnly MappedDate { get; set; }
    [ChillProperty] public TimeOnly MappedTime { get; set; }
    [ChillProperty] public DateTime MappedDateTime { get; set; }
    [ChillProperty] public DateTimeOffset MappedDateTimeOffset { get; set; }
    [ChillProperty] public TimeSpan MappedDuration { get; set; }
    [ChillProperty] public bool MappedBoolean { get; set; }

    [ChillProperty] public MapperCoverageRelated? MappedEntity { get; set; }
    [ChillProperty] public ICollection<MapperCoverageRelated> MappedCollection { get; set; } = [];

    [NotMapped, ChillProperty] public string UnmappedString { get; set; } = string.Empty;
    [NotMapped, ChillProperty] public Guid UnmappedGuid { get; set; }
    [NotMapped, ChillProperty] public int UnmappedInteger { get; set; }
    [NotMapped, ChillProperty] public decimal UnmappedDecimal { get; set; }
    [NotMapped, ChillProperty] public DateOnly UnmappedDate { get; set; }
    [NotMapped, ChillProperty] public TimeOnly UnmappedTime { get; set; }
    [NotMapped, ChillProperty] public DateTime UnmappedDateTime { get; set; }
    [NotMapped, ChillProperty] public DateTimeOffset UnmappedDateTimeOffset { get; set; }
    [NotMapped, ChillProperty] public TimeSpan UnmappedDuration { get; set; }
    [NotMapped, ChillProperty] public bool UnmappedBoolean { get; set; }

    [NotMapped, ChillProperty] public MapperCoverageRelated? UnmappedEntity { get; set; }
    [NotMapped, ChillProperty] public ICollection<MapperCoverageRelated> UnmappedCollection { get; set; } = [];
}

[ChillEntity(
    UniquePropertyKeyString: "0A4319EE-0642-4E98-910C-6D51D1E580BE",
    PrimaryLanguageLabel: "Mapper related",
    SecondaryLanguageLabel: "Mapper related")]
public class MapperCoverageRelated : ChillEntity
{
    [Key]
    public override Guid Guid { get; set; }

    [ChillProperty]
    public string Name { get; set; } = string.Empty;
}
