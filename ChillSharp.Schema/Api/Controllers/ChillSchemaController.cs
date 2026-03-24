using ChillSharp.Dto;
using ChillSharp.EF;
using Microsoft.AspNetCore.Mvc;

namespace ChillSharp.Schema.Api.Controllers;

/// <summary>
/// Exposes the schema-management endpoints from the schema module.
/// </summary>
[ApiController]
[Route("api/chill-schema")]
public sealed class ChillSchemaController : ControllerBase
{
    private readonly IChillDtoEngine _dtoEngine;
    private readonly IChillContext _context;

    public ChillSchemaController(IChillDtoEngine dtoEngine, IChillContext context)
    {
        _dtoEngine = dtoEngine;
        _context = context;
    }

    [HttpGet("get-schema")]
    public IActionResult GetSchema([FromQuery] string ChillType, [FromQuery] string ChillViewCode, [FromQuery] string? CultureName = null)
    {
        return Ok(_dtoEngine.GetSchema(ChillType, ChillViewCode, CultureName));
    }

    [HttpGet("get-schema-list")]
    public IActionResult GetSchemaList([FromQuery] string? CultureName = null)
    {
        return Ok(BuildSchemaList(CultureName));
    }

    [HttpPost("set-schema")]
    [ServiceFilter(typeof(ChillSchemaManagementAccessFilter))]
    public IActionResult SetSchema([FromBody] ChillDtoSchema Schema)
    {
        return Ok(_dtoEngine.SetSchema(Schema));
    }

    [HttpGet("get-entity-options")]
    [ServiceFilter(typeof(ChillSchemaManagementAccessFilter))]
    public IActionResult GetEntityOptions([FromQuery] string ChillType)
    {
        return Ok(_dtoEngine.GetEntityOptions(ChillType));
    }

    [HttpPost("set-entity-options")]
    [ServiceFilter(typeof(ChillSchemaManagementAccessFilter))]
    public IActionResult SetEntityOptions([FromBody] ChillDtoEntityOptions EntityOptions)
    {
        return Ok(_dtoEngine.SetEntityOptions(EntityOptions));
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
            .Select(type => ChillDtoSchemaListItem.FromQueryType(type, shrinkTypePrefix, _context, cultureName));

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
}
