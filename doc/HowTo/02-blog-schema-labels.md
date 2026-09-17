# HOW-TO: Add Schema Labels to Blog and Read Them

This example updates the previous `Blog` model by adding schema texts to `ChillEntity` and `ChillProperty`, then shows how to read those labels back.

## Goal

Decorate the model with schema metadata and retrieve it through ChillSharp schema services.

## 1. Add labels to the entity and properties

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

## 2. Enable schema persistence

To expose schema metadata through `get-schema`, the EF Core context must also implement `IChillSchemaDbContext`, add the schema model, and register `AddChillSchema<TContext>()`.

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

Register the schema service together with the API:

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

## 3. Read schema metadata from a client

`ChillSharpClient.GetSchema(...)` returns property metadata generated from the decorated model.

```csharp
using ChillSharp.Client;

var client = new ChillSharpClient("http://localhost:5000/api/chill");
var schema = client.GetSchema("Model.Blog", "default");

var nameProperty = schema?.Properties.Single(x => x.Name == "Name");
var urlProperty = schema?.Properties.Single(x => x.Name == "Url");

Console.WriteLine(nameProperty?.DisplayName); // Blog name
Console.WriteLine(urlProperty?.DisplayName);  // Blog url
```

## 4. Read the entity display name in server-side code

The server-side `ChillSharp.Dto.ChillDtoSchema` also carries the entity-level `DisplayName`, which is populated from `ChillEntityAttribute.PrimaryLanguageLabel`.

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

## Notes

- Property `DisplayName` comes from `ChillPropertyAttribute.PrimaryLanguageLabel`.
- Entity `DisplayName` comes from `ChillEntityAttribute.PrimaryLanguageLabel`.
- If you later call `SetSchema(...)`, persisted schema values can override the generated defaults for a given `ChillType` and `ChillViewCode`.

Next example: [Use authentication with ChillSharp](03-authentication.md)
