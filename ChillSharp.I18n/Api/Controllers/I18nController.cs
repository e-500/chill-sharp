using ChillSharp.I18n.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChillSharp.I18n.Api.Controllers;

/// <summary>
/// Exposes endpoints for localized text lookup.
/// </summary>
[ApiController]
[Route("api/chill-i18n")]
public sealed class I18nController : ControllerBase
{
    private readonly IChillI18nService _service;

    public I18nController(IChillI18nService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets the localized text for a label guid and culture name.
    /// </summary>
    [HttpGet("text/{labelGuid:guid}/{cultureName}")]
    public async Task<IActionResult> GetText(Guid labelGuid, string cultureName, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _service.GetTextAsync(labelGuid, cultureName, cancellationToken);
            if (response is null)
            {
                return NotFound();
            }

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
