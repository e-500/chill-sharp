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
using ChillSharp.Mcp.Contracts;
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

    private static readonly string McpExampleStructure = JsonSerializer.Serialize(new
    {
        ChillMcpQuery = new ChillMcpQuery
        {
            ChillType = "Query.PostQuery",
            Properties =
            {
                ["FullTextSearch"] = "\"example phrase\"",
                ["Blog"] = new ChillMcpEntity
                {
                    ChillType = "Model.Blog",
                    Guid = Guid.Parse("11111111-1111-1111-1111-111111111111")
                }
            },
            ResultProperties =
            [
                new ChillMcpProperty { PropertyName = "Guid" },
                new ChillMcpProperty { PropertyName = "Title" },
                new ChillMcpProperty
                {
                    PropertyName = "Blog",
                    SubProperties =
                    [
                        new ChillMcpProperty { PropertyName = "Guid" },
                        new ChillMcpProperty { PropertyName = "Title" }
                    ]
                }
            ],
            Pagination = new ChillMcpPagination
            {
                Page = 1,
                PageResults = 20
            },
            Ordering = new ChillMcpOrdering
            {
                PropertyName = "Title",
                Direction = ChillOrdering.AscendingDirection
            },
            Results =
            [
                new ChillMcpEntity
                {
                    Guid = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Position = 1,
                    ChillType = "Model.Post",
                    Label = "Example post",
                    ShortLabel = "Example",
                    Properties =
                    {
                        ["Title"] = "Example post",
                        ["Blog"] = new ChillMcpEntity
                        {
                            ChillType = "Model.Blog",
                            Guid = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                            Label = "Example blog"
                        }
                    }
                }
            ]
        },
        ChillMcpEntity = new ChillMcpEntity
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
                ["Blog"] = new ChillMcpEntity
                {
                    ChillType = "Model.Blog",
                    Guid = Guid.Parse("11111111-1111-1111-1111-111111111111")
                }
            }
        }
    }, ExampleJsonSerializerOptions);

    private const string AuthenticationAndPermissionsNotice =
        "Authentication with a bearer token is required by the host API. Permissions and other limitations can be applied through the authenticated API-key user, so tool results may be filtered or denied based on that identity.";

    private const string FullTextSearchGuidance =
        "FullTextSearch supports plain AND-matched terms, quoted phrases, grouped AND/OR expressions, and leading or trailing wildcards inside quoted phrases.";

    private const string RequestPayloadGuidance =
        "Inspect the schema first, use exact schema property names, and match each property's simplePropertyType. " +
        "Use Properties.FullTextSearch for broad keyword search. " + FullTextSearchGuidance;

    private readonly ChillMcpSchemaDiscoveryService _schemaDiscoveryService;
    private readonly IChillDtoEngine _dtoEngine;

    public ChillMcpTools(ChillMcpSchemaDiscoveryService schemaDiscoveryService, IChillDtoEngine dtoEngine)
    {
        _schemaDiscoveryService = schemaDiscoveryService;
        _dtoEngine = dtoEngine;
    }

    [McpServerTool(Name = "ChillSharp.get-schema-list", UseStructuredContent = true), Description(
        "Lists all MCP-enabled ChillSharp entity and query schemas available to the authenticated caller. " +
        "Use this first to discover the database structure entry points, then call 'ChillSharp get-schema' for the full shape of a specific entity or query. " +
        "Schema entries describe entities, queries, their properties, and returned types. " +
        AuthenticationAndPermissionsNotice)]
    public Task<IReadOnlyList<ChillMcpSchemaListItem>> GetSchemaList(
        [Description("Optional culture name used to localize schema labels, for example 'en-GB' or 'it-IT'.")]
        string? cultureName = null,
        CancellationToken cancellationToken = default)
    {
        return _schemaDiscoveryService.GetSchemaListAsync(cultureName, cancellationToken);
    }

    [McpServerTool(Name = "ChillSharp.get-schema", UseStructuredContent = true), Description(
        "Returns the full ChillSharp schema for one MCP-enabled entity or query type. " +
        "Use this tool to understand the structure of the database before querying: schemas describe entities, query types, their properties, descriptions, reference types, and for query schemas the related returned entity type. " +
        "Each property includes propertyType as a stable numeric id and simplePropertyType as an agent-friendly string to use when constructing request payloads. " +
        "This is the best tool for learning which fields exist and how a query is expected to behave. " +
        AuthenticationAndPermissionsNotice)]
    public Task<ChillMcpSchema?> GetSchemaAsync(
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

    [McpServerTool(Name = "ChillSharp.get-dto-examples", UseStructuredContent = true), Description(
        "Returns a static serialized JSON object showing example MCP query and entity payload structures. " +
        "Use this tool when constructing MCP requests that need exact DTO property names, including ResultProperties with PropertyName and SubProperties, Pagination with Page and PageResults, and Ordering with PropertyName and Direction.")]
    public string GetDtoExamples()
    {
        return McpExampleStructure;
    }

    [McpServerTool(Name = "ChillSharp.query", UseStructuredContent = true), Description(
        "Executes either a registered ChillSharp query or an automatic entity query for an MCP-enabled resource and returns the MCP query payload populated with results. " +
        "For a registered query, inspect its query schema. For AutomaticQuery, inspect the target entity schema and use its ChillType. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillMcpQuery> Query(
        [Description("The query payload. Use a query ChillType such as 'Query.PostQuery' normally, or an entity ChillType such as 'Model.Post' when AutomaticQuery is present.")]
        ChillMcpQuery query,
        CancellationToken cancellationToken = default)
    {
        // DEBUG
        //var logId = Guid.NewGuid().ToString("N");
        //LogMcpQueryPayload(logId, "request", query);

        var dto = query.ToDto();
        await EnsureMcpEnabledAsync(
            dto.ChillType,
            isQueryType: dto.AutomaticQuery == null,
            cancellationToken);

        return ChillMcpQuery.FromDto(_dtoEngine.Query(dto));

        // DEBUG
        //var response = _dtoEngine.Query(query);
        //LogMcpQueryPayload(logId, "response", response);
        //return response;
    }

    [McpServerTool(Name = "ChillSharp.lookup", UseStructuredContent = true), Description(
        "Executes a generic full-text lookup against an MCP-enabled entity schema and returns an MCP query payload populated with matching entities. " +
        "Use an entity ChillType such as 'Model.Blog', provide keyword or quoted phrase search text in Properties.FullTextSearch, and optionally restrict returned fields through ResultProperties, Pagination, and Ordering. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillMcpQuery> Lookup(
        [Description("The lookup payload. ChillType should be an MCP-enabled entity type such as 'Model.Blog'.")]
        ChillMcpQuery query,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(query.ChillType, isQueryType: false, cancellationToken);
        return ChillMcpQuery.FromDto(_dtoEngine.Lookup(query.ToDto()));
    }

    [McpServerTool(Name = "ChillSharp.find", UseStructuredContent = true), Description(
        "Finds one MCP-enabled ChillSharp entity by ChillType and Guid and returns the matching MCP entity, or null when no record exists. " +
        "Use 'ChillSharp get-schema' first to understand the entity shape before reading or mutating it. " +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillMcpEntity?> Find(
        [Description("The entity identifier payload. ChillType must be an MCP-enabled entity type and Guid must identify the record to find.")]
        ChillMcpEntity entity,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(entity.ChillType, isQueryType: false, cancellationToken);
        var found = _dtoEngine.Find(entity.ToDto());
        return found == null ? null : ChillMcpEntity.FromDto(found);
    }

    [McpServerTool(Name = "ChillSharp.create", UseStructuredContent = true), Description(
        "Creates a new MCP-enabled ChillSharp entity from an MCP entity payload and returns the persisted entity. " +
        "Inspect the target entity schema first so required and meaningful Properties are supplied correctly. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillMcpEntity> Create(
        [Description("The entity payload to create. ChillType must be an MCP-enabled entity type; Properties contains values for annotated fields.")]
        ChillMcpEntity entity,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(entity.ChillType, isQueryType: false, cancellationToken);
        return ChillMcpEntity.FromDto(_dtoEngine.Create(entity.ToDto()));
    }

    [McpServerTool(Name = "ChillSharp.update", UseStructuredContent = true), Description(
        "Updates an existing MCP-enabled ChillSharp entity from an MCP entity payload and returns the updated entity. " +
        "Guid must identify an existing record and Properties should contain the fields to update. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillMcpEntity> Update(
        [Description("The entity payload to update. ChillType must be an MCP-enabled entity type and Guid must identify the existing record.")]
        ChillMcpEntity entity,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(entity.ChillType, isQueryType: false, cancellationToken);
        return ChillMcpEntity.FromDto(_dtoEngine.Update(entity.ToDto()));
    }

    [McpServerTool(Name = "ChillSharp.delete", UseStructuredContent = true), Description(
        "Deletes an existing MCP-enabled ChillSharp entity identified by ChillType and Guid. " +
        "This is a mutating operation; inspect schema and confirm the target record with 'ChillSharp find' before deleting. " +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillMcpEmptyResult> Delete(
        [Description("The entity identifier payload. ChillType must be an MCP-enabled entity type and Guid must identify the existing record to delete.")]
        ChillMcpEntity entity,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(entity.ChillType, isQueryType: false, cancellationToken);
        _dtoEngine.Delete(entity.ToDto());
        return new ChillMcpEmptyResult();
    }

    [McpServerTool(Name = "ChillSharp.autocomplete-entity", UseStructuredContent = true), Description(
        "Applies ChillSharp autocomplete logic to an MCP-enabled entity DTO without explicitly choosing create or update. " +
        "Use this before create or update when the model calculates labels, URLs, references, or other derived values. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillMcpEntity> AutocompleteEntity(
        [Description("The entity payload to autocomplete. ChillType must be an MCP-enabled entity type.")]
        ChillMcpEntity entity,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(entity.ChillType, isQueryType: false, cancellationToken);
        return ChillMcpEntity.FromDto(_dtoEngine.Autocomplete(entity.ToDto()));
    }

    [McpServerTool(Name = "ChillSharp.autocomplete-query", UseStructuredContent = true), Description(
        "Applies ChillSharp autocomplete logic to an MCP-enabled query DTO without executing the query. " +
        "Use this when query inputs have dependent or calculated values. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<ChillMcpQuery> AutocompleteQuery(
        [Description("The query payload to autocomplete. ChillType must be an MCP-enabled query type.")]
        ChillMcpQuery query,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(query.ChillType, isQueryType: true, cancellationToken);
        return ChillMcpQuery.FromDto(_dtoEngine.Autocomplete(query.ToDto()));
    }

    [McpServerTool(Name = "ChillSharp.validate-entity", UseStructuredContent = true), Description(
        "Validates an MCP-enabled entity DTO and returns ChillSharp validation errors without persisting changes. " +
        "Use this before create or update when the host model exposes validation rules. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<IEnumerable<ChillMcpValidationError>> ValidateEntity(
        [Description("The entity payload to validate. ChillType must be an MCP-enabled entity type.")]
        ChillMcpEntity entity,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(entity.ChillType, isQueryType: false, cancellationToken);
        return _dtoEngine.Validate(entity.ToDto()).Select(ChillMcpValidationError.FromDto);
    }

    [McpServerTool(Name = "ChillSharp.validate-query", UseStructuredContent = true), Description(
        "Validates an MCP-enabled query DTO and returns ChillSharp validation errors without executing the query. " +
        "Use this before query execution when the query type exposes validation rules. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<IEnumerable<ChillMcpValidationError>> ValidateQuery(
        [Description("The query payload to validate. ChillType must be an MCP-enabled query type.")]
        ChillMcpQuery query,
        CancellationToken cancellationToken = default)
    {
        await EnsureMcpEnabledAsync(query.ChillType, isQueryType: true, cancellationToken);
        return _dtoEngine.Validate(query.ToDto()).Select(ChillMcpValidationError.FromDto);
    }

    [McpServerTool(Name = "ChillSharp.chunk", UseStructuredContent = true), Description(
        "Executes a list of MCP operation items against MCP-enabled ChillSharp schemas and returns the updated operation list. " +
        "Supported verbs are transaction, query, find, create, update, delete, autocomplete, validate, and commit. " +
        "Each operation is checked for MCP visibility before any operation is executed, so unpublished schemas are rejected for the whole chunk. " +
        RequestPayloadGuidance +
        AuthenticationAndPermissionsNotice)]
    public async Task<List<ChillMcpOperation>> Chunk(
        [Description("Ordered operation items. Use Index for client-side ordering and Verb to choose the operation; provide Query or Entity according to the verb.")]
        List<ChillMcpOperation> chunk,
        CancellationToken cancellationToken = default)
    {
        var dtoChunk = chunk.Select(operation => operation.ToDto()).ToList();
        foreach (var operation in dtoChunk)
        {
            await EnsureChunkOperationMcpEnabledAsync(operation, cancellationToken);
        }

        dtoChunk.ForEach(operation => operation.Execute(_dtoEngine));
        return dtoChunk.Select(ChillMcpOperation.FromDto).ToList();
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
                await EnsureMcpEnabledAsync(
                    operation.Query.ChillType,
                    isQueryType: operation.Query.AutomaticQuery == null,
                    cancellationToken);
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
