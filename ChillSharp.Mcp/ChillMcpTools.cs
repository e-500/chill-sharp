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
using System.Text.Json;
using ChillSharp.Dto;
using ChillSharp.EF;
using ChillSharp.Schema.Contracts;
using ModelContextProtocol.Server;

namespace ChillSharp.Mcp;

/// <summary>
/// MCP tools exposing ChillSharp schema discovery, query execution, and DTO operations.
/// </summary>
[McpServerToolType]
public sealed class ChillMcpTools
{
    private const int MaxLoggedDtoQueryLength = 1024;

    private static readonly JsonSerializerOptions LogJsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions ExampleJsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string DtoExampleStructure = JsonSerializer.Serialize(new
    {
        ChillDtoQuery = new ChillDtoQuery
        {
            ChillType = "Query.PostQuery",
            Properties =
            {
                ["FullTextSearch"] = "search terms",
                ["Blog"] = new ChillDtoEntity
                {
                    ChillType = "Model.Blog",
                    Guid = Guid.Parse("11111111-1111-1111-1111-111111111111")
                }
            },
            ResultProperties =
            [
                new ChillDtoProperty("Guid"),
                new ChillDtoProperty("Title"),
                new ChillDtoProperty("Blog",
                [
                    new ChillDtoProperty("Guid"),
                    new ChillDtoProperty("Title")
                ])
            ],
            Pagination = new ChillPagination
            {
                Page = 1,
                PageResults = 20
            },
            Ordering = new ChillOrdering
            {
                PropertyName = "Title",
                Direction = ChillOrdering.AscendingDirection
            },
            Results =
            [
                new ChillDtoEntity
                {
                    Guid = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Position = 1,
                    ChillType = "Model.Post",
                    Label = "Example post",
                    ShortLabel = "Example",
                    Properties =
                    {
                        ["Title"] = "Example post",
                        ["Blog"] = new ChillDtoEntity
                        {
                            ChillType = "Model.Blog",
                            Guid = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                            Label = "Example blog"
                        }
                    }
                }
            ]
        },
        ChillDtoEntity = new ChillDtoEntity
        {
            Guid = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Position = 1,
            ChillType = "Model.Post",
            Label = "Example post",
            ShortLabel = "Example",
            Properties =
            {
                ["Title"] = "Example post",
                ["Author"] = "Ada",
                ["Blog"] = new ChillDtoEntity
                {
                    ChillType = "Model.Blog",
                    Guid = Guid.Parse("11111111-1111-1111-1111-111111111111")
                }
            }
        }
    }, ExampleJsonSerializerOptions);

    private const string AuthenticationAndPermissionsNotice =
        "Authentication with a bearer token is required by the host API. Permissions and other limitations can be applied through the authenticated API-key user, so tool results may be filtered or denied based on that identity.";

    private const string RequestPayloadGuidance =
        "Do not invent request objects or property names. First inspect the target schema with 'ChillSharp get-schema', then build payloads from that schema only. " +
        "Call 'ChillSharp get-dto-examples' when you need a concrete serialized ChillDtoQuery and ChillDtoEntity example structure. " +
        "ChillDtoQuery payload properties are ChillType, Properties, ResultProperties, Pagination, Ordering, and Results; Pagination contains Page and PageResults; Ordering contains PropertyName and Direction. " +
        "ChillDtoEntity payload properties are Guid, Position, ChillType, Label, ShortLabel, and Properties. " +
        "For Properties, use exact schema property names and values matching each property's simplePropertyType: guid, int, decimal, date, time, datetime, duration, bool, string, text, json, chill-entity, chill-entity-collection, or chill-query. " +
        "For query Properties, read each property's mcpDescription to infer search behavior such as equals, contains, range, lookup, or custom matching; when mcpDescription is missing or does not specify matching behavior, assume exact-match equals. " +
        "Every Chill query also supports a FullTextSearch property for generic full-text search; use Properties.FullTextSearch when the user asks for broad keyword search instead of a specific structured filter. " +
        "For chill-entity values, send a ChillDtoEntity reference with ChillType and Guid. For ResultProperties, send ChillDtoProperty objects with PropertyName and optional SubProperties using property names from the returned entity schema. ";

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
        "Each property includes propertyType as a stable numeric id and simplePropertyType as an agent-friendly string to use when constructing request payloads. " +
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

    [McpServerTool(Name = "ChillSharp get-dto-examples"), Description(
        "Returns a static serialized JSON object showing example ChillDtoQuery and ChillDtoEntity payload structures. " +
        "Use this tool when constructing MCP requests that need exact DTO property names, including ResultProperties with PropertyName and SubProperties, Pagination with Page and PageResults, and Ordering with PropertyName and Direction.")]
    public string GetDtoExamples()
    {
        return DtoExampleStructure;
    }

    [McpServerTool(Name = "ChillSharp query"), Description(
        "Executes a ChillSharp query for an MCP-enabled query schema and returns the ChillDtoQuery payload populated with results. " +
        "Before calling this tool, inspect the target query schema with 'ChillSharp get-schema' to understand accepted input properties, available result properties, and the returned entity type. " +
        "Use a query ChillType such as 'Query.PostQuery', provide input values in Properties, use Properties.FullTextSearch for generic keyword search, and optionally restrict returned fields through ResultProperties, Pagination, and Ordering on the ChillDtoQuery request. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillDtoQuery> Query(
        [Description("The full ChillSharp query payload. ChillType should usually be a query type such as 'Query.PostQuery'.")]
        ChillDtoQuery query,
        CancellationToken cancellationToken = default)
    {
        // DEBUG
        //var logId = Guid.NewGuid().ToString("N");
        //LogMcpQueryPayload(logId, "request", query);

        if (!await _schemaDiscoveryService.IsMcpEnabledAsync(query.ChillType, "default", cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException($"ChillSharp query '{query.ChillType}' is not MCP-enabled.");
        }

        return _dtoEngine.Query(query);

        // DEBUG
        //var response = _dtoEngine.Query(query);
        //LogMcpQueryPayload(logId, "response", response);
        //return response;
    }

    [McpServerTool(Name = "ChillSharp lookup"), Description(
        "Executes a generic full-text lookup against an MCP-enabled entity schema and returns a ChillDtoQuery payload populated with matching entities. " +
        "Use an entity ChillType such as 'Model.Blog', provide the search text in Properties.FullTextSearch, and optionally restrict returned fields through ResultProperties, Pagination, and Ordering. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillDtoQuery> Lookup(
        [Description("The lookup payload. ChillType should be an MCP-enabled entity type such as 'Model.Blog'.")]
        ChillDtoQuery query,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(query.ChillType, isQueryType: false, cancellationToken);
        return _dtoEngine.Lookup(query);
    }

    [McpServerTool(Name = "ChillSharp find"), Description(
        "Finds one MCP-enabled ChillSharp entity by ChillType and Guid and returns the matching ChillDtoEntity, or null when no record exists. " +
        "Use 'ChillSharp get-schema' first to understand the entity shape before reading or mutating it. " +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillDtoEntity?> Find(
        [Description("The entity identifier payload. ChillType must be an MCP-enabled entity type and Guid must identify the record to find.")]
        ChillDtoEntity entity,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(entity.ChillType, isQueryType: false, cancellationToken);
        return _dtoEngine.Find(entity);
    }

    [McpServerTool(Name = "ChillSharp create"), Description(
        "Creates a new MCP-enabled ChillSharp entity from a ChillDtoEntity payload and returns the persisted DTO. " +
        "Inspect the target entity schema first so required and meaningful Properties are supplied correctly. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillDtoEntity> Create(
        [Description("The entity payload to create. ChillType must be an MCP-enabled entity type; Properties contains values for annotated fields.")]
        ChillDtoEntity entity,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(entity.ChillType, isQueryType: false, cancellationToken);
        return _dtoEngine.Create(entity);
    }

    [McpServerTool(Name = "ChillSharp update"), Description(
        "Updates an existing MCP-enabled ChillSharp entity from a ChillDtoEntity payload and returns the updated DTO. " +
        "Guid must identify an existing record and Properties should contain the fields to update. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillDtoEntity> Update(
        [Description("The entity payload to update. ChillType must be an MCP-enabled entity type and Guid must identify the existing record.")]
        ChillDtoEntity entity,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(entity.ChillType, isQueryType: false, cancellationToken);
        return _dtoEngine.Update(entity);
    }

    [McpServerTool(Name = "ChillSharp delete"), Description(
        "Deletes an existing MCP-enabled ChillSharp entity identified by ChillType and Guid. " +
        "This is a mutating operation; inspect schema and confirm the target record with 'ChillSharp find' before deleting. " +
        AuthenticationAndPermissionsNotice)]
    public async Task Delete(
        [Description("The entity identifier payload. ChillType must be an MCP-enabled entity type and Guid must identify the existing record to delete.")]
        ChillDtoEntity entity,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(entity.ChillType, isQueryType: false, cancellationToken);
        _dtoEngine.Delete(entity);
    }

    [McpServerTool(Name = "ChillSharp autocomplete-entity"), Description(
        "Applies ChillSharp autocomplete logic to an MCP-enabled entity DTO without explicitly choosing create or update. " +
        "Use this before create or update when the model calculates labels, URLs, references, or other derived values. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillDtoEntity> AutocompleteEntity(
        [Description("The entity payload to autocomplete. ChillType must be an MCP-enabled entity type.")]
        ChillDtoEntity entity,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(entity.ChillType, isQueryType: false, cancellationToken);
        return _dtoEngine.Autocomplete(entity);
    }

    [McpServerTool(Name = "ChillSharp autocomplete-query"), Description(
        "Applies ChillSharp autocomplete logic to an MCP-enabled query DTO without executing the query. " +
        "Use this when query inputs have dependent or calculated values. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillDtoQuery> AutocompleteQuery(
        [Description("The query payload to autocomplete. ChillType must be an MCP-enabled query type.")]
        ChillDtoQuery query,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(query.ChillType, isQueryType: true, cancellationToken);
        return _dtoEngine.Autocomplete(query);
    }

    [McpServerTool(Name = "ChillSharp validate-entity"), Description(
        "Validates an MCP-enabled entity DTO and returns ChillSharp validation errors without persisting changes. " +
        "Use this before create or update when the host model exposes validation rules. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<IEnumerable<ChillValidationError>> ValidateEntity(
        [Description("The entity payload to validate. ChillType must be an MCP-enabled entity type.")]
        ChillDtoEntity entity,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(entity.ChillType, isQueryType: false, cancellationToken);
        return _dtoEngine.Validate(entity);
    }

    [McpServerTool(Name = "ChillSharp validate-query"), Description(
        "Validates an MCP-enabled query DTO and returns ChillSharp validation errors without executing the query. " +
        "Use this before query execution when the query type exposes validation rules. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<IEnumerable<ChillValidationError>> ValidateQuery(
        [Description("The query payload to validate. ChillType must be an MCP-enabled query type.")]
        ChillDtoQuery query,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(query.ChillType, isQueryType: true, cancellationToken);
        return _dtoEngine.Validate(query);
    }

    [McpServerTool(Name = "ChillSharp chunk"), Description(
        "Executes a list of ChillOperation items against MCP-enabled ChillSharp schemas and returns the updated operation list. " +
        "Supported verbs are transaction, query, find, create, update, delete, autocomplete, validate, and commit. " +
        "Each operation is checked for MCP visibility before any operation is executed, so unpublished schemas are rejected for the whole chunk. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<List<ChillOperation>> Chunk(
        [Description("Ordered ChillOperation items. Use Index for client-side ordering and Verb to choose the operation; provide Query or Entity according to the verb.")]
        List<ChillOperation> chunk,
        CancellationToken cancellationToken = default)
    {
        foreach (var operation in chunk)
        {
            await EnsureChunkOperationMcpEnabledAsync(operation, cancellationToken);
        }

        chunk.ForEach(operation => operation.Execute(_dtoEngine));
        return chunk;
    }

    private async Task EnsureMcpEnabledAsync(string chillType, bool isQueryType, CancellationToken cancellationToken)
    {
        if (!await _schemaDiscoveryService.IsMcpEnabledAsync(chillType, "default", cancellationToken: cancellationToken))
        {
            var schemaKind = isQueryType ? "query" : "entity";
            throw new InvalidOperationException($"ChillSharp {schemaKind} '{chillType}' is not MCP-enabled.");
        }
    }

    private async Task EnsureChunkOperationMcpEnabledAsync(ChillOperation operation, CancellationToken cancellationToken)
    {
        switch (operation.Verb?.ToLowerInvariant())
        {
            case ChillOperationVerb.QUERY when operation.Query != null:
                await EnsureMcpEnabledAsync(operation.Query.ChillType, isQueryType: true, cancellationToken);
                break;
            case ChillOperationVerb.FIND when operation.Entity != null:
            case ChillOperationVerb.CREATE when operation.Entity != null:
            case ChillOperationVerb.UPDATE when operation.Entity != null:
            case ChillOperationVerb.DELETE when operation.Entity != null:
                await EnsureMcpEnabledAsync(operation.Entity.ChillType, isQueryType: false, cancellationToken);
                break;
            case ChillOperationVerb.AUTOCOMPLETE when operation.Query != null:
            case ChillOperationVerb.VALIDATE when operation.Query != null:
                await EnsureMcpEnabledAsync(operation.Query.ChillType, isQueryType: true, cancellationToken);
                break;
            case ChillOperationVerb.AUTOCOMPLETE when operation.Entity != null:
            case ChillOperationVerb.VALIDATE when operation.Entity != null:
                await EnsureMcpEnabledAsync(operation.Entity.ChillType, isQueryType: false, cancellationToken);
                break;
        }
    }

    private static void LogMcpQueryPayload(string logId, string direction, ChillDtoQuery query)
    {
        Console.WriteLine($"ChillSharp MCP query {direction} [{logId}]");
        Console.WriteLine(TruncateSerializedPayload(JsonSerializer.Serialize(query, LogJsonSerializerOptions)));
    }

    private static string TruncateSerializedPayload(string serializedPayload)
    {
        if (serializedPayload.Length <= MaxLoggedDtoQueryLength)
        {
            return serializedPayload;
        }

        const string truncationMarker = "...";
        return string.Concat(
            serializedPayload.AsSpan(0, MaxLoggedDtoQueryLength - truncationMarker.Length),
            truncationMarker);
    }
}
