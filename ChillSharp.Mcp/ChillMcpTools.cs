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

using System.ComponentModel;
using ChillSharp.Dto;
using ChillSharp.Schema.Contracts;
using ModelContextProtocol.Server;

namespace ChillSharp.Mcp;

/// <summary>
/// MCP tools exposing ChillSharp schema discovery and query execution.
/// </summary>
[McpServerToolType]
public sealed class ChillMcpTools
{
    private const string AuthenticationAndPermissionsNotice =
        "Authentication with a bearer token is required by the host API. Permissions and other limitations can be applied through the authenticated API-key user, so tool results may be filtered or denied based on that identity.";

    private readonly ChillMcpSchemaDiscoveryService _schemaDiscoveryService;
    private readonly IChillDtoEngine _dtoEngine;

    public ChillMcpTools(ChillMcpSchemaDiscoveryService schemaDiscoveryService, IChillDtoEngine dtoEngine)
    {
        _schemaDiscoveryService = schemaDiscoveryService;
        _dtoEngine = dtoEngine;
    }

    [McpServerTool(Name = "ChillSharp get-schema-list"), Description(
        "Lists all MCP-enabled ChillSharp entity and query schemas available to the authenticated caller. " +
        "Use this first to discover the database structure entry points, then call 'ChillSharp get-schema' for the full shape of a specific entity or query. " +
        "Schema entries describe entities, queries, their properties, and returned types. " +
        AuthenticationAndPermissionsNotice)]
    public Task<IReadOnlyList<ChillDtoSchemaListItem>> GetSchemaList(
        [Description("Optional culture name used to localize schema labels, for example 'en-GB' or 'it-IT'.")]
        string? cultureName = null,
        CancellationToken cancellationToken = default)
    {
        return _schemaDiscoveryService.GetSchemaListAsync(cultureName, cancellationToken);
    }

    [McpServerTool(Name = "ChillSharp get-schema"), Description(
        "Returns the full ChillSharp schema for one MCP-enabled entity or query type. " +
        "Use this tool to understand the structure of the database before querying: schemas describe entities, query types, their properties, descriptions, reference types, and for query schemas the related returned entity type. " +
        "This is the best tool for learning which fields exist and how a query is expected to behave. " +
        AuthenticationAndPermissionsNotice)]
    public Task<ChillDtoSchema?> GetSchemaAsync(
        [Description("The ChillSharp entity or query type, for example 'Model.Blog' or 'Query.PostQuery'.")]
        string chillType,
        [Description("Optional view code. Use 'default' unless the host exposes a custom schema view.")]
        string chillViewCode = "default",
        [Description("Optional culture name used to localize schema labels, for example 'en-GB' or 'it-IT'.")]
        string? cultureName = null,
        CancellationToken cancellationToken = default)
    {
        return _schemaDiscoveryService.GetSchemaAsync(chillType, chillViewCode, cultureName, cancellationToken);
    }

    [McpServerTool(Name = "ChillSharp query"), Description(
        "Executes a ChillSharp query for an MCP-enabled query schema and returns the ChillDtoQuery payload populated with results. " +
        "Before calling this tool, inspect the target query schema with 'ChillSharp get-schema' to understand accepted input properties, available result properties, and the returned entity type. " +
        "Use a query ChillType such as 'Query.PostQuery', provide input values in Properties, and optionally restrict returned fields through ResultProperties, Pagination, and Ordering on the ChillDtoQuery request. " +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillDtoQuery> Query(
        [Description("The full ChillSharp query payload. ChillType should usually be a query type such as 'Query.PostQuery'.")]
        ChillDtoQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!await _schemaDiscoveryService.IsMcpEnabledAsync(query.ChillType, "default", cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException($"ChillSharp query '{query.ChillType}' is not MCP-enabled.");
        }

        return _dtoEngine.Query(query);
    }
}
