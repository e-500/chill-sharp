namespace ChillSharp.Mcp.Contracts;

/// <summary>
/// Describes an MCP-enabled Chill resource.
/// </summary>
public sealed class ChillMcpResource
{
    public string Uri { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ChillType { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ViewCode { get; set; } = "default";

    public string MimeType { get; set; } = "application/json";

    public string QueryRelatedChillType { get; set; } = string.Empty;

    public List<ChillMcpResourceProperty> Properties { get; set; } = [];
}
