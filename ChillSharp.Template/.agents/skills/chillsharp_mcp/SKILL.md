---
name: chillsharp_mcp
description: Guidance on configuring, enabling, and designing models for ChillSharp Model Context Protocol (MCP) servers.
---

# ChillSharp MCP Integration

This skill guides you on setting up the Model Context Protocol (`ChillSharp.Mcp`) module, enabling MCP on models, and optimizing database models for AI agent consumption.

## 1. Registration in ASP.NET Core

The MCP module is registered automatically with `AddChillApi()` in `Program.cs` since `options.EnableMcpApi = true;` is set.

Alternatively, you can register it or configure its route pattern explicitly:
```csharp
using ChillSharp.Mcp.Api;

builder.Services.AddChillMcpApi<ChillSharpTemplateContext>(options =>
{
    options.Enabled = true;
    options.RoutePattern = "/api/chill-mcp"; // Default endpoint for MCP clients
});
```

## 2. Exposing Schemas to MCP

To expose an entity or query schema to MCP, set `EnableMCP = true` in `[ChillEntity]`.
For example, the template's `Example` entity is exposed by default:

```csharp
[ChillEntity(
    UniquePropertyKeyString: "C65A0497-8D09-4A30-B641-B02453D735CC",
    PrimaryLanguageLabel: "Example",
    SecondaryLanguageLabel: "Esempio",
    EnableMCP = true,
    MCPDescription = "Minimal example entity included in the ChillSharp backend starter.")]
public class Example : ChillEntity
{
    // ...
}
```

A query DTO is visible and executable through MCP only when its target returned entity has `EnableMCP = true`.

## 3. Best Practices for AI/Agent Optimization

- **MCPDescription**: Write detailed descriptions at both the entity and property levels using the `MCPDescription` attribute property. AI Agents rely on these descriptions to build correct payloads and query filters.
- **Explain Filters**: In query property descriptions, explicitly describe search behaviors (e.g. contains-style text search, range boundary, exact match). If unspecified, agents assume exact matches.
- **Reference Types**: Declare relationships clearly using `ReferenceChillTypeQuery` and descriptions.
- **OAuth & Auth**: When using protected APIs, MCP endpoints expect bearer authorization (`Authorization: Bearer <access-token>`). The Identity auth module includes built-in OAuth PKCE flow support for ChatGPT/MCP clients.
