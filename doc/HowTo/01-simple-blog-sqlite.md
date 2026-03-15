# HOW-TO: Simple Blog API on SQLite

This example shows the smallest useful ChillSharp setup: one `Blog` entity, one EF Core SQLite context, and a ChillSharp API with no schema label texts in `ChillEntity` or `ChillProperty` attributes.

## Goal

Build a minimal API that can create and read `Blog` entities through ChillSharp.

## 1. Define the entity

Use the parameterless versions of `ChillEntity` and `ChillProperty` when you do not want to provide schema texts yet.

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

## 2. Define a query

`Find` is enough to load one entity by `Guid`, but a query type is still useful when you want normal filtering and projections.

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

## 3. Create the SQLite context

The context must implement `IChillContext` and return the namespace prefix used by your model and query types.

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

## 4. Register ChillSharp

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

## 5. Create and read a blog with `ChillSharpClient`

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

## Notes

- `Model.Blog` and `Query.BlogQuery` are short type names. ChillSharp expands them using `GetChillTypePrefix()`.
- With parameterless attributes, schema metadata falls back to the CLR type and property names.

Next example: [Add schema labels and read them through ChillSharp](02-blog-schema-labels.md)
