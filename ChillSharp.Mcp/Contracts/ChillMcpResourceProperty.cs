namespace ChillSharp.Mcp.Contracts;

/// <summary>
/// Describes a property exposed through an MCP-enabled Chill resource.
/// </summary>
public sealed class ChillMcpResourceProperty
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string PropertyType { get; set; } = string.Empty;

    public string ReferenceChillType { get; set; } = string.Empty;
}
