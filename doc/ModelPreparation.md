# Preparing A Model For ChillSharp

Versione italiana: [Italiano](./it/ModelPreparation.md)

This document describes the model-side requirements for exposing an EF Core domain model through ChillSharp.

## Goals

After preparation, your model can:

- be activated dynamically by Chill type name
- be queried and mutated through Chill DTOs
- expose schema metadata for clients
- participate in audit-field maintenance
- use context-specific label cultures and current-user information

## 1. Implement `IChillContext`

Your `DbContext` must implement `IChillContext`.

Required behavior:

```csharp
public class AppDbContext : DbContext, IChillContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public string GetChillTypePrefix()
    {
        return "MyCompany.MyProduct.Data";
    }

    public string GetPrimaryCultureName()
    {
        return "en-US";
    }

    public string GetSecondaryCultureName()
    {
        return "it-IT";
    }

    public string GetCurrentUserName()
    {
        return Environment.UserName;
    }
}
```

### What each method is used for

- `GetChillTypePrefix()`
  Expands short Chill type names such as `Model.Blog` into fully qualified CLR types.

- `GetPrimaryCultureName()`
  Defines which culture should use `PrimaryLanguageLabel`.

- `GetSecondaryCultureName()`
  Defines which culture should use `SecondaryLanguageLabel`.

- `GetCurrentUserName()`
  Feeds entity audit tracking.

Each context instance can return different values. This is important in multi-tenant or multi-module hosts where more than one Chill context can exist with different language or user settings.

## 2. Use `ChillEntity` For Exposed Entities

The recommended pattern is to inherit from `ChillSharp.EF.ChillEntity`.

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

    [ChillProperty(
        UniquePropertyKeyString: "A18E7754-D8F7-45FE-B8A8-EA762A4EC9E6",
        PrimaryLanguageLabel: "Blog url",
        SecondaryLanguageLabel: "Url del blog")]
    public string Url { get; set; } = string.Empty;

    public override string GetLabel(IChillContext context) => Title;
}
```

`ChillEntity` already provides:

- `Guid`
- `Label`
- `ShortLabel`
- `FullTextContent`
- `Checksum`
- `LastUpdateUser`
- `LastUpdateUtc`

## 3. Annotate Exposed Properties

ChillSharp only treats properties decorated with `[ChillProperty]` as part of the Chill metadata surface.

That affects:

- DTO mapping
- schema generation
- checksum calculation
- label metadata

If a property is not marked with `[ChillProperty]`, it is not part of the standard Chill property surface.

## 4. Understand Lifecycle Hooks

`ChillEngine` drives entity lifecycle methods.

### Create flow

On create, ChillSharp runs:

1. `OnCreate(context)`
2. `OnUpdate(context)`
3. save
4. internal audit update + `OnAfterUpdate(context)`
5. recompute `Label`, `ShortLabel`, `FullTextContent`
6. save

### Update flow

On update, ChillSharp runs:

1. `OnUpdate(context)`
2. save
3. internal audit update + `OnAfterUpdate(context)`
4. recompute `Label`, `ShortLabel`, `FullTextContent`
5. save

### Delete flow

On delete, ChillSharp runs:

1. `OnDelete(context)`
2. save delete
3. `OnAfterDelete(context)`
4. save

## 5. Audit-Field Behavior

`ChillEntity` automatically maintains:

- `Checksum`
- `LastUpdateUser`
- `LastUpdateUtc`

The checksum is computed from all `[ChillProperty]` values except the audit fields themselves.

Notes:

- scalar values are serialized using invariant culture
- referenced `IChillEntity` values contribute their `Guid`
- collections are flattened into a deterministic string sequence before summing bytes

### Why overriding `OnAfterUpdate()` is safe

`ChillEntity` uses an explicit interface implementation for `IChillEntity.OnAfterUpdate(...)`.

`ChillEngine` calls `OnAfterUpdate()` through the interface, so the runtime flow is:

1. update audit fields
2. call the derived class override of `public virtual OnAfterUpdate(...)`

This means derived entities get a clean override surface while the base audit logic cannot be skipped accidentally.

## 6. Labels And Cultures

`PrimaryLanguageLabel` and `SecondaryLanguageLabel` are not just comments. They are interpreted using the active UI culture and the active `IChillContext`.

Current behavior:

- if the current UI culture matches the context secondary culture, ChillSharp prefers `SecondaryLanguageLabel`
- if it matches the primary culture, ChillSharp prefers `PrimaryLanguageLabel`
- otherwise it falls back to primary first, then secondary

This logic is used when schema metadata is generated.

## 7. Queries

Queries should implement `IChillQuery<IChillEntity>` and can also be decorated with `ChillEntityAttribute` and `ChillPropertyAttribute`.

That allows query schemas to be generated exactly like entity schemas.

## 8. Schema Persistence Readiness

If you want persisted schema metadata and schema caching, your context must also implement `IChillSchemaDbContext` and include:

```csharp
modelBuilder.AddChillSchemaModel();
```

Then register:

```csharp
builder.Services.AddChillSchema<AppDbContext>();
```

## 9. Auth Readiness

If you want ChillSharp auth and permissions, your context must implement `IChillAuthDbContext` and include:

```csharp
modelBuilder.AddChillAuthModel();
```

Then register one of:

```csharp
builder.Services.AddChillAuthApi<AppDbContext>();
builder.Services.AddChillAuthIdentityApi<AppDbContext, IdentityUser>();
```

## 10. I18n Readiness

If you want localized text storage and lookup, your context must implement `IChillI18nDbContext` and include:

```csharp
modelBuilder.AddChillI18nModel();
```

Then register:

```csharp
builder.Services.AddChillI18nApi<AppDbContext>();
```

## 11. Recommendations

- Prefer inheriting from `ChillEntity` instead of implementing `IChillEntity` from scratch.
- Use stable GUIDs in `UniquePropertyKeyString` and `UniqueEntityKeyString`.
- Mark only the properties you actually want in the Chill DTO/schema surface.
- Keep `GetLabel()` and `GetFullTextContent()` cheap enough to run during standard CRUD flows.
- Return a real request identity from `GetCurrentUserName()` in API hosts.
- Keep context-specific culture settings on the context, not in static globals.

