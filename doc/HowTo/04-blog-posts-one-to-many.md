# HOW-TO: Handle a One-to-Many Blog-Posts Relation

Versione italiana: [Italiano](../it/HowTo/04-blog-posts-one-to-many.md)

This example extends the blog model with posts and shows how to load one blog together with its posts in a single `ChillSharpClient.Query(...)` call.

## Goal

Model a `Blog` to `Posts` one-to-many relation and project the nested collection from the ChillSharp client.

## 1. Define the entities

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

## 2. Add both sets to the context

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

## 3. Define a query for blogs

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

## 4. Create a blog and two posts

The relation is created by sending the parent `Blog` DTO as the `Blog` property of each `Post`.

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

## 5. Load one blog with its posts in one call

This is the part that matters most for a one-to-many relation: the nested projection is described through `ChillDtoProperty.With(...)`, and the returned collection is read with `GetCollection(...)`.

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

## What this gives you

- one HTTP call to load the parent entity and the child collection
- a strongly-typed projection builder on the client side
- DTO-based relation handling without writing a dedicated controller for `Blog` or `Post`

Next: [Back to the documentation index](../README.md)

