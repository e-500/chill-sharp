using ChillSharp.Mcp.Contracts;

namespace ChillSharp.Mcp;

/// <summary>
/// Resolves MCP-enabled Chill resources from runtime schema metadata.
/// </summary>
public interface IChillMcpService
{
    Task<IReadOnlyList<ChillMcpResource>> GetResourcesAsync(string? cultureName = null, CancellationToken cancellationToken = default);

    Task<ChillMcpResource?> GetResourceAsync(string chillType, string? cultureName = null, CancellationToken cancellationToken = default);
}
