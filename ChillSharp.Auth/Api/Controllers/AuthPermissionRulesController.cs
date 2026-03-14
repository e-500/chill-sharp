/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core 
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using ChillSharp.Auth.Contracts;
using ChillSharp.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChillSharp.Auth.Api.Controllers;

[ApiController]
[Route("api/chill-auth/permissions")]
public class AuthPermissionRulesController : ControllerBase
{
    private readonly IChillAuthService _service;

    public AuthPermissionRulesController(IChillAuthService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetRules([FromQuery] Guid? userGuid, [FromQuery] Guid? roleGuid, CancellationToken cancellationToken)
    {
        return Ok(await _service.GetPermissionRulesAsync(userGuid, roleGuid, cancellationToken));
    }

    [HttpGet("{ruleGuid:guid}")]
    public async Task<IActionResult> GetRule(Guid ruleGuid, CancellationToken cancellationToken)
    {
        var rule = await _service.GetPermissionRuleAsync(ruleGuid, cancellationToken);
        return rule is null ? NotFound() : Ok(rule);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRule([FromBody] CreateAuthPermissionRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _service.CreatePermissionRuleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRule), new { ruleGuid = rule.Guid }, rule);
    }

    [HttpPut("{ruleGuid:guid}")]
    public async Task<IActionResult> UpdateRule(Guid ruleGuid, [FromBody] UpdateAuthPermissionRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _service.UpdatePermissionRuleAsync(ruleGuid, request, cancellationToken);
        return rule is null ? NotFound() : Ok(rule);
    }

    [HttpDelete("{ruleGuid:guid}")]
    public async Task<IActionResult> DeleteRule(Guid ruleGuid, CancellationToken cancellationToken)
    {
        return await _service.DeletePermissionRuleAsync(ruleGuid, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("evaluate/entity")]
    public async Task<IActionResult> EvaluateEntity([FromBody] EvaluateEntityPermissionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.EvaluateEntityPermissionAsync(request, cancellationToken));
    }

    [HttpPost("evaluate/property")]
    public async Task<IActionResult> EvaluateProperty([FromBody] EvaluatePropertyPermissionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.EvaluatePropertyPermissionAsync(request, cancellationToken));
    }

    [HttpPost("evaluate/property-set")]
    public async Task<IActionResult> EvaluatePropertySet([FromBody] EvaluatePropertySetPermissionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.EvaluatePropertySetPermissionAsync(request, cancellationToken));
    }
}
