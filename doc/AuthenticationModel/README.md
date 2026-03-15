# Authentication Model

This document explains how to interact with the ChillSharp auth module through `ChillSharpClient`.

It focuses on:

- account flows backed by ASP.NET Core Identity
- auth users, roles, and permission rules
- the behavior of a server that starts from a clean empty state

## Base URL Convention

Create the client with the normal Chill API base URL:

```csharp
var client = new ChillSharpClient("http://localhost:5002/api/chill");
```

When you call auth methods, the client automatically switches from:

```text
/api/chill
```

to:

```text
/api/chill-auth
```

So:

- `client.Query(...)` goes to `/api/chill/...`
- `client.RegisterAuthAccount(...)` goes to `/api/chill-auth/account/register`
- `client.CreateAuthRole(...)` goes to `/api/chill-auth/roles`

## Two Parts of the Auth Module

The auth module exposes two different API surfaces.

### 1. Identity account endpoints

These are for authentication:

- register
- login
- refresh token
- change password
- request password reset
- reset password

Client methods:

- `RegisterAuthAccount`
- `LoginAuthAccount`
- `RefreshAuthAccount`
- `ChangeAuthPassword`
- `RequestAuthPasswordReset`
- `ResetAuthPassword`

### 2. Authorization management endpoints

These are for application authorization data:

- auth users
- roles
- user-role assignments
- permission rules
- permission evaluation

Client methods:

- `GetAuthUsers`, `GetAuthUser`, `CreateAuthUser`, `UpdateAuthUser`, `DeleteAuthUser`
- `GetAuthRoles`, `GetAuthRole`, `CreateAuthRole`, `UpdateAuthRole`, `DeleteAuthRole`
- `GetAuthUserRoles`, `AssignAuthRole`, `RemoveAuthRole`
- `GetAuthPermissionRules`, `GetAuthPermissionRule`, `CreateAuthPermissionRule`, `UpdateAuthPermissionRule`, `DeleteAuthPermissionRule`
- `EvaluateAuthEntityPermission`, `EvaluateAuthPropertyPermission`, `EvaluateAuthPropertySetPermission`

## Clean Empty State

Assume the database is empty.

At startup there are:

- no Identity accounts
- no `AuthUser` rows
- no roles
- no permission rules

This has one important consequence:

`RegisterAuthAccount(..., CreateChillAuthUser = true)` creates the Identity account and the matching `AuthUser`, but it does **not** create any role or grant any permission rule.

So after the first registration, the user is authenticated but still has no application permissions until you create roles and/or direct permission rules.

Also, a normal `AuthUser` does not automatically gain access to the auth-management API. That requires `CanManagePermissions = true`.

If you protect the auth-management endpoints (`/api/chill-auth/users`, `/roles`, `/permissions`) from the first request, you must also provide a bootstrap strategy for the first administrator, for example:

- seed the first admin user and its rules in the database
- temporarily expose the management endpoints during installation
- protect them with a separate host-level policy that already knows who is admin

Without a bootstrap step, a clean protected system has no user able to grant the first privileges.

## Minimal Server Example with `DummyContext`

The integration tests already contain a working example based on `DummyContext`.

```csharp
using ChillSharp.Api;
using ChillSharp.Auth.Api;
using ChillSharp.Tests.EF;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DummyContext>(options =>
    options.UseSqlite("Data Source=auth-demo.db"));

builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<DummyContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(ChillAuthIdentityDefaults.AuthenticationScheme)
    .AddChillAuthBearer();

builder.Services.AddAuthorization();

builder.Services.AddChillApi<DummyContext>(options =>
{
    options.ProtectedApi = true;
});

builder.Services.AddChillAuthIdentityApi<DummyContext, IdentityUser>(options =>
{
    options.ReturnPasswordResetTokensInResponse = true;
    // Optional explicit bootstrap values.
    // options.RootUserName = "root";
    // options.RootPassword = "Pass123$";
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapChillApi();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DummyContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
```

`DummyContext` works because it is both:

- an `IdentityDbContext<IdentityUser>`
- an `IChillAuthDbContext`

and it includes the auth tables through `modelBuilder.AddChillAuthModel()`.

## Optional Root User Bootstrap

`AddChillAuthIdentityApi(...)` can initialize a root Identity account during startup.

By default, it looks for these environment variables:

- `CHILLSHARP_AUTH_ROOT_USERNAME`
- `CHILLSHARP_AUTH_ROOT_PASSWORD`
- `CHILLSHARP_AUTH_ROOT_EMAIL`
- `CHILLSHARP_AUTH_ROOT_DISPLAY_NAME`

If user name and password are present, the extension creates the Identity account if it does not already exist. By default it also creates the matching `AuthUser`.

The root `AuthUser` is created with `CanManagePermissions = true`.

Example:

```csharp
builder.Services.AddChillAuthIdentityApi<DummyContext, IdentityUser>(options =>
{
    options.InitializeRootUserOnStartup = true;
    options.CreateChillAuthUserForRoot = true;
});
```

You can also set the values directly in code instead of using environment variables:

```csharp
builder.Services.AddChillAuthIdentityApi<DummyContext, IdentityUser>(options =>
{
    options.RootUserName = "root";
    options.RootPassword = "Pass123$";
    options.RootEmail = "root@example.com";
    options.RootDisplayName = "Root";
});
```

## Typical Client Flows

### Register the first account

```csharp
using ChillSharp.Auth.Contracts;
using ChillSharp.Client;

var client = new ChillSharpClient("http://localhost:5002/api/chill");

var registerResponse = client.RegisterAuthAccount(new RegisterAuthIdentityRequest
{
    UserName = "admin",
    Email = "admin@example.com",
    Password = "Pass123$",
    DisplayName = "First Admin",
    CreateChillAuthUser = true
});
```

After this call:

- the client stores `AccessToken` and `RefreshToken`
- future authenticated client calls automatically use the bearer token
- the matching `AuthUser` exists if `CreateChillAuthUser` is `true`
- no role or permission exists yet

### Log in with an existing account

```csharp
var client = new ChillSharpClient("http://localhost:5002/api/chill");

var loginResponse = client.LoginAuthAccount(new LoginAuthIdentityRequest
{
    UserNameOrEmail = "admin",
    Password = "Pass123$"
});
```

### Let the client handle token refresh automatically

If the client already has a token pair, authenticated calls refresh it automatically when needed.

```csharp
var client = new ChillSharpClient("http://localhost:5002/api/chill");

client.LoginAuthAccount(new LoginAuthIdentityRequest
{
    UserNameOrEmail = "admin",
    Password = "Pass123$"
});

var roles = client.GetAuthRoles();
```

You can also refresh explicitly:

```csharp
var refreshed = client.RefreshAuthAccount();
```

## Managing Users, Roles, and Rules

Once you are allowed to manage the auth module, the usual flow is:

1. create or locate the auth user
2. create a role
3. assign the role to the user
4. create permission rules for that role or directly for the user

### Create an auth user directly

This is useful when the external identity already exists elsewhere.

```csharp
var authUser = client.CreateAuthUser(new CreateAuthUserRequest
{
    ExternalId = "external-user-001",
    UserName = "external.admin",
    DisplayName = "External Admin",
    IsActive = true,
    CanManagePermissions = false
});
```

If `CanManagePermissions` is `true`, that user can interact with the auth-management API and therefore manage all auth users, roles, and permission rules.

### Create a role and assign it

```csharp
var adminRole = client.CreateAuthRole(new CreateAuthRoleRequest
{
    Name = "Administrators",
    Description = "Full access to blog content",
    IsActive = true
});

client.AssignAuthRole(authUser.Guid, adminRole.Guid);
```

### Create permission rules

Replace `YourModule` and `YourEntity` with the actual ChillSharp module and entity names used by your application.

```csharp
using ChillSharp.Auth.Model;

client.CreateAuthPermissionRule(new CreateAuthPermissionRuleRequest
{
    RoleGuid = adminRole.Guid,
    Effect = PermissionEffect.Allow,
    Action = PermissionAction.Query,
    Scope = PermissionScope.Entity,
    Module = "YourModule",
    EntityName = "YourEntity",
    Description = "Allow entity query"
});

client.CreateAuthPermissionRule(new CreateAuthPermissionRuleRequest
{
    RoleGuid = adminRole.Guid,
    Effect = PermissionEffect.Allow,
    Action = PermissionAction.Update,
    Scope = PermissionScope.Entity,
    Module = "YourModule",
    EntityName = "YourEntity",
    Description = "Allow entity update"
});
```

For property rules:

```csharp
client.CreateAuthPermissionRule(new CreateAuthPermissionRuleRequest
{
    RoleGuid = adminRole.Guid,
    Effect = PermissionEffect.Deny,
    Action = PermissionAction.Modify,
    Scope = PermissionScope.Property,
    Module = "YourModule",
    EntityName = "YourEntity",
    PropertyName = "SensitiveField",
    Description = "Block editing one property"
});
```

## Evaluating Permissions

You can ask the server how a permission resolves for a user.

### Entity permission

```csharp
var result = client.EvaluateAuthEntityPermission(new EvaluateEntityPermissionRequest
{
    UserGuid = authUser.Guid,
    Action = PermissionAction.Update,
    Module = "YourModule",
    EntityName = "YourEntity"
});
```

### Property permission

```csharp
var propertyResult = client.EvaluateAuthPropertyPermission(new EvaluatePropertyPermissionRequest
{
    UserGuid = authUser.Guid,
    Action = PermissionAction.Modify,
    Module = "YourModule",
    EntityName = "YourEntity",
    PropertyName = "SensitiveField"
});
```

### Multiple properties in one call

```csharp
var setResult = client.EvaluateAuthPropertySetPermission(new EvaluatePropertySetPermissionRequest
{
    UserGuid = authUser.Guid,
    Action = PermissionAction.See,
    Module = "YourModule",
    EntityName = "YourEntity",
    PropertyNames = new[] { "Field1", "Field2", "SensitiveField" }
});
```

The evaluation response tells you:

- whether access is allowed
- which rule matched
- whether the match came from a user rule or role rule
- why the decision was made

## Password Flows

### Change password

Requires an authenticated user.

```csharp
var response = client.ChangeAuthPassword(new ChangePasswordRequest
{
    CurrentPassword = "Pass123$",
    NewPassword = "Pass456$"
});
```

### Request password reset

```csharp
var resetToken = client.RequestAuthPasswordReset(new RequestPasswordResetRequest
{
    UserNameOrEmail = "admin"
});
```

### Complete password reset

```csharp
var resetResponse = client.ResetAuthPassword(new ResetPasswordRequest
{
    UserId = resetToken.UserId!,
    ResetToken = resetToken.ResetToken!,
    NewPassword = "Pass789$"
});
```

`PasswordResetTokenResponse.ResetToken` is only useful when the server is configured to return reset tokens directly. That is appropriate for tests and demos, not for production.

## Constructor Choices

`ChillSharpClient` supports three common ways to start:

```csharp
new ChillSharpClient("http://localhost:5002/api/chill");
new ChillSharpClient("http://localhost:5002/api/chill", "existing-access-token");
new ChillSharpClient("http://localhost:5002/api/chill", "admin", "Pass123$");
```

Notes:

- username/password mode can obtain tokens on demand
- after a successful register or login, the client stores the returned tokens internally
- once a refresh token is available, the client prefers refresh-token renewal

## Recommended First-Run Sequence

For a brand new empty database, the safest sequence is:

1. start the server and create the schema
2. register the first Identity account with `CreateChillAuthUser = true`
3. bootstrap admin privileges through a trusted setup path
4. create roles and permission rules
5. use normal `ChillSharpClient` auth-management calls from then on

If step 3 is missing, the first registered user can authenticate but may still be unable to administer the system.
