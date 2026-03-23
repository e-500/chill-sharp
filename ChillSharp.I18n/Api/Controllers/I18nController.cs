using ChillSharp.I18n.Contracts;
using ChillSharp.I18n.Services;
using Microsoft.AspNetCore.Authorization;
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
    [AllowAnonymous]
    [HttpGet("get-text")]
    public async Task<IActionResult> GetText([FromBody] GetTextRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _service.GetTextAsync(request, cancellationToken);
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

    /// <summary>
    /// Gets multiple localized texts.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("get-multiple-text")]
    public async Task<IActionResult> GetMultipleText([FromBody] GetTextRequest[] requests, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.GetTextsAsync(requests, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Creates or updates the localized text for a label guid and culture name.
    /// </summary>
    [HttpPut("set-text")]
    public async Task<IActionResult> SetText([FromBody] SetTextRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.SetTextAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
