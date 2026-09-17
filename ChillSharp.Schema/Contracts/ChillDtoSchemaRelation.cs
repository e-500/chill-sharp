namespace ChillSharp.Schema.Contracts;

/// <summary>
/// Relation metadata derived from an annotated entity collection.
/// </summary>
public class ChillDtoSchemaRelation : IChillDtoSchemaRelation
{
    public string ChillType { get; set; } = string.Empty;

    public string ChillQuery { get; set; } = string.Empty;

    public Dictionary<string, string> FixedValues { get; set; } = new();

    public Dictionary<string, string> FixedQueryValues { get; set; } = new();

    public ChillDtoSchemaRelationLabel RelationLabel { get; set; } = new();

    IReadOnlyDictionary<string, string> IChillDtoSchemaRelation.FixedValues => FixedValues;

    IReadOnlyDictionary<string, string> IChillDtoSchemaRelation.FixedQueryValues => FixedQueryValues;

    IChillDtoSchemaRelationLabel IChillDtoSchemaRelation.RelationLabel => RelationLabel;
}
