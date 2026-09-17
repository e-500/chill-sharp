# HOW-TO: API Blog Semplice Su SQLite

Versione originale in inglese: [English](../../HowTo/01-simple-blog-sqlite.md)

Questo esempio mostra la configurazione ChillSharp minima ma utile: una entita `Blog`, un contesto EF Core SQLite e una API ChillSharp senza testi label di schema negli attributi `ChillEntity` o `ChillProperty`.

## Obiettivo

Costruire una API minima che possa creare e leggere entita `Blog` tramite ChillSharp.

## 1. Definire L'Entita

Usa le versioni senza parametri di `ChillEntity` e `ChillProperty` quando non vuoi ancora fornire testi di schema.

```csharp
using ChillSharp.Annotations;
using ChillSharp.EF;
using System.ComponentModel.DataAnnotations;

namespace MyBlogApp.Model;

[ChillEntity]
public class Blog : ChillEntity
{
    [Key]
    public override Guid Guid { get; set; }

    [ChillProperty]
    public string Name { get; set; } = string.Empty;

    [ChillProperty]
    public string? Url { get; set; }

    public override string GetLabel(IChillContext context) => Name;
}
```

## 2. Definire Una Query

`Find` basta per caricare una sola entita tramite `Guid`, ma un tipo query resta utile quando vuoi filtri e proiezioni normali.

```csharp
using ChillSharp.Annotations;
using ChillSharp.EF;

namespace MyBlogApp.Query;

[ChillEntity]
public class BlogQuery : ChillQuery
{
    [ChillProperty]
    public string? Name { get; set; }

    public override IQueryable<IChillEntity> OnQuery(IChillContext context)
    {
        var db = (BloggingContext)context;
        var query = db.Blogs.AsQueryable();

        if (Guid.HasValue)
            query = query.Where(x => x.Guid == Guid.Value);

        if (!string.IsNullOrWhiteSpace(Name))
            query = query.Where(x => x.Name.Contains(Name));

        return query;
    }
}
```

## 3. Creare Il Contesto SQLite

Il contesto deve implementare `IChillContext` e restituire il prefisso namespace usato dai tipi model e query.

```csharp
using Microsoft.EntityFrameworkCore;
using MyBlogApp.Model;

namespace MyBlogApp;

public class BloggingContext : DbContext, IChillContext
{
    public DbSet<Blog> Blogs => Set<Blog>();

    public BloggingContext(DbContextOptions<BloggingContext> options) : base(options)
    {
    }

    public string GetChillTypePrefix() => "MyBlogApp";
}
```

## 4. Registrare ChillSharp

```csharp
using ChillSharp.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BloggingContext>(options =>
    options.UseSqlite("Data Source=blogging.db"));

builder.Services.AddChillApi<BloggingContext>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BloggingContext>();
    db.Database.EnsureCreated();
}

app.MapChillApi();
app.Run();
```

## 5. Creare E Leggere Un Blog Con `ChillSharpClient`

```csharp
using ChillSharp.Client;
using ChillSharp.Client.Dto;

var client = new ChillSharpClient("http://localhost:5000/api/chill");

var blog = new ChillDtoEntity
{
    ChillType = "Model.Blog",
    Guid = Guid.NewGuid()
};
blog.Properties["Name"] = "SQLite Notes";
blog.Properties["Url"] = "https://example.local/sqlite-notes";

var created = client.Create(blog);

var query = new ChillDtoQuery
{
    ChillType = "Query.BlogQuery"
};
query.Properties["Guid"] = created.Guid;
query.ResultProperties = ChillDtoProperty.Build("Guid", "Name", "Url");

var result = client.Query(query);
var loadedBlog = result.Results.Single();
Console.WriteLine($"{loadedBlog.GetString("Name")} -> {loadedBlog.GetString("Url")}");
```

## Note

- `Model.Blog` e `Query.BlogQuery` sono nomi tipo brevi. ChillSharp li espande usando `GetChillTypePrefix()`.
- Con attributi senza parametri, i metadati schema fanno fallback ai nomi CLR di tipo e proprieta.

Esempio successivo: [Aggiungere label di schema e leggerle tramite ChillSharp](02-blog-schema-labels.md)
