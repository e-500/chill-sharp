---
name: chillsharp-template
description: Extend or upgrade the ChillSharp.Template backend and export its agent guidance to generated client projects.
---

# ChillSharp Backend Template

Use this skill when changing the starter backend or applying its patterns to a client project.

- `ChillSharpTemplateContext` integrates `IChillContext`, auth, i18n, attachments, and schema persistence.
- `Model/Context/ChillSharpTemplateContext.ChillSharpModules.cs` owns module `DbSet`s and `AddChill*Model()` calls.
- Add exposed entities under `Model`, register a `DbSet<T>`, and use stable GUIDs in `[ChillEntity]` and `[ChillProperty]`.
- `Program.cs` enables protected API, schema, auth, i18n, attachments, Swagger, and MCP. Preserve that registration unless the client opts out.

Schema metadata is generated from annotated entities and queries and can be persisted through `IChillSchemaDbContext`, `AddChillSchemaModel()`, and `AddChillSchema<TContext>()`. For persisted translations, retain `IChillI18nDbContext`, `AddChillI18nModel()`, and `AddChillI18nApi<TContext>()`, then update the EF migration.

Expose only intentional MCP models with `EnableMCP = true`; describe entity, query, and property matching semantics with `MCPDescription`. MCP access remains subject to the protected API and default-deny permissions. Reason about ACLs in `Module -> Entity -> Property` order: entity `Query/Create/Update/Delete` is required before property `See/Modify` can refine access.

The package packs `.agents/skills/**` from this template. `upgrade.ps1` and `upgrade.sh` extract that folder after updating the local NuGet package. Keep skill names stable and never add generated secrets.
