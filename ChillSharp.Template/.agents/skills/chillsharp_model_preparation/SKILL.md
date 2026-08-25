---
name: chillsharp_model_preparation
description: Guidance on preparing and writing EF Core model/entity classes for ChillSharp including attributes, lifecycle hooks, and DbContext setups.
---

# ChillSharp Model Preparation

This skill guides you on preparing a domain model for ChillSharp, mapping CLR classes to Chill entities, implementing `IChillContext` on the DbContext, using lifecycle hooks, and annotating properties.

## 1. Implement `IChillContext`

Your DbContext must implement `IChillContext` to declare prefixes, culture fallback preferences, and audit tracking usernames. This is already implemented in `ChillSharpTemplateContext`:

```csharp
public partial class ChillSharpTemplateContext : IdentityDbContext<IdentityUser>, IChillContext
{
    public string GetChillTypePrefix() => "ChillSharp.Template";
    public string GetPrimaryCultureName() => "en-US";
    public string GetSecondaryCultureName() => "it-IT";
    public string GetCurrentUserName() => Environment.UserName; // Or resolve from http context
}
```

## 2. ChillEntity Definition & Property Annotation

- Inherit from `ChillSharp.EF.ChillEntity` for exposed entities.
- Decorate class with `[ChillEntity]` and assign a stable UUID string for `UniquePropertyKeyString`.
- Decorate exposed properties with `[ChillProperty]` using a stable UUID string.
- Override `GetLabel(IChillContext)` to return a user-friendly string (e.g. Title, Name, etc.).

Here is the existing `Example` entity in the template (`Model/Example.cs`):

```csharp
using ChillSharp.Annotations;
using ChillSharp.EF;
using System.ComponentModel.DataAnnotations;

namespace ChillSharp.Template.Model;

[ChillEntity(
    UniquePropertyKeyString: "C65A0497-8D09-4A30-B641-B02453D735CC",
    PrimaryLanguageLabel: "Example",
    SecondaryLanguageLabel: "Esempio",
    LabelFormatString = "{Code} {Title}",
    ShortLabelFormatString = "{Code}",
    FullTextContentFormatString = "{Code} {Title}",
    EnableMCP = true,
    MCPDescription = "Minimal example entity included in the ChillSharp backend starter.")]
public partial class Example : ChillEntity
{
    [Key]
    public override Guid Guid { get; set; }

    [Required]
    [MaxLength(64)]
    [ChillProperty(
        UniquePropertyKeyString: "42AF176F-91BF-4B8F-A56F-E697A0C34EA9",
        PrimaryLanguageLabel: "Code",
        SecondaryLanguageLabel: "Codice",
        MCPDescription = "Short code used to identify the example entity.")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    [ChillProperty(
        UniquePropertyKeyString: "B13A4C7A-FE90-4D40-BF39-9E7FD25EEC26",
        PrimaryLanguageLabel: "Title",
        SecondaryLanguageLabel: "Titolo",
        MCPDescription = "Human-readable title of the example entity.")]
    public string Title { get; set; } = string.Empty;

    public override string GetLabel(IChillContext context) => Title;
}
```

## 3. Lifecycle Hooks

Provide lifecycle hooks inside your entities by overriding virtual methods:
- `OnCreate(context)`: Called before first save.
- `OnUpdate(context)`: Called on create and update flows before save.
- `OnAfterUpdate(context)`: Called after audit fields are updated and saved, safe to override.
- `OnDelete(context)`: Called before entity is deleted.
- `OnAfterDelete(context)`: Called after entity is deleted from DB.
- `OnSelect(context)`: Called during retrieval.
- `OnInflate(context)`: Called when rebuilding relation values.
- `OnAutocomplete(context)`: Override for search suggestion behavior.

## 4. Metadata Schema & Audit Fields

- Audit fields: `Checksum`, `LastUpdateUser`, `LastUpdate`, `LastUpdateUtcOffset` are managed automatically on `ChillEntity`.
- Ensure new entities are registered as `DbSet<T>` properties in your context class so they are tracked by EF Core.
