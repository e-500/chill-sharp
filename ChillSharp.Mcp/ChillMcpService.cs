using ChillSharp.Dto;
using ChillSharp.EF;
using ChillSharp.Mcp.Contracts;
using ChillSharp.Schema;
using ChillSharp.Schema.Contracts;

namespace ChillSharp.Mcp;

/// <summary>
/// Builds MCP resource descriptors from Chill schema and entity-option metadata.
/// </summary>
public sealed class ChillMcpService : IChillMcpService
{
    private readonly IChillContext _context;
    private readonly IChillSchemaService _schemaService;

    public ChillMcpService(IChillContext context, IChillSchemaService schemaService)
    {
        _context = context;
        _schemaService = schemaService;
    }

    public async Task<IReadOnlyList<ChillMcpResource>> GetResourcesAsync(string? cultureName = null, CancellationToken cancellationToken = default)
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

        var candidates = entityItems
            .Concat(queryItems)
            .OrderBy(x => x.Type, StringComparer.Ordinal)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ChillType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var resources = new List<ChillMcpResource>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var resource = await BuildResourceAsync(candidate, cultureName, cancellationToken);
            if (resource != null)
            {
                resources.Add(resource);
            }
        }

        return resources;
    }

    public async Task<ChillMcpResource?> GetResourceAsync(string chillType, string? cultureName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chillType))
        {
            throw new ArgumentException("ChillType is required.", nameof(chillType));
        }

        var normalizedChillType = NormalizeRequiredChillType(chillType);
        return (await GetResourcesAsync(cultureName, cancellationToken))
            .FirstOrDefault(x => string.Equals(x.ChillType, normalizedChillType, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ChillMcpResource?> BuildResourceAsync(ChillDtoSchemaListItem item, string? cultureName, CancellationToken cancellationToken)
    {
        var schema = await _schemaService.GetSchemaAsync(item.ChillType, "default", cultureName, cancellationToken);
        if (schema == null)
        {
            return null;
        }

        var entityOptions = await _schemaService.GetEntityOptionsAsync(item.ChillType, cancellationToken);
        var isEnabled = entityOptions.EnableMCP || schema.EnableMCP;
        if (!isEnabled)
        {
            return null;
        }

        var description = FirstNonEmpty(entityOptions.MCPDescription, schema.MCPDescription, schema.DisplayName);
        return new ChillMcpResource
        {
            Uri = BuildResourceUri(item),
            Name = item.Name,
            ChillType = item.ChillType,
            ResourceType = item.Type,
            Description = description,
            ViewCode = schema.ChillViewCode,
            MimeType = "application/json",
            QueryRelatedChillType = schema.QueryRelatedChillType ?? string.Empty,
            Properties = schema.Properties.Select(BuildProperty).ToList()
        };
    }

    private static ChillMcpResourceProperty BuildProperty(ChillDtoPropertySchema property)
    {
        return new ChillMcpResourceProperty
        {
            Name = property.Name,
            DisplayName = property.DisplayName,
            Description = FirstNonEmpty(property.MCPDescription, property.DisplayName),
            PropertyType = property.PropertyType.ToString(),
            ReferenceChillType = property.ReferenceChillType
        };
    }

    private static string BuildResourceUri(ChillDtoSchemaListItem item)
    {
        return $"chill://{item.Type}/{Uri.EscapeDataString(item.ChillType)}";
    }

    private string NormalizeRequiredChillType(string chillType)
    {
        var resolvedType = ChillTypeResolver.ResolveType(_context.GetType().Assembly, chillType, _context.GetChillTypePrefix());
        return ChillTypeResolver.NormalizeChillType(resolvedType, _context.GetChillTypePrefix());
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
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
