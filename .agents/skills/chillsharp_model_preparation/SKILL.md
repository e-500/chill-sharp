---
name: chillsharp_model_preparation
description: Guidance on preparing and writing EF Core model/entity classes for ChillSharp including attributes, lifecycle hooks, and DbContext setups.
---

# ChillSharp Model Preparation

This skill guides you on preparing a domain model for ChillSharp, mapping CLR classes to Chill entities, implementing `IChillContext` on the DbContext, using lifecycle hooks, and annotating properties.

## 1. Implement `IChillContext`

The `DbContext` must implement `IChillContext` to declare prefixes, culture fallback preferences, and audit tracking usernames.

```csharp
public class AppDbContext : DbContext, IChillContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public string GetChillTypePrefix() => "MyCompany.MyProduct.Data";
    public string GetPrimaryCultureName() => "en-US";
    public string GetSecondaryCultureName() => "it-IT";
    public string GetCurrentUserName() => Environment.UserName; // Replace with user principal resolved name if using auth
}
```

## 2. ChillEntity Definition & Property Annotation

- Always inherit from `ChillSharp.EF.ChillEntity` for exposed entities.
- Decorate class with `[ChillEntity]` and assign a stable UUID string for `UniquePropertyKeyString`.
- Decorate exposed properties with `[ChillProperty]` using a stable UUID string.
- Override `GetLabel(IChillContext)` to return a user-friendly string (e.g. Title, Name, etc.).

```csharp
using ChillSharp.Annotations;
using ChillSharp.EF;
using System.ComponentModel.DataAnnotations;

[ChillEntity(
    UniquePropertyKeyString: "4E16F6C0-6B95-4D67-98BC-9F4D0D63EAF1",
    PrimaryLanguageLabel: "Blog",
    SecondaryLanguageLabel: "Blog")]
public class Blog : ChillEntity
{
    [Key]
    public override Guid Guid { get; set; }

    [ChillProperty(
        UniquePropertyKeyString: "50B1BB6C-D794-41E4-A85C-D4F9D7A6FA7E",
        PrimaryLanguageLabel: "Blog title",
        SecondaryLanguageLabel: "Titolo del blog")]
    public string Title { get; set; } = string.Empty;

    public override string GetLabel(IChillContext context) => Title;
}
```

## 3. Lifecycle Hooks

Provide lifecycle hooks inside the entity by overriding virtual methods:
- `OnCreate(context)`: Called before first save.
- `OnUpdate(context)`: Called on create and update flows before save.
- `OnAfterUpdate(context)`: Called after audit fields are updated and saved, safe to override since base is implemented explicitly.
- `OnDelete(context)`: Called before entity is deleted.
- `OnAfterDelete(context)`: Called after entity is deleted from DB.
- `OnSelect(context)`: Called during retrieval.
- `OnInflate(context)`: Called when rebuilding relation values.
- `OnAutocomplete(context)`: Override for search suggestion behavior.

## 4. Metadata Schema & Audit Fields

- Audit fields: `Checksum`, `LastUpdateUser`, `LastUpdate`, `LastUpdateUtcOffset` are managed automatically.
- Ensure context implements additional DB interfaces like `IChillSchemaDbContext`, `IChillAuthDbContext`, or `IChillI18nDbContext` and registers them via `modelBuilder.AddChillSchemaModel()`, etc. in `OnModelCreating`.
