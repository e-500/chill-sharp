using Microsoft.AspNetCore.Mvc;

namespace ChillSharp.Mcp.Api.Controllers;

/// <summary>
/// Exposes MCP-enabled Chill resources discovered from runtime schema metadata.
/// </summary>
[ApiController]
[Route("api/chill-mcp")]
public sealed class ChillMcpController : ControllerBase
{
    private readonly IChillMcpService _service;

    public ChillMcpController(IChillMcpService service)
    {
        _service = service;
    }

    [HttpGet("get-resource-list")]
    public async Task<IActionResult> GetResourceList([FromQuery] string? cultureName = null, CancellationToken cancellationToken = default)
    {
        return Ok(await _service.GetResourcesAsync(cultureName, cancellationToken));
    }

    [HttpGet("get-resource")]
    public async Task<IActionResult> GetResource([FromQuery] string chillType, [FromQuery] string? cultureName = null, CancellationToken cancellationToken = default)
    {
        var resource = await _service.GetResourceAsync(chillType, cultureName, cancellationToken);
        return resource == null ? NotFound() : Ok(resource);
    }
}
