# HOW-TO: Connect ChillSharp MCP to ChatGPT

This guide shows how to expose a protected ChillSharp MCP server over HTTPS and connect it from ChatGPT using the built-in ChillSharp OAuth flow.

## Goal

Let ChatGPT connect to:

```text
https://your-domain.example/api/chill-mcp
```

and have every MCP request run under the same ChillSharp user, role, and permission limitations used by the normal bearer-authenticated API.

## Requirements

- A public HTTPS domain that can reach your ASP.NET Core host
- A `DbContext` that supports ChillSharp, schema metadata, and auth
- ASP.NET Core Identity configured for your users
- `ProtectedApi = true`
- At least one MCP-enabled entity or query

ChatGPT cannot connect directly to `localhost`. For local development, expose the app with a public HTTPS tunnel and use the public URL in ChatGPT.

## 1. Prepare the DbContext

The context must support the normal ChillSharp model, schema metadata for MCP discovery, and auth tables for users, roles, permissions, and token sessions.

```csharp
using ChillSharp;
using ChillSharp.Auth;
using ChillSharp.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MyApp;

public class AppDbContext : IdentityDbContext<IdentityUser>, IChillContext, IChillAuthDbContext, IChillSchemaDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public string GetChillTypePrefix() => "MyApp";

    public string GetPrimaryCultureName() => "en-US";

    public string GetSecondaryCultureName() => "it-IT";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddChillAuthModel();
        modelBuilder.AddChillSchemaModel();
    }
}
```

If your app also uses i18n or attachments, keep those model registrations too.

## 2. Register Identity, bearer auth, ChillSharp, and MCP

The Identity-backed `AddChillApi<TContext, TUser>()` registration enables the auth endpoints, the MCP module, and the OAuth endpoints that ChatGPT needs.

```csharp
using ChillSharp.Api;
using ChillSharp.Auth.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(ChillAuthIdentityDefaults.AuthenticationScheme)
    .AddChillAuthBearer();

builder.Services.AddAuthorization();

builder.Services.AddChillApi<AppDbContext, IdentityUser>(options =>
{
    options.ProtectedApi = true;

    // Defaults shown explicitly for clarity.
    options.EnableMcpApi = true;
    options.EnableOAuthEndpoints = true;
    options.OAuthBasePath = "/api/chill-auth/oauth";
    options.OAuthProtectedResourcePath = "/api/chill-mcp";
    options.OAuthAuthorizationCodeLifetime = TimeSpan.FromMinutes(5);
});
```

## 3. Map the API

Authentication and authorization middleware must run before `MapChillApi()`.

```csharp
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapChillApi();
app.Run();
```

With the defaults, this exposes:

| Purpose | URL |
| --- | --- |
| MCP server | `/api/chill-mcp` |
| OAuth authorization metadata | `/.well-known/oauth-authorization-server` |
| MCP protected-resource metadata | `/.well-known/oauth-protected-resource` |
| Dynamic client registration | `/api/chill-auth/oauth/register` |
| Authorization and consent page | `/api/chill-auth/oauth/authorize` |
| Token endpoint | `/api/chill-auth/oauth/token` |

## 4. Create or bootstrap a user

ChatGPT signs in through the ChillSharp OAuth authorization page using the same ASP.NET Core Identity users used by normal ChillSharp bearer authentication.

For a first protected system, you can bootstrap a root user:

```csharp
builder.Services.AddChillApi<AppDbContext, IdentityUser>(options =>
{
    options.ProtectedApi = true;
    options.InitializeRootUserOnStartup = true;
    options.CreateChillAuthUserForRoot = true;
    options.RootUserName = "root";
    options.RootPassword = "Pass123$";
    options.RootEmail = "root@example.com";
    options.RootDisplayName = "Root Administrator";
});
```

When `CreateChillAuthUserForRoot = true`, ChillSharp also creates the linked `AuthUser` record used by permission checks.

## 5. Enable only safe MCP schemas

ChatGPT can only see schemas that are MCP-enabled. Mark only the entities and queries that are safe and useful for AI access.

```csharp
using ChillSharp.Annotations;
using ChillSharp.EF;

[ChillEntity(
    UniquePropertyKeyString: "4E16F6C0-6B95-4D67-98BC-9F4D0D63EAF1",
    PrimaryLanguageLabel: "Invoice",
    SecondaryLanguageLabel: "Fattura",
    EnableMCP = true,
    MCPDescription = "Customer invoice header. Use it to inspect invoice identity, customer, dates, totals, and payment state.")]
public class Invoice : ChillEntity
{
    [ChillProperty(
        UniquePropertyKeyString: "50B1BB6C-D794-41E4-A85C-D4F9D7A6FA7E",
        PrimaryLanguageLabel: "Invoice number",
        SecondaryLanguageLabel: "Numero fattura",
        MCPDescription = "Human-readable invoice number used by accountants and customers.")]
    public string InvoiceNumber { get; set; } = string.Empty;
}
```

Use clear `MCPDescription` text on entities, queries, and properties. ChatGPT relies heavily on those descriptions when choosing tools and building query payloads.

## 6. Connect from ChatGPT

In ChatGPT, add a custom connector or remote MCP server using your public HTTPS MCP URL:

```text
https://your-domain.example/api/chill-mcp
```

ChatGPT should discover the OAuth metadata, register itself as a public OAuth client, and open the ChillSharp authorization page.

After the user signs in and consents:

1. ChatGPT receives an authorization code.
2. ChatGPT exchanges the code and PKCE verifier for a ChillSharp bearer access token.
3. ChatGPT calls the MCP endpoint with:

```http
Authorization: Bearer <access-token>
```

OAuth is only the consent and token-acquisition flow. The MCP endpoint still uses normal ChillSharp bearer authentication.

## Permission behavior

OAuth users are not separate users.

The OAuth flow authenticates an ASP.NET Core Identity user and issues the same ChillSharp bearer token type used by normal login. The token contains the same user identifier claims, so ChillSharp resolves the same `AuthUser.ExternalId` and applies the same roles, permission rules, schema permissions, and API limitations.

That means:

- a user blocked from a normal protected ChillSharp operation is also blocked through ChatGPT
- a role-limited user keeps the same limitations through MCP
- MCP visibility still requires `EnableMCP`
- OAuth scopes do not currently create a separate permission layer

## Useful public URLs to test

Open these from outside your server network:

```text
https://your-domain.example/.well-known/oauth-authorization-server
https://your-domain.example/.well-known/oauth-protected-resource
https://your-domain.example/api/chill-mcp
```

The MCP URL should reject anonymous access when `ProtectedApi = true`, and the response should advertise OAuth protected-resource metadata in the `WWW-Authenticate` header.

## Troubleshooting

### ChatGPT cannot reach the server

Verify that the MCP URL uses public HTTPS and is not a private network or `localhost` address.

### ChatGPT does not start OAuth

Check:

- `EnableOAuthEndpoints = true`
- `ProtectedApi = true`
- `AddChillAuthBearer()` is registered
- `UseAuthentication()` and `UseAuthorization()` run before `MapChillApi()`
- `/.well-known/oauth-authorization-server` is reachable over HTTPS
- `/.well-known/oauth-protected-resource` is reachable over HTTPS

### Login succeeds but MCP actions are forbidden

The Identity login succeeded, but the linked ChillSharp `AuthUser` or its roles do not allow the requested operation. Check `AuthUser.ExternalId`, role assignments, and permission rules.

### ChatGPT sees no useful schemas

Check that the target entity or query is MCP-enabled and has useful descriptions:

- `EnableMCP = true`
- entity/query `MCPDescription`
- property-level `MCPDescription`
- focused query types for common AI workflows

### Multiple app instances lose OAuth registrations

The built-in dynamic OAuth client registry is currently in memory. For multi-instance production or restart-stable registrations, persist OAuth client registrations in the auth database.

## Production checklist

- Use HTTPS only
- Keep `ProtectedApi = true`
- Enable MCP only on safe schemas
- Use focused query surfaces instead of exposing everything
- Give ChatGPT a least-privilege user or role
- Review mutating tools such as create, update, delete, and chunk
- Persist OAuth client registrations if running more than one app instance
- Keep access-token lifetimes short

## Related documents

- [MCP module reference](README.md)
- [Authentication how-to](../HowTo/03-authentication.md)
- [Model preparation](../ModelPreparation.md)
