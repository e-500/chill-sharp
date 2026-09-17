namespace ChillSharp.Schema.Contracts;

/// <summary>
/// Read-only contract for schema-level relation metadata derived from entity collections.
/// </summary>
public interface IChillDtoSchemaRelation
{
    string ChillType { get; }
    string ChillQuery { get; }
    IReadOnlyDictionary<string, string> FixedValues { get; }
    IReadOnlyDictionary<string, string> FixedQueryValues { get; }
    IChillDtoSchemaRelationLabel RelationLabel { get; }
}
