---
name: chillsharp_mcp
description: Guidance on configuring, enabling, and designing models for ChillSharp Model Context Protocol (MCP) servers.
---

# ChillSharp MCP Integration

This skill guides you on setting up the Model Context Protocol (`ChillSharp.Mcp`) module, enabling MCP on models, and optimizing database models for AI agent consumption.

## 1. Registration in ASP.NET Core

The MCP module is registered automatically with `AddChillApi()` if `options.EnableMcpApi` is not disabled. 

Alternatively, register it directly in `Program.cs`:
```csharp
using ChillSharp.Mcp.Api;

builder.Services.AddChillMcpApi<AppDbContext>(options =>
{
    options.Enabled = true;
    options.RoutePattern = "/api/chill-mcp"; // Default endpoint for MCP clients
});
```

Context Requirements:
- DbContext must implement `IChillContext` and `IChillSchemaDbContext`.
- Include `modelBuilder.AddChillSchemaModel()` in `OnModelCreating`.

## 2. Exposing Schemas to MCP

To expose an entity or query schema to MCP, set `EnableMCP = true` in `[ChillEntity]`:
```csharp
[ChillEntity(
    UniquePropertyKeyString: "4E16F6C0-6B95-4D67-98BC-9F4D0D63EAF1",
    PrimaryLanguageLabel: "Invoice",
    SecondaryLanguageLabel: "Fattura",
    EnableMCP = true,
    MCPDescription = "Customer invoice header. Use it to inspect invoice identity, customer, dates, totals, and payment state.")]
public class Invoice : ChillEntity
{
    // ...
}
```

A query DTO is visible and executable through MCP only when its target returned entity has `EnableMCP = true`.

## 3. Best Practices for AI/Agent Optimization

- **MCPDescription**: Write detailed descriptions at both the entity and property levels (`MCPDescription` attribute property). Agents rely on this to build correct payloads and query filters.
- **Explain Filters**: In query property descriptions, explicitly describe search behaviors (e.g. contains-style text search, range boundary, exact match). If unspecified, agents assume exact matches.
- **Reference Types**: Declare relationships clearly using `ReferenceChillTypeQuery` and descriptions.
- **Keep Queries Focused**: Prefer specialized query types (e.g., `Query.OpenInvoicesQuery`) rather than general catch-all queries with dozens of optional inputs.
- **OAuth & Auth**: When using protected APIs, MCP endpoints expect bearer authorization (`Authorization: Bearer <access-token>`). The Identity auth module includes built-in OAuth PKCE flow support for ChatGPT/MCP clients.
