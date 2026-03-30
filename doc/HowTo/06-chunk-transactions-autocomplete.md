# HOW-TO: Use Chunk, Transactions, and Autocomplete

Versione italiana: [Italiano](../it/HowTo/06-chunk-transactions-autocomplete.md)

This example shows how to send multiple ChillSharp operations in one call with `chunk`, how to wrap write operations in one database transaction, and how to use `autocomplete` for entity and query DTOs.

## Goal

Use the core ChillSharp API efficiently when the client must:

- execute several operations in one HTTP request
- commit a group of writes atomically
- ask the server to complete or normalize DTO values before saving

## 1. Prepare the client

All examples below use the .NET client and assume the core API is mapped at `/api/chill`.

```csharp
using ChillSharp.Client;
using ChillSharp.Client.Dto;

var client = new ChillSharpClient("http://localhost:5000/api/chill");
```

## 2. Send multiple operations with `chunk`

`chunk` sends a list of `ChillOperation` items to `/api/chill/chunk`.
Each operation can contain a `Query` or an `Entity`, depending on the `Verb`.

Set `Index` explicitly when execution order matters.

```csharp
using ChillSharp.Client.Dto;

var firstPost = new ChillDtoEntity
{
    ChillType = "Model.Post",
    Guid = Guid.Parse("11111111-1111-1111-1111-111111111111")
};
firstPost.Properties["Title"] = "First";
firstPost.Properties["Author"] = "Ada";

var secondPost = new ChillDtoEntity
{
    ChillType = "Model.Post",
    Guid = Guid.Parse("22222222-2222-2222-2222-222222222222")
};
secondPost.Properties["Title"] = "Second";
secondPost.Properties["Author"] = "Linus";

var updateFirstPost = new ChillDtoEntity
{
    ChillType = "Model.Post",
    Guid = firstPost.Guid
};
updateFirstPost.Properties["Title"] = "First updated";

var operations = client.Chunk(new List<ChillOperation>
{
    new() { Index = 0, Verb = ChillOperationVerb.CREATE, Entity = firstPost },
    new() { Index = 1, Verb = ChillOperationVerb.CREATE, Entity = secondPost },
    new() { Index = 2, Verb = ChillOperationVerb.UPDATE, Entity = updateFirstPost }
});
```

What this gives you:

- one HTTP request instead of three
- ordered execution through `Index`
- one combined response containing the processed operations

## 3. Wrap a chunk in one transaction

Use `transaction` and `commit` when all enclosed write operations must succeed or fail together.

```csharp
var blog = new ChillDtoEntity
{
    ChillType = "Model.Blog",
    Guid = Guid.NewGuid()
};
blog.Properties["Name"] = "Batch blog";
blog.Properties["Url"] = "https://example.local/batch-blog";

var post = new ChillDtoEntity
{
    ChillType = "Model.Post",
    Guid = Guid.NewGuid()
};
post.Properties["Title"] = "Batch post";
post.Properties["Blog"] = blog.Mock();

var transactionalOperations = client.Chunk(new List<ChillOperation>
{
    new() { Index = 0, Verb = ChillOperationVerb.TRANSACTION },
    new() { Index = 1, Verb = ChillOperationVerb.CREATE, Entity = blog },
    new() { Index = 2, Verb = ChillOperationVerb.CREATE, Entity = post },
    new() { Index = 3, Verb = ChillOperationVerb.COMMIT }
});
```

Use this pattern only for the write operations that must share the same database transaction.
If one operation fails before `commit`, the transaction is not committed.

## 4. Autocomplete an entity DTO

`autocomplete` uses the same DTO style as `create`, `update`, and `delete`, but it calls `/api/chill/autocomplete`.

For entities, ChillSharp performs `OnAutocomplete(...)` without persisting changes:

- if the entity already exists, it is loaded from the current `DbContext`
- otherwise it is attached in `Added` state
- the autocomplete logic runs inside a temporary transaction
- the transaction is rolled back at the end, so the database is unchanged

This is useful for previewing computed values such as slugs, labels, derived text, or default field combinations.

```csharp
var draftBlog = new ChillDtoEntity
{
    ChillType = "Model.Blog",
    Guid = Guid.NewGuid()
};
draftBlog.Properties["Title"] = "  My first ChillSharp blog  ";

var autocompletedBlog = client.Autocomplete(draftBlog);

Console.WriteLine(autocompletedBlog.GetString("Title"));
Console.WriteLine(autocompletedBlog.GetString("Url"));
```

Typical entity-side logic:

```csharp
public override void OnAutocomplete(IChillContext context)
{
    base.OnAutocomplete(context);

    Title = Title?.Trim();

    if (string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(Title))
    {
        Url = "/blogs/" + Title.ToLowerInvariant().Replace(' ', '-');
    }
}
```

## 5. Autocomplete a query DTO

Queries also use `/api/chill/autocomplete`, but they do not participate in the EF Core context transaction flow.
The query DTO is simply passed through `OnAutocomplete(...)` on the resolved `IChillQuery`.

This is useful for normalizing filters before `Query(...)`, for example:

- trimming text inputs
- expanding a search box into a full-text field
- setting default paging or sorting values

```csharp
var blogQuery = new ChillDtoQuery
{
    ChillType = "Query.BlogQuery"
};
blogQuery.Properties["Title"] = "  chillsharp  ";

var autocompletedQuery = client.Autocomplete(blogQuery);

Console.WriteLine(autocompletedQuery.GetString("Title"));
Console.WriteLine(autocompletedQuery.GetString("FullTextSearch"));
```

Typical query-side logic:

```csharp
public override void OnAutocomplete(IChillContext context)
{
    base.OnAutocomplete(context);

    Title = Title?.Trim();

    if (!string.IsNullOrWhiteSpace(Title))
    {
        FullTextSearch = Title;
    }
}
```

## 6. Choose the right API

- use `create`, `update`, and `delete` when the operation must change the database immediately
- use `chunk` when several operations should travel in one request
- use `transaction` plus `commit` inside `chunk` when multiple writes must be atomic
- use `autocomplete` when the server should calculate or normalize values without saving them

Next: [Back to the documentation index](../README.md)
