# ChillSharp.Mcp

`ChillSharp.Mcp` exposes a Model Context Protocol server for ChillSharp applications by using the official MCP C# SDK.

The module is enabled by default when `EnableMcpApi` stays `true`. You can disable it globally through `AddChillApi(..., options => options.EnableMcpApi = false)` or directly on the module with `AddChillMcpApi<TContext>(options => options.Enabled = false)`.

## Registered tools

The module registers these tools:

- `ChillSharp get-schema-list`
- `ChillSharp get-schema`
- `ChillSharp query`
- `ChillSharp lookup`
- `ChillSharp find`
- `ChillSharp create`
- `ChillSharp update`
- `ChillSharp delete`
- `ChillSharp autocomplete-entity`
- `ChillSharp autocomplete-query`
- `ChillSharp validate-entity`
- `ChillSharp validate-query`
- `ChillSharp chunk`

### `ChillSharp get-schema-list`

Lists the available MCP-enabled ChillSharp entity and query schemas visible to the authenticated caller.

Use this tool first to discover the available database structure entry points, then call `ChillSharp get-schema` for the full schema of the entity or query you want to inspect.

### `ChillSharp get-schema`

Returns the full schema for one MCP-enabled ChillSharp entity or query type.

Use this tool to understand the structure of the database and the available query surface. Schemas contain descriptions of queries and entities, their own properties, reference types, and returned types. Query schemas also describe the related entity type returned by the query.

### `ChillSharp query`

Executes the ChillSharp query endpoint through MCP for MCP-enabled query types.

Use `ChillSharp get-schema` first so you know which query properties are accepted, which result properties are available, and which entity type is returned. Then send a `ChillDtoQuery` payload with:

- `ChillType` set to a query type such as `Query.PostQuery`
- `Properties` containing the input filters or parameters
- optional `ResultProperties`, `Pagination`, and `Ordering`

Only schemas with `EnableMCP` enabled, either directly in the schema or through runtime entity options, are exposed by these MCP tools.

### `ChillSharp lookup`

Executes a generic full-text lookup against an MCP-enabled entity type.

Use a `ChillDtoQuery` payload with:

- `ChillType` set to an entity type such as `Model.Blog`
- `Properties.FullTextSearch` containing the search text
- optional `ResultProperties`, `Pagination`, and `Ordering`

### `ChillSharp find`

Finds a single MCP-enabled entity by `ChillType` and `Guid`.

Use a `ChillDtoEntity` payload with:

- `ChillType` set to an entity type such as `Model.Blog`
- `Guid` set to the record identifier

The tool returns a `ChillDtoEntity` when the record exists, or `null` when it does not.

### `ChillSharp create`

Creates a new MCP-enabled entity and returns the persisted `ChillDtoEntity`.

Use `ChillSharp get-schema` first, then send a `ChillDtoEntity` payload with:

- `ChillType` set to an entity type such as `Model.Blog`
- optional `Guid` when the client needs to choose the identifier
- `Properties` containing annotated field values

### `ChillSharp update`

Updates an existing MCP-enabled entity and returns the updated `ChillDtoEntity`.

Use a `ChillDtoEntity` payload with:

- `ChillType` set to an entity type such as `Model.Blog`
- `Guid` set to an existing record
- `Properties` containing the fields to update

### `ChillSharp delete`

Deletes an existing MCP-enabled entity identified by `ChillType` and `Guid`.

This is a mutating operation. A client should normally call `ChillSharp find` first to confirm the exact record before deletion.

### `ChillSharp autocomplete-entity`

Applies ChillSharp entity autocomplete logic without persisting changes.

Use it before `create` or `update` when the entity model calculates labels, URLs, references, or other derived values.

### `ChillSharp autocomplete-query`

Applies ChillSharp query autocomplete logic without executing the query.

Use it when query inputs have dependent or calculated values.

### `ChillSharp validate-entity`

Validates an MCP-enabled entity DTO and returns ChillSharp validation errors without persisting changes.

Use it before `create` or `update` when the host model exposes validation rules.

### `ChillSharp validate-query`

Validates an MCP-enabled query DTO and returns ChillSharp validation errors without executing the query.

Use it before `query` when the query type exposes validation rules.

### `ChillSharp chunk`

Executes a list of `ChillOperation` items and returns the updated operation list.

Supported verbs are:

- `transaction`
- `query`
- `find`
- `create`
- `update`
- `delete`
- `autocomplete`
- `validate`
- `commit`

Each operation is checked for MCP visibility before any operation executes. If one operation targets a non-MCP-enabled schema, the whole chunk is rejected.

For `query`, `autocomplete`, and `validate` operations that use a query payload, set `Query`. For entity operations, set `Entity`.

## Authentication and permissions

The host API is expected to require bearer authentication.

Permissions and other limitations can be applied to the authenticated API-key user, so the visible schemas and query results may be filtered or denied according to that user.
