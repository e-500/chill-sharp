# HOW-TO: Aggiungere Label Di Schema Al Blog E Leggerle

Versione originale in inglese: [English](../../HowTo/02-blog-schema-labels.md)

Questo esempio aggiorna il modello `Blog` precedente aggiungendo testi di schema a `ChillEntity` e `ChillProperty`, poi mostra come leggere quelle label.

## Obiettivo

Decorare il modello con metadati di schema e recuperarli tramite i servizi schema di ChillSharp.

## 1. Aggiungere Label A Entita E Proprieta

```csharp
using ChillSharp.Annotations;
using ChillSharp.EF;
using System.ComponentModel.DataAnnotations;

namespace MyBlogApp.Model;

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
        PrimaryLanguageLabel: "Blog name",
        SecondaryLanguageLabel: "Nome blog")]
    public string Name { get; set; } = string.Empty;

    [ChillProperty(
        UniquePropertyKeyString: "A18E7754-D8F7-45FE-B8A8-EA762A4EC9E6",
        PrimaryLanguageLabel: "Blog url",
        SecondaryLanguageLabel: "Url blog")]
    public string? Url { get; set; }

    public override string GetLabel(IChillContext context) => Name;
}
```

## 2. Abilitare La Persistenza Dello Schema

Per esporre metadati schema tramite `get-schema`, il contesto EF Core deve anche implementare `IChillSchemaDbContext`, aggiungere il modello schema e registrare `AddChillSchema<TContext>()`.

```csharp
using ChillSharp.Schema;
using ChillSharp.Schema.Model;
using Microsoft.EntityFrameworkCore;
using MyBlogApp.Model;

namespace MyBlogApp;

public class BloggingContext : DbContext, IChillContext, IChillSchemaDbContext
{
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<ChillSchemaEntry> SchemaEntries => Set<ChillSchemaEntry>();

    public BloggingContext(DbContextOptions<BloggingContext> options) : base(options)
    {
    }

    public string GetChillTypePrefix() => "MyBlogApp";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddChillSchemaModel();
    }
}
```

Registra il servizio schema insieme all'API:

```csharp
using ChillSharp.Api;
using ChillSharp.Schema;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BloggingContext>(options =>
    options.UseSqlite("Data Source=blogging.db"));

builder.Services.AddChillApi<BloggingContext>();
builder.Services.AddChillSchema<BloggingContext>();
```

## 3. Leggere I Metadati Di Schema Da Un Client

`ChillSharpClient.GetSchema(...)` restituisce metadati proprieta generati dal modello decorato.

```csharp
using ChillSharp.Client;

var client = new ChillSharpClient("http://localhost:5000/api/chill");
var schema = client.GetSchema("Model.Blog", "default");

var nameProperty = schema?.Properties.Single(x => x.Name == "Name");
var urlProperty = schema?.Properties.Single(x => x.Name == "Url");

Console.WriteLine(nameProperty?.DisplayName); // Blog name
Console.WriteLine(urlProperty?.DisplayName);  // Blog url
```

## 4. Leggere Il Nome Visualizzato Dell'Entita Sul Server

La `ChillSharp.Dto.ChillDtoSchema` lato server contiene anche il `DisplayName` dell'entita, popolato da `ChillEntityAttribute.PrimaryLanguageLabel`.

```csharp
using ChillSharp;

public class SchemaDebugService
{
    private readonly IChillDtoEngine _dtoEngine;

    public SchemaDebugService(IChillDtoEngine dtoEngine)
    {
        _dtoEngine = dtoEngine;
    }

    public string? GetBlogSchemaDisplayName()
    {
        var schema = _dtoEngine.GetSchema("Model.Blog", "default");
        return schema?.DisplayName;
    }
}
```

## Note

- `DisplayName` della proprieta viene da `ChillPropertyAttribute.PrimaryLanguageLabel`.
- `DisplayName` dell'entita viene da `ChillEntityAttribute.PrimaryLanguageLabel`.
- Se poi chiami `SetSchema(...)`, i valori schema persistiti possono sovrascrivere i default generati per un dato `ChillType` e `ChillViewCode`.

Esempio successivo: [Usare l'autenticazione con ChillSharp](03-authentication.md)
