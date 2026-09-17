# Making your DbContext "Chillable" (and use it in ChillSharp)

A short guide to preparing your existing EF Core `DbContext` and entity classes so they can be used by ChillSharp's engine and the ChillDto API (query/create/update/delete via DTOs).

> **Goal:** Keep your normal EF Core model and add a small, well-defined surface so the ChillEngine and ChillDtoEngine can activate entities, run queries and fire lifecycle hooks.

---

## Table of contents

* Overview
* Requirements
* Implement `IChillContext`
* Make your entities Chillable
  * Required interface(s) & properties
  * Decorating fields with `ChillFieldAttribute`
  * Lifecycle methods
  * Computed metadata (labels / full text)
* Registering your DbContext with ChillSharp
* Example: `User` entity
* Example: `AppDbContext` implementing `IChillContext`
* Example: Startup / Program registration
* Notes & tips
* License

---

## Overview

ChillSharp expects:

* A `DbContext` that exposes the base namespace prefix used to resolve Chill type ids.
* Entities that conform to the Chill contracts (expose a GUID, implement lifecycle hooks, and have annotated fields).
* DTO objects (`ChillDtoQuery`, `ChillDtoEntity`) to move data over the wire; ChillDtoEngine handles converting between DTOs and real EF entities.

This lets ChillSharp dynamically instantiate and operate on entities by a short `ChillTypeId` (e.g. `User` or `Module.User`) while keeping your EF Core model intact. ([GitHub][1])

---

## Requirements

* .NET 6+ / .NET 7/8 (use the target you already use)
* Entity Framework Core
* Add ChillSharp package(s) (the example repo contains sample code) — see repository for exact package details. ([GitHub][1])

---

## Implement `IChillContext`

Your `DbContext` must implement `IChillContext`. The key method is:

```csharp
string GetChillTypeIdPrefix();
```

This method should return the base namespace prefix that ChillEngine will prepend to short type identifiers. Example: if your entities live in `My.App.Data.Entities`, return `"My.App.Data.Entities"` — then a Chill type id of `"User.Account"` will resolve to `My.App.Data.Entities.User.Account`.

**Why:** ChillSharp activates entities/queries by creating instances via reflection using the combination of prefix + ChillTypeId. ([GitHub][1])

---

## Make your entities Chillable

### 1) Interfaces and GUID

ChillSharp works with an entity interface (conceptually `IChillEntity`). At minimum:

* Expose a `Guid? Guid { get; set; }` property (recommended as primary key — helps offline creation & sync).
* Implement lifecycle hooks (see below).
* Provide label/full-text methods (optional but recommended for metadata).

Example skeleton interface (conceptual):

```csharp
public interface IChillEntity
{
    Guid? Guid { get; set; }

    // lifecycle hooks
    void OnCreate(IChillContext ctx);
    void OnUpdate(IChillContext ctx);
    void OnAfterUpdate(IChillContext ctx);
    void OnDelete(IChillContext ctx);
    void OnAfterDelete(IChillContext ctx);
    void OnSelect(IChillContext ctx);

    // metadata helpers
    string GetLabel(IChillContext ctx);
    string GetShortLabel(IChillContext ctx);
    string GetFullTextContent(IChillContext ctx);
}
```

> Your actual code may use concrete types or additional methods; the example engine calls these lifecycle methods when creating/updating/deleting/selecting entities. Implement them to keep invariants, recalculate computed fields, and populate labels. ([GitHub][1])

---

### 2) Decorating entity fields with `ChillFieldAttribute`

To expose properties as part of Chill DTO serialization and mapping, decorate properties with a `ChillFieldAttribute` (or equivalent present in the ChillSharp lib).

Pattern:

```csharp
public class User : IChillEntity
{
    [Key] // EF Core mapping if you want
    public Guid? Guid { get; set; }

    [ChillField(Type = ChillFieldType.String)]
    public string FirstName { get; set; }

    [ChillField(Type = ChillFieldType.String)]
    public string LastName { get; set; }

    [ChillField(Type = ChillFieldType.DateTime)]
    public DateTime CreatedAt { get; set; }

    // ... lifecycle methods and metadata helpers
}
```

**Notes**

* `ChillFieldAttribute` marks which properties get serialized into `ChillDtoField` and used by `ChillDtoQuery`.
* The `Type` (e.g., `String`, `Int`, `DateTime`) helps the DTO engine perform conversions and validations.
* Avoid exposing navigation collection properties — ChillDto objects are intentionally lightweight and web-friendly (they omit large collections / navigation lists).

---

### 3) Lifecycle methods

Implement lifecycle hooks so ChillEngine can:

* run business logic before/after persistence,
* compute labels and full text content,
* validate or normalize fields.

Typical flow (as used by the engine):

* `OnCreate(ctx)` — called before insert

* `OnUpdate(ctx)` — called before save (both for create and update in examples)

* `OnAfterUpdate(ctx)` — called after save to finalize state

* After persistence, ChillEngine updates:

  * `Label = GetLabel(ctx)`
  * `ShortLabel = GetShortLabel(ctx)`
  * `FullTextContent = GetFullTextContent(ctx)`

* `OnDelete(ctx)` and `OnAfterDelete(ctx)` for delete lifecycle.

* `OnSelect(ctx)` invoked for each entity after querying (useful to hydrate transient fields).

Implement these methods to maintain domain invariants, compute derived fields and raise events if needed. The ChillEngine calls them automatically. ([GitHub][1])

## Example: `User` entity

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using ChillSharp.EF; 

public class User : IChillEntity
{
    [Key]
    public Guid? Guid { get; set; } = Guid.NewGuid();

    [ChillField(Type = ChillFieldType.String)]
    public string FirstName { get; set; } = string.Empty;

    [ChillField(Type = ChillFieldType.String)]
    public string LastName { get; set; } = string.Empty;

    [ChillField(Type = ChillFieldType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Lifecycle hooks
    public void OnCreate(IChillContext ctx)
    {
        CreatedAt = DateTime.UtcNow;
    }

    public void OnUpdate(IChillContext ctx)
    {
        // e.g., update a LastModified time
    }

    public void OnAfterUpdate(IChillContext ctx) { }
    public void OnDelete(IChillContext ctx) { }
    public void OnAfterDelete(IChillContext ctx) { }

    public void OnSelect(IChillContext ctx)
    {
        // hydrate computed/transient fields if needed
    }

    // Metadata helpers
    public string GetLabel(IChillContext ctx) => $"{FirstName} {LastName}";
    public string GetShortLabel(IChillContext ctx) => FirstName;
    public string GetFullTextContent(IChillContext ctx) => $"{FirstName} {LastName} {CreatedAt:O}";
}
```

---

## Example: `AppDbContext` implementing `IChillContext`

```csharp
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext, IChillContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    // ChillSharp uses this prefix to resolve Chill type ids
    public string GetChillTypeIdPrefix()
    {
        // Return the assembly namespace where your entity types live, for dynamic activation.
        // e.g. "My.App.Data.Entities"
        return typeof(User).Namespace ?? string.Empty;
    }

    // Usual EF overrides...
}
```

---

## Example usage from a client (teaser)

To query all users:

```http
POST /api/chill/query
Content-Type: application/json

{
  "ChillTypeId": "User",
  "Fields": {}
}
```

The ChillDtoEngine will:

1. Activate the `User` query object (if used) or map DTO to entity/filters,
2. Execute query via `ChillEngine`,
3. Return results as `ChillDtoEntity` objects (lightweight DTOs).

---

## Notes & tips

* **Prefer GUIDs as PKs** for offline/DTO-friendly behavior. Use EF `[Key]` or configure via Fluent API.
* **Decorate only scalar properties** with `ChillFieldAttribute` — navigation collections are intentionally excluded from DTOs.
* **Keep attribute metadata accurate** — field `Type` helps conversions and validation.
* **Implement lifecycle hooks** to centralize business logic (validations, computed fields).
* **Use `ChillTypeId` naming convention**: short form (e.g. `User` or `Module.User`) — ChillEngine will append your context’s prefix automatically.
* **Security:** ChillSharp exposes dynamic endpoints; secure them with authentication/authorization as you would any API.
* **Licensing:** ChillSharp is published under GPLv3 in the example — ensure you comply with license terms or obtain a commercial license for closed-source use. ([GitHub][1])

---

## Troubleshooting

* If `ChillEngine` cannot activate a type, verify `GetChillTypeIdPrefix()` and the `ChillTypeId` you pass.
* If fields are missing from DTOs, ensure properties are annotated with `ChillFieldAttribute`.
* If lifecycle methods appear not to run, check that ChillEngine is used (i.e., operations go through `ChillEngine.Create/Update/Delete/Query`).

---

## License

This README references the ChillSharp example repository. ChillSharp example code is GPLv3 - check the repository's `LICENSE` and README for official licensing info. ([GitHub][1])

---

If you’d like, I can:

* Generate a **copy-paste README.md file** (I can produce the exact markdown file contents).
* Create a **“how-to” checklist** that you can include in your example repo as CONTRIBUTING.md.
* Produce **Swagger example request/response bodies** for the Chill endpoints.

Which of these would you like next?

[1]: https://github.com/e-500/chill-sharp/tree/main/ChillSharp.Examples/CustomChillApiService "chill-sharp/ChillSharp.Examples/CustomChillApiService at main · e-500/chill-sharp · GitHub"
