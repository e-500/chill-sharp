# HOW-TO: Gestire Una Relazione Blog-Posts Uno-a-Molti

Versione originale in inglese: [English](../../HowTo/04-blog-posts-one-to-many.md)

Questo esempio estende il modello blog con i post e mostra come caricare un blog insieme ai suoi post in una singola chiamata `ChillSharpClient.Query(...)`.

## Obiettivo

Modellare una relazione uno-a-molti `Blog` -> `Posts` e proiettare la collezione annidata dal client ChillSharp.

## 1. Definire Le Entita

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

    [ChillProperty]
    public ICollection<Post>? Posts { get; set; }

    public override string GetLabel(IChillContext context) => Name;
}

[ChillEntity]
public class Post : ChillEntity
{
    [Key]
    public override Guid Guid { get; set; }

    [ChillProperty]
    public Blog? Blog { get; set; }

    [ChillProperty]
    public DateTime? CreatedAt { get; set; }

    [ChillProperty]
    public string Title { get; set; } = string.Empty;

    [ChillProperty]
    public string Content { get; set; } = string.Empty;

    public override void OnCreate(IChillContext context)
    {
        base.OnCreate(context);
        CreatedAt = DateTime.UtcNow;
    }

    public override string GetLabel(IChillContext context) => Title;
}
```

## 2. Aggiungere Entrambi I Set Al Contesto

```csharp
using Microsoft.EntityFrameworkCore;
using MyBlogApp.Model;

namespace MyBlogApp;

public class BloggingContext : DbContext, IChillContext
{
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<Post> Posts => Set<Post>();

    public BloggingContext(DbContextOptions<BloggingContext> options) : base(options)
    {
    }

    public string GetChillTypePrefix() => "MyBlogApp";
}
```

## 3. Definire Una Query Per I Blog

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

## 4. Creare Un Blog E Due Post

La relazione viene creata inviando il DTO padre `Blog` come proprieta `Blog` di ogni `Post`.

```csharp
using ChillSharp.Client;
using ChillSharp.Client.Dto;

var client = new ChillSharpClient("http://localhost:5000/api/chill");

var blog = new ChillDtoEntity
{
    ChillType = "Model.Blog",
    Guid = Guid.NewGuid()
};
blog.Properties["Name"] = "ChillSharp Blog";
blog.Properties["Url"] = "https://example.local/chillsharp-blog";

var createdBlog = client.Create(blog);

var firstPost = new ChillDtoEntity
{
    ChillType = "Model.Post",
    Guid = Guid.NewGuid()
};
firstPost.Properties["Title"] = "First post";
firstPost.Properties["Content"] = "Hello from ChillSharp";
firstPost.Properties["Blog"] = createdBlog.Mock();
client.Create(firstPost);

var secondPost = new ChillDtoEntity
{
    ChillType = "Model.Post",
    Guid = Guid.NewGuid()
};
secondPost.Properties["Title"] = "Second post";
secondPost.Properties["Content"] = "Nested queries are handy";
secondPost.Properties["Blog"] = createdBlog.Mock();
client.Create(secondPost);
```

## 5. Caricare Un Blog Con I Suoi Post In Una Chiamata

Questa e la parte piu importante per una relazione uno-a-molti: la proiezione annidata viene descritta tramite `ChillDtoProperty.With(...)`, e la collezione restituita viene letta con `GetCollection(...)`.

```csharp
using ChillSharp.Client.Dto;

var query = new ChillDtoQuery
{
    ChillType = "Query.BlogQuery"
};
query.Properties["Guid"] = createdBlog.Guid;
query.ResultProperties = ChillDtoProperty.Build(
    "Guid",
    "Name",
    "Url",
    ChillDtoProperty.With("Posts", "Guid", "Title", "CreatedAt"));

var queryResult = client.Query(query);
var loadedBlog = queryResult.Results.Single();

Console.WriteLine(loadedBlog.GetString("Name"));

foreach (var post in loadedBlog.GetCollection("Posts"))
{
    Console.WriteLine(post.GetString("Title"));
}
```

## Cosa Ti Da Questo

- una chiamata HTTP per caricare l'entita padre e la collezione figlia
- un builder di proiezione fortemente tipizzato lato client
- gestione della relazione tramite DTO senza scrivere un controller dedicato per `Blog` o `Post`

Prossimo: [Torna all'indice della documentazione](../README.md)
