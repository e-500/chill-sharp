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

using ChillSharp.Auth.Api;
using ChillSharp.Auth.Services;
using ChillSharp.EF;
using ChillSharp.Schema.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ChillSharp.Schema.Api.Controllers;

/// <summary>
/// Exposes the schema-management endpoints from the schema module.
/// </summary>
[ApiController]
[Route("api/chill-schema")]
public sealed class ChillSchemaController : ControllerBase
{
    private readonly IChillContext _context;
    private readonly IChillSchemaService _schemaService;
    private readonly IChillAuthService? _authService;
    private readonly IChillAuthIdentityResolver? _identityResolver;

    public ChillSchemaController(
        IChillContext context,
        IChillSchemaService schemaService,
        IChillAuthService? authService = null,
        IChillAuthIdentityResolver? identityResolver = null)
    {
        _context = context;
        _schemaService = schemaService;
        _authService = authService;
        _identityResolver = identityResolver;

        if (schemaService is IChillSchemaResolverService schemaResolver)
        {
            _context.RegisterSchemaService(schemaResolver);
        }
    }

    [HttpGet("get-schema")]
    public async Task<IActionResult> GetSchema([FromQuery] string ChillType, [FromQuery] string ChillViewCode, [FromQuery] string? CultureName = null)
    {
        return Ok(await _schemaService.GetSchemaAsync(ChillType, ChillViewCode, CultureName));
    }

    [HttpGet("get-schema-list")]
    public IActionResult GetSchemaList([FromQuery] string? CultureName = null)
    {
        return Ok(BuildSchemaList(CultureName));
    }

    [HttpPost("set-schema")]
    [ServiceFilter(typeof(ChillSchemaManagementAccessFilter))]
    public async Task<IActionResult> SetSchema([FromBody] ChillDtoSchema Schema)
    {
        return Ok(await _schemaService.SetSchemaAsync(Schema));
    }

    [HttpGet("get-entity-options")]
    [ServiceFilter(typeof(ChillSchemaManagementAccessFilter))]
    public async Task<IActionResult> GetEntityOptions([FromQuery] string ChillType)
    {
        return Ok(await _schemaService.GetEntityOptionsAsync(ChillType));
    }

    [HttpPost("set-entity-options")]
    [ServiceFilter(typeof(ChillSchemaManagementAccessFilter))]
    public async Task<IActionResult> SetEntityOptions([FromBody] ChillDtoEntityOptions EntityOptions)
    {
        return Ok(await _schemaService.SetEntityOptionsAsync(EntityOptions));
    }

    [HttpGet("get-menu")]
    public async Task<IActionResult> GetMenu([FromQuery] Guid? ParentGuid = null, CancellationToken cancellationToken = default)
    {
        var schemaService = _schemaService ?? throw new ChillException("Chill schema service is not registered.");
        var menuItems = await schemaService.GetMenuAsync(ParentGuid, cancellationToken);
        return Ok(await FilterMenuAsync(menuItems, cancellationToken));
    }

    [HttpPost("set-menu")]
    [ServiceFilter(typeof(ChillSchemaManagementAccessFilter))]
    public async Task<IActionResult> SetMenu([FromBody] ChillDtoMenuItem MenuItem, CancellationToken cancellationToken)
    {
        var schemaService = _schemaService ?? throw new ChillException("Chill schema service is not registered.");
        return Ok(await schemaService.SetMenuAsync(MenuItem, cancellationToken));
    }


    [HttpDelete("delete-menu")]
    [ServiceFilter(typeof(ChillSchemaManagementAccessFilter))]
    public async Task<IActionResult> DeleteMenu([FromQuery] Guid MenuItemGuid, CancellationToken cancellationToken)
    {
        var schemaService = _schemaService ?? throw new ChillException("Chill schema service is not registered.");
        await schemaService.DeleteMenuAsync(MenuItemGuid, cancellationToken);
        return NoContent();
    }
    private List<ChillDtoSchemaListItem> BuildSchemaList(string? cultureName)
    {
        var assembly = _context.GetType().Assembly;
        var shrinkTypePrefix = _context.GetChillTypePrefix();

        var entityItems = assembly
            .GetTypes()
            .Where(IsRegisteredEntityType)
            .Select(type => ChillDtoSchemaListItem.FromEntityType(type, shrinkTypePrefix, _context, cultureName));

        var queryItems = assembly
            .GetTypes()
            .Where(IsRegisteredQueryType)
            .Select(type => ChillDtoSchemaListItem.CreateFromQueryType(type, shrinkTypePrefix, _context, cultureName));

        return entityItems
            .Concat(queryItems)
            .OrderBy(x => x.Type, StringComparer.Ordinal)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ChillType, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsRegisteredEntityType(Type type)
    {
        return (type.IsPublic || type.IsNestedPublic)
            && type.IsClass
            && !type.IsAbstract
            && typeof(IChillEntity).IsAssignableFrom(type);
    }

    private static bool IsRegisteredQueryType(Type type)
    {
        return (type.IsPublic || type.IsNestedPublic)
            && type.IsClass
            && !type.IsAbstract
            && typeof(IChillQuery<IChillEntity>).IsAssignableFrom(type);
    }

    private async Task<IReadOnlyList<ChillDtoMenuItem>> FilterMenuAsync(IReadOnlyList<ChillDtoMenuItem> menuItems, CancellationToken cancellationToken)
    {
        if (_authService == null || _identityResolver == null || HttpContext.User.Identity?.IsAuthenticated != true)
            return menuItems;

        var externalId = _identityResolver.ResolveExternalId(HttpContext.User);
        if (string.IsNullOrWhiteSpace(externalId))
            return [];

        var user = await _authService.GetUserByExternalIdAsync(externalId, cancellationToken);
        if (user == null || !user.IsActive)
            return [];

        var hierarchies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddMenuHierarchy(hierarchies, user.MenuHierarchy);

        var roles = await _authService.GetUserRolesAsync(user.Guid, cancellationToken);
        foreach (var role in roles.Where(x => x.IsActive))
        {
            AddMenuHierarchy(hierarchies, role.MenuHierarchy);
        }

        if (hierarchies.Contains("*"))
            return menuItems;

        if (hierarchies.Count == 0)
            return [];

        return menuItems
            .Where(item => IsMenuAllowed(item.MenuHierarchy, hierarchies))
            .ToList();
    }

    private static void AddMenuHierarchy(HashSet<string> hierarchies, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            hierarchies.Add(value.Trim());
    }

    private static bool IsMenuAllowed(string? menuHierarchy, IReadOnlyCollection<string> allowedHierarchies)
    {
        var normalizedHierarchy = menuHierarchy?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedHierarchy))
            return false;

        return allowedHierarchies.Any(prefix => normalizedHierarchy.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
