# HOW-TO: Use Authentication with ChillSharp

Versione italiana: [Italiano](../it/HowTo/03-authentication.md)

This example shows the smallest useful authentication setup for a ChillSharp API: protect the API, enable the auth module, register an account, log in, and let `ChillSharpClient` reuse and refresh tokens automatically.

## Goal

Expose a protected ChillSharp API backed by ASP.NET Core Identity and authenticate with `ChillSharpClient`.

## 1. Use a context that supports Identity and ChillSharp auth

The context must support your normal ChillSharp model and the auth tables.

```csharp
using ChillSharp;
using ChillSharp.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MyBlogApp;

public class BloggingContext : IdentityDbContext<IdentityUser>, IChillContext, IChillAuthDbContext
{
    public BloggingContext(DbContextOptions<BloggingContext> options) : base(options)
    {
    }

    public string GetChillTypePrefix() => "MyBlogApp";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddChillAuthModel();
    }
}
```

## 2. Register Identity, authentication, and ChillSharp auth services

Protect the normal ChillSharp API and add the auth endpoints on top of it.

```csharp
using ChillSharp.Api;
using ChillSharp.Auth;
using ChillSharp.Auth.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BloggingContext>(options =>
    options.UseSqlite("Data Source=blogging-auth.db"));

builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<BloggingContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(ChillAuthIdentityDefaults.AuthenticationScheme)
    .AddChillAuthBearer();

builder.Services.AddAuthorization();

builder.Services.AddChillApi<BloggingContext>(options =>
{
    options.ProtectedApi = true;
});

builder.Services.AddChillAuthIdentityApi<BloggingContext, IdentityUser>(options =>
{
    options.ReturnPasswordResetTokensInResponse = true;
    options.InitializeRootUserOnStartup = true;
    options.CreateChillAuthUserForRoot = true;
    options.RootUserName = "root";
    options.RootPassword = "Pass123$";
    options.RootEmail = "root@example.com";
    options.RootDisplayName = "Root Administrator";
});
```

When `CreateChillAuthUserForRoot = true`, startup also creates the linked ChillSharp `AuthUser` and sets `CanManagePermissions = true` for that root user.

You can also provide the same bootstrap values through environment variables instead of hardcoding them:

```text
CHILLSHARP_AUTH_ROOT_USERNAME=root
CHILLSHARP_AUTH_ROOT_PASSWORD=Pass123$
CHILLSHARP_AUTH_ROOT_EMAIL=root@example.com
CHILLSHARP_AUTH_ROOT_DISPLAY_NAME=Root Administrator
```

## 3. Enable middleware and map the API

`MapChillApi()` exposes both the normal ChillSharp endpoints and the auth endpoints once the auth services are registered.

```csharp
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BloggingContext>();
    db.Database.EnsureCreated();
}

app.MapChillApi();
app.Run();
```

## 4. Use the root user to manage permissions

The root-user initializer is the easiest bootstrap path for a new protected system because the linked ChillSharp auth user is created with permission-management enabled.

```csharp
var rootClient = new ChillSharpClient("http://localhost:5000/api/chill");

var rootLogin = rootClient.LoginAuthAccount(new LoginAuthIdentityRequest
{
    UserNameOrEmail = "root",
    Password = "Pass123$"
});

var authUsers = rootClient.GetAuthUsers();
Console.WriteLine(authUsers.Count);
```

That login can call auth-management endpoints such as users, roles, and permission rules because the generated root `AuthUser` has `CanManagePermissions = true`.

## 5. Register the first normal account

Create the client with the normal Chill base URL. Auth calls automatically switch from `/api/chill` to `/api/chill-auth`.

```csharp
using ChillSharp.Auth.Contracts;
using ChillSharp.Client;

var client = new ChillSharpClient("http://localhost:5000/api/chill");

var registerResponse = client.RegisterAuthAccount(new RegisterAuthIdentityRequest
{
    UserName = "admin",
    Email = "admin@example.com",
    Password = "Pass123$",
    DisplayName = "Administrator",
    CreateChillAuthUser = true
});
```

After a successful registration, the client stores the returned access token and refresh token internally.

## 6. Log in explicitly

If the account already exists, log in with the same `ChillSharpClient`.

```csharp
var loginResponse = client.LoginAuthAccount(new LoginAuthIdentityRequest
{
    UserNameOrEmail = "admin",
    Password = "Pass123$"
});
```

## 7. Call protected endpoints

Once authenticated, the same client can call protected ChillSharp endpoints.

```csharp
using ChillSharp.Client.Dto;

var query = new ChillDtoQuery
{
    ChillType = "Query.BlogQuery"
};
query.ResultProperties = ChillDtoProperty.Build("Guid", "Name");

var result = client.Query(query);
Console.WriteLine(result.Results.Count);
```

## 8. Let the client refresh tokens automatically

You do not need to manually attach bearer tokens on every request. If the client already has a refresh token, authenticated calls renew the access token when needed.

```csharp
var roles = client.GetAuthRoles();
```

You can also refresh explicitly:

```csharp
var refreshed = client.RefreshAuthAccount();
```

## Notes

- Use `options.ProtectedApi = true` on `AddChillApi<TContext>(...)` if your ChillSharp endpoints must require authentication.
- The root user created by `AddChillAuthIdentityApi(...)` is the bootstrap administrator path. When `CreateChillAuthUserForRoot = true`, the linked ChillSharp `AuthUser` is created with `CanManagePermissions = true`.
- `CreateChillAuthUser = true` creates the linked ChillSharp `AuthUser`, but it does not automatically grant admin permissions.
- For production, bootstrap the first administrator deliberately, for example through root-user initialization or a trusted install-time flow.

Next example: [Create a Docker image and configure it with environment variables](04-docker-env-variables.md)

