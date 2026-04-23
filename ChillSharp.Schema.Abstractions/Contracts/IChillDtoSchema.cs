namespace ChillSharp.Schema.Contracts;

/// <summary>
/// Read-only contract for DTO schema metadata shared with ChillSharp core components.
/// </summary>
public interface IChillDtoSchema
{
    string ChillType { get; }
    string ChillViewCode { get; }
    string DisplayName { get; }
    bool HandleAttachments { get; }
    bool EnableMCP { get; }
    string MCPDescription { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
    string? QueryRelatedChillType { get; }
    IReadOnlyList<IChillDtoPropertySchema> Properties { get; }
    IReadOnlyList<IChillDtoSchemaRelation> Relations { get; }
}
