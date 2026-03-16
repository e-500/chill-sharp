# ChillSharp Authentication And Authorization

This document covers the auth module at a reference level. For a guided setup, keep using [doc/HowTo/03-authentication.md](../HowTo/03-authentication.md).

## Module Split

`ChillSharp.Auth` exposes two related but distinct concerns:

- account authentication
  ASP.NET Core Identity-backed register, login, refresh, password-change, and password-reset flows

- authorization management
  auth users, roles, role assignments, permission rules, and permission evaluation

## Account Endpoints

Registered through:

```csharp
builder.Services.AddChillAuthIdentityApi<AppDbContext, IdentityUser>();
```

Typical routes:

- `/api/chill-auth/account/register`
- `/api/chill-auth/account/login`
- `/api/chill-auth/account/refresh`
- `/api/chill-auth/account/change-password`
- `/api/chill-auth/account/request-password-reset`
- `/api/chill-auth/account/reset-password`

These are the routes used internally by `ChillSharpClient`.

## Authorization Management Endpoints

Registered through:

```csharp
builder.Services.AddChillAuthApi<AppDbContext>();
```

Typical route groups:

- `/api/chill-auth/users`
- `/api/chill-auth/roles`
- `/api/chill-auth/permissions`

These endpoints manage:

- `AuthUser`
- `AuthRole`
- `AuthUserRole`
- `AuthPermissionRule`
- permission evaluation results

## Context Requirements

The host context must:

- implement `IChillAuthDbContext`
- include `modelBuilder.AddChillAuthModel()`

For Identity-backed accounts, the context must also be a valid EF store for ASP.NET Core Identity, typically:

```csharp
public class AppDbContext : IdentityDbContext<IdentityUser>, IChillContext, IChillAuthDbContext
{
}
```

## Root User Bootstrap

`AddChillAuthIdentityApi(...)` can bootstrap a root account on startup.

Supported configuration:

- direct options in code
- environment variables

Default environment variables:

- `CHILLSHARP_AUTH_ROOT_USERNAME`
- `CHILLSHARP_AUTH_ROOT_PASSWORD`
- `CHILLSHARP_AUTH_ROOT_EMAIL`
- `CHILLSHARP_AUTH_ROOT_DISPLAY_NAME`

When enabled, the bootstrap flow can also create the linked ChillSharp `AuthUser` with permission-management access.

## Permission Management Access

Being authenticated is not enough to manage the auth module.

Auth-management endpoints require a ChillSharp auth user with permission-management rights, typically:

- `CanManagePermissions = true`

This is the critical bootstrap problem for a clean database:

- the first registered Identity user may exist
- the linked `AuthUser` may exist
- but no one may yet have rights to manage roles and permissions

That is why root-user bootstrap or another trusted setup path matters.

## Client Usage

Use the normal Chill base URL:

```csharp
var client = new ChillSharpClient("http://localhost:5000/api/chill");
```

`ChillSharpClient` automatically switches to `/api/chill-auth/...` for auth methods.

Examples:

- `RegisterAuthAccount`
- `LoginAuthAccount`
- `RefreshAuthAccount`
- `ChangeAuthPassword`
- `CreateAuthRole`
- `CreateAuthPermissionRule`
- `EvaluateAuthEntityPermission`

## Token Handling

`ChillSharpClient` stores:

- access token
- refresh token

If a refresh token is present, the client can renew the access token automatically during later authenticated calls.

## Relationship With The Permission Model

The exact permission-resolution rules are documented separately in:

- [PermissionModel/README.md](../PermissionModel/README.md)

Use that document for the precedence and scope model. Use this one for registration and runtime auth flow.

