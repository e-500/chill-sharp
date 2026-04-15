# ChillSharp.Mcp

`ChillSharp.Mcp` exposes a Model Context Protocol server for ChillSharp applications by using the official MCP C# SDK.

The module is enabled by default when `EnableMcpApi` stays `true`. You can disable it globally through `AddChillApi(..., options => options.EnableMcpApi = false)` or directly on the module with `AddChillMcpApi<TContext>(options => options.Enabled = false)`.

## Registered tools

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

## Authentication and permissions

The host API is expected to require bearer authentication.

Permissions and other limitations can be applied to the authenticated API-key user, so the visible schemas and query results may be filtered or denied according to that user.
