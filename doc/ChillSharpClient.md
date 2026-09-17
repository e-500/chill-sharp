# ChillSharp.Client

`ChillSharp.Client` is the .NET client library for calling a ChillSharp host from console apps, workers, tests, desktop apps, or other .NET services.

Use it when the consumer is .NET. For browser frameworks or Python automation, use the generic clients under `extra-libs/` or generate a host-specific client from OpenAPI.

## Install

Reference the `ChillSharp.Client` project or package from the consuming .NET application.

```xml
<ProjectReference Include="..\ChillSharp.Client\ChillSharp.Client.csproj" />
```

Then import the client namespaces:

```csharp
using ChillSharp.Client;
using ChillSharp.Client.Dto;
```

Auth account methods use request and response contracts from `ChillSharp.Auth.Contracts`:

```csharp
using ChillSharp.Auth.Contracts;
```

I18n methods use contracts from `ChillSharp.I18n.Contracts`:

```csharp
using ChillSharp.I18n.Contracts;
```

## Create A Client

The normal base URL is the core ChillSharp endpoint:

```csharp
var client = new ChillSharpClient("http://localhost:5000/api/chill");
```

You can also pass the host root. The client appends the default `api/chill` path:

```csharp
var client = new ChillSharpClient("http://localhost:5000");
```

For a custom API base path:

```csharp
var client = new ChillSharpClient(
    "http://localhost:5000",
    new ChillSharpClientOptions { ApiBasePath = "backend" });
```

This resolves the core API as:

```text
http://localhost:5000/backend/chill
```

## Culture

Pass a default culture when reading schemas or i18n text:

```csharp
var client = new ChillSharpClient(
    "http://localhost:5000/api/chill",
    CultureName: "it-IT");

var schema = client.GetSchema("Model.Blog", "default");
```

You can still override culture per schema call:

```csharp
var englishSchema = client.GetSchema("Model.Blog", "default", "en-GB");
```

## Authentication

If the API is protected, authenticate with one of these patterns.

Use an existing bearer token:

```csharp
var client = new ChillSharpClient(
    "http://localhost:5000/api/chill",
    AuthToken: accessToken);
```

Use credentials and let the client log in on demand:

```csharp
var client = new ChillSharpClient(
    "http://localhost:5000/api/chill",
    UserName: "admin",
    Password: "Pass123$");
```

Register or log in through the auth account endpoints:

```csharp
var client = new ChillSharpClient("http://localhost:5000/api/chill");

var token = client.LoginAuthAccount(new LoginAuthIdentityRequest
{
    UserNameOrEmail = "admin",
    Password = "Pass123$"
});
```

The client stores the returned access token and refresh token. Later authenticated calls reuse the access token and refresh it automatically when possible.

To force refresh:

```csharp
client.RefreshAuthAccount();
```

To revoke the current session:

```csharp
client.LogoutAuthAccount();
```

## Core Entity Operations

ChillSharp entity calls use `ChillDtoEntity`.

Create:

```csharp
var blog = new ChillDtoEntity
{
    ChillType = "Model.Blog"
};
blog.Properties["Title"] = "My first blog";
blog.Properties["Description"] = "Created through ChillSharp.Client";

var created = client.Create(blog);
```

Find:

```csharp
var found = client.Find(new ChillDtoEntity
{
    ChillType = "Model.Blog",
    Guid = created.Guid
});
```

Update:

```csharp
created.Properties["Title"] = "Updated blog";
var updated = client.Update(created);
```

Delete:

```csharp
client.Delete(updated);
```

Validate without saving:

```csharp
var errors = client.Validate(blog);
```

## Query And Lookup

Use `Query` when the host exposes an entity query type:

```csharp
var query = new ChillDtoQuery
{
    ChillType = "Query.BlogQuery",
    Pagination = new ChillPagination
    {
        Page = 1,
        PageResults = 20
    }
};

query.Properties["FullTextSearch"] = "release notes";

var result = client.Query(query);
foreach (var item in result.Results)
{
    Console.WriteLine(item.Label);
}
```

Use `Lookup` for generic full-text entity lookup:

```csharp
var lookup = client.Lookup(new ChillDtoQuery
{
    ChillType = "Model.Blog",
    Properties =
    {
        ["FullTextSearch"] = "release"
    }
});
```

## Batch Operations

Use `Chunk` to send several operations in one HTTP call.

```csharp
var operations = new List<ChillOperation>
{
    new() { Index = 0, Verb = ChillOperationVerb.TRANSACTION },
    new()
    {
        Index = 1,
        Verb = ChillOperationVerb.CREATE,
        Entity = new ChillDtoEntity
        {
            ChillType = "Model.Blog",
            Properties =
            {
                ["Title"] = "Batch blog"
            }
        }
    },
    new() { Index = 2, Verb = ChillOperationVerb.COMMIT }
};

var processed = client.Chunk(operations);
```

Use a transaction/commit wrapper when the write operations must be committed together.

## Schema And Menu

Read schema metadata:

```csharp
var schema = client.GetSchema("Model.Blog", "default");
var schemaList = client.GetSchemaList();
```

Manage entity options:

```csharp
var options = client.GetEntityOptions("Model.Blog");
options.HandleAttachments = true;
client.SetEntityOptions(options);
```

Read menu nodes:

```csharp
var rootItems = client.GetMenu();
var children = client.GetMenu(rootItems[0].Guid);
```

Create or update a menu item:

```csharp
var item = client.SetMenu(new ChillDtoMenuItem
{
    PositionNo = 10,
    Title = "Blogs",
    ComponentName = "CRUD",
    MenuHierarchy = "CONTENT.BLOGS"
});
```

Delete a menu item and its descendants:

```csharp
client.DeleteMenu(item.Guid);
```

Schema write operations require schema-management access on protected hosts.

## Auth Management

Auth-management helpers are available when the host registers `ChillSharp.Auth`.

```csharp
var users = client.GetAuthUsers();
var roles = client.GetAuthRoles();
var permissions = client.GetAuthPermissions();
```

Create a managed auth user:

```csharp
var user = client.CreateAuthUser(new CreateAuthUserRequest
{
    ExternalId = "external-user-id",
    UserName = "editor",
    DisplayName = "Editor",
    IsActive = true,
    MenuHierarchy = "CONTENT"
});
```

Create a role and assign it:

```csharp
var role = client.CreateAuthRole(new CreateAuthRoleRequest
{
    Name = "Editors",
    IsActive = true,
    MenuHierarchy = "CONTENT"
});

client.AssignAuthRole(user.Guid, role.Guid);
```

For richer administration screens, use the aggregate helpers:

```csharp
var managedUser = client.GetAuthManagedUser(user.Guid);
var roleList = client.GetAuthRoleList();
var moduleList = client.GetAuthModuleList();
```

## I18n

Read a localized text:

```csharp
var text = client.GetText(new GetTextRequest
{
    LabelGuid = labelGuid,
    CultureName = "it-IT",
    PrimaryDefaultText = "Hello"
});
```

Read several texts:

```csharp
var texts = client.GetTexts(requests);
```

Create or update a text:

```csharp
client.SetText(new SetTextRequest
{
    LabelGuid = labelGuid,
    CultureName = "it-IT",
    Value = "Ciao"
});
```

## Attachments

Upload a file and attach it to an entity:

```csharp
var files = client.UploadAttachment(
    created,
    @"C:\temp\contract.pdf",
    title: "Contract");
```

List attachments for an entity:

```csharp
var attachments = client.GetAttachments(created);
```

Download an attachment:

```csharp
var bytes = client.DownloadAttachment(attachments[0].Guid);
```

Download directly to a file:

```csharp
client.DownloadAttachmentToFile(
    attachments[0].Guid,
    @"C:\temp\downloaded-contract.pdf");
```

Attachment upload and private download require the attachment module and appropriate auth configuration.

## Custom HttpClient

Use a custom factory when tests or host integration need special headers, handlers, or certificates:

```csharp
var client = new ChillSharpClient(
    "http://localhost:5000/api/chill",
    () =>
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("X-Test-User", "integration-user");
        return httpClient;
    });
```

The factory is invoked for each request. Dispose any external resources according to your application’s `HttpClient` strategy.

## Errors

Server errors and transport failures are wrapped in `ChillClientException`.

```csharp
try
{
    client.Create(blog);
}
catch (ChillClientException ex)
{
    Console.WriteLine(ex.Message);
}
```

For HTTP errors, the exception message includes the status code and response body when available.

## Endpoint Resolution

From a core URL ending in `/chill`, the client resolves module endpoints automatically:

| Module | Resolved endpoint |
| --- | --- |
| Core | `/api/chill` |
| Auth | `/api/chill-auth` |
| Schema | `/api/chill-schema` |
| I18n | `/api/chill-i18n` |
| Attachment | `/api/chill-attachment` |

For example:

```csharp
var client = new ChillSharpClient("http://localhost:5000/api/chill");
client.GetMenu();          // calls /api/chill-schema/get-menu
client.LoginAuthAccount(...); // calls /api/chill-auth/login
```

## Related Documentation

- [AuthenticationModel/README.md](./AuthenticationModel/README.md)
- [MenuModel.md](./MenuModel.md)
- [AttachmentModel/README.md](./AttachmentModel/README.md)
- [ClientGeneration/README.md](./ClientGeneration/README.md)
