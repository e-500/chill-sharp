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

using ChillSharp.Dto;
using ChillSharp.EF;
using ChillSharp.Mcp.Contracts;
using ChillSharp.Schema;
using ChillSharp.Schema.Contracts;

namespace ChillSharp.Mcp;

/// <summary>
/// Provides schema discovery helpers used by the ChillSharp MCP tools.
/// </summary>
public sealed class ChillMcpSchemaDiscoveryService
{
    private readonly IChillContext _context;
    private readonly IChillSchemaService _schemaService;

    public ChillMcpSchemaDiscoveryService(IChillContext context, IChillSchemaService schemaService)
    {
        _context = context;
        _schemaService = schemaService;
    }

    public async Task<IReadOnlyList<ChillMcpSchemaListItem>> GetSchemaListAsync(
        string? cultureName = null,
        CancellationToken cancellationToken = default)
    {
        var assemblies = ChillAssemblyDiscovery.GetCandidateAssemblies(_context.GetType().Assembly);
        var shrinkTypePrefix = _context.GetChillTypePrefix();

        var entityItems = assemblies
            .SelectMany(ChillAssemblyDiscovery.GetLoadableTypes)
            .Where(IsRegisteredEntityType)
            .Select(type => ChillDtoSchemaListItem.FromEntityType(type, shrinkTypePrefix, _context, cultureName));

        var queryItems = assemblies
            .SelectMany(ChillAssemblyDiscovery.GetLoadableTypes)
            .Where(IsRegisteredQueryType)
            .Select(type => ChillDtoSchemaListItem.CreateFromQueryType(type, shrinkTypePrefix, _context, cultureName));

        var candidates = entityItems
            .Concat(queryItems)
            .OrderBy(x => x.Type, StringComparer.Ordinal)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ChillType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var results = new List<ChillMcpSchemaListItem>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var schema = await GetDtoSchemaAsync(candidate.ChillType, "default", cultureName, cancellationToken);
            if (schema != null)
            {
                results.Add(ChillMcpSchemaListItem.FromDto(candidate, schema.MCPDescription));
            }
        }

        return results;
    }

    public async Task<ChillMcpSchema?> GetSchemaAsync(
        string chillType,
        string chillViewCode = "default",
        string? cultureName = null,
        CancellationToken cancellationToken = default)
    {
        var schema = await GetDtoSchemaAsync(chillType, chillViewCode, cultureName, cancellationToken);
        return schema == null ? null : ChillMcpSchema.FromDto(schema);
    }

    private async Task<ChillDtoSchema?> GetDtoSchemaAsync(
        string chillType,
        string chillViewCode = "default",
        string? cultureName = null,
        CancellationToken cancellationToken = default)
    {
        var schema = await _schemaService.GetSchemaAsync(chillType, chillViewCode, cultureName, cancellationToken);
        if (schema == null)
        {
            return null;
        }

        return await IsMcpEnabledAsync(schema.ChillType, chillViewCode, cultureName, cancellationToken)
            ? schema
            : null;
    }

    public async Task<bool> IsMcpEnabledAsync(
        string chillType,
        string chillViewCode = "default",
        string? cultureName = null,
        CancellationToken cancellationToken = default)
    {
        var schema = await _schemaService.GetSchemaAsync(chillType, chillViewCode, cultureName, cancellationToken);
        if (schema == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(schema.QueryRelatedChillType))
        {
            return await IsEntityMcpEnabledAsync(schema.QueryRelatedChillType, cultureName, cancellationToken);
        }

        return await IsEntityMcpEnabledAsync(schema.ChillType, cultureName, cancellationToken);
    }

    private async Task<bool> IsEntityMcpEnabledAsync(
        string chillType,
        string? cultureName,
        CancellationToken cancellationToken)
    {
        var schema = await _schemaService.GetSchemaAsync(chillType, "default", cultureName, cancellationToken);
        if (schema == null || !string.IsNullOrWhiteSpace(schema.QueryRelatedChillType))
        {
            return false;
        }

        return schema.EnableMCP;
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
