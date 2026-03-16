# Registering A ChillSharp Context

Versione italiana: [Italiano](./it/RegisterContext.md)

This document shows how to wire ChillSharp modules into an ASP.NET Core host.

## Minimal Core API

For the core Chill API only:

```csharp
using ChillSharp.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

builder.Services.AddChillApi<AppDbContext>();

var app = builder.Build();
app.MapChillApi();
app.Run();
```

Requirements:

- `AppDbContext` must inherit `DbContext`
- `AppDbContext` must implement `IChillContext`

## What `AddChillApi<TContext>()` Registers

The core registration sets up:

- ChillSharp controllers
- `IChillContext` resolution from your host context
- `IChillDtoEngine`
- optional protected API behavior through `ChillApiOptions`

## Protecting The Core API

If the host already configures authentication and authorization, you can require auth on the Chill API:

```csharp
builder.Services.AddChillApi<AppDbContext>(options =>
{
    options.ProtectedApi = true;
});
```

Then use the standard ASP.NET Core middleware:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.MapChillApi();
```

## Adding Schema Services

To persist and serve schema metadata:

```csharp
using ChillSharp.Schema;

builder.Services.AddChillSchema<AppDbContext>();
```

Context requirements:

- `AppDbContext : IChillSchemaDbContext`
- `modelBuilder.AddChillSchemaModel()`

`ChillSharp.Schema` also owns the schema cache registration.

## Adding Auth Management

To expose auth-management endpoints without ASP.NET Core Identity account flows:

```csharp
using ChillSharp.Auth.Api;

builder.Services.AddChillAuthApi<AppDbContext>();
```

Context requirements:

- `AppDbContext : IChillAuthDbContext`
- `modelBuilder.AddChillAuthModel()`

This adds endpoints for:

- auth users
- roles
- user-role assignments
- permission rules
- permission evaluation

## Adding Identity Account Flows

To expose account registration, login, refresh tokens, password change, and password reset:

```csharp
using ChillSharp.Auth;
using ChillSharp.Auth.Api;
using Microsoft.AspNetCore.Identity;

builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(ChillAuthIdentityDefaults.AuthenticationScheme)
    .AddChillAuthBearer();

builder.Services.AddAuthorization();

builder.Services.AddChillAuthIdentityApi<AppDbContext, IdentityUser>();
```

This builds on `AddChillAuthApi<TContext>()`.

## Adding I18n Services

To expose i18n endpoints:

```csharp
using ChillSharp.I18n.Api;

builder.Services.AddChillI18nApi<AppDbContext>();
```

Context requirements:

- `AppDbContext : IChillI18nDbContext`
- `modelBuilder.AddChillI18nModel()`

The module currently exposes:

- `GET /api/chill-i18n/text/{labelGuid}/{cultureName}`
- `PUT /api/chill-i18n/text`

## Full Host Example

```csharp
using ChillSharp.Api;
using ChillSharp.Auth;
using ChillSharp.Auth.Api;
using ChillSharp.I18n.Api;
using ChillSharp.Schema;
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

builder.Services.AddChillApi<AppDbContext>(options =>
{
    options.ProtectedApi = true;
});
builder.Services.AddChillSchema<AppDbContext>();
builder.Services.AddChillAuthIdentityApi<AppDbContext, IdentityUser>();
builder.Services.AddChillI18nApi<AppDbContext>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapChillApi();
app.Run();
```

## Mapping Behavior

`app.MapChillApi()` maps the ChillSharp controllers and helper endpoints. It does not replace standard ASP.NET Core middleware setup. You still need:

- `UseAuthentication()` if auth is enabled
- `UseAuthorization()` if authorization is enabled

I18n and auth controllers are added through controller discovery when their modules are registered.

## Recommended Startup Order

1. register the EF Core context
2. register Identity if needed
3. register authentication and authorization if needed
4. register `AddChillApi<TContext>()`
5. register optional modules (`Schema`, `Auth`, `I18n`)
6. build app
7. apply middleware
8. map Chill API

## OpenAPI / Swagger

ChillSharp itself does not force Swagger into your host. If you want OpenAPI output for documentation or client generation, add it in the host application:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
```

That is the recommended base for generating TypeScript and Python clients from a ChillSharp host.

