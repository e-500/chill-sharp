namespace ChillSharp.Schema.Contracts;

/// <summary>
/// Read-only contract for runtime-configurable options persisted for a specific Chill entity type.
/// </summary>
public interface IChillDtoEntityOptions
{
    string ChillType { get; }
    bool ChecksumEnabled { get; }
    bool HandleAttachments { get; }
    string? LabelFormatString { get; }
    string? ShortLabelFormatString { get; }
    string? FullTextContentFormatString { get; }
    bool EnableMCP { get; }
    string? MCPDescription { get; }
    bool ChangeLogEnabled { get; }
}
