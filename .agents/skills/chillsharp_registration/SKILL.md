---
name: chillsharp_registration
description: Guidance on registering ChillSharp API, DbContext modules, schema services, auth, and i18n configurations in an ASP.NET Core application.
---

# Registering a ChillSharp Context

This skill describes how to register and configure ChillSharp modules in an ASP.NET Core application.

## 1. DbContext Configuration

In your `DbContext` class:
- Implement `IChillContext` (and optionally `IChillSchemaDbContext`, `IChillAuthDbContext`, `IChillI18nDbContext`).
- Register internal ChillSharp models in `OnModelCreating(ModelBuilder modelBuilder)` using extension methods:
  - `modelBuilder.AddChillSchemaModel()`
  - `modelBuilder.AddChillAuthModel()`
  - `modelBuilder.AddChillI18nModel()`

## 2. Core API Registration

In `Program.cs`:
```csharp
using ChillSharp.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

// Register Core Chill API
builder.Services.AddChillApi<AppDbContext>();

var app = builder.Build();
app.MapChillApi();
app.Run();
```

## 3. Protecting the API

If using auth:
```csharp
builder.Services.AddChillApi<AppDbContext>(options =>
{
    options.ProtectedApi = true;
});

// Middleware setup
app.UseAuthentication();
app.UseAuthorization();
app.MapChillApi();
```

## 4. Registering Modules

- **Schema Services**:
  ```csharp
  using ChillSharp.Schema;
  builder.Services.AddChillSchema<AppDbContext>();
  ```
- **Auth Services**:
  ```csharp
  using ChillSharp.Auth.Api;
  builder.Services.AddChillAuthApi<AppDbContext>();
  ```
- **ASP.NET Core Identity Integration**:
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

  // Combined registration
  builder.Services.AddChillApi<AppDbContext, IdentityUser>();
  ```
- **I18n Localized Text Services**:
  ```csharp
  using ChillSharp.I18n.Api;
  builder.Services.AddChillI18nApi<AppDbContext>();
  ```

## 5. Startup Order Flow
1. Register DbContext.
2. Register Identity, Authentication, and Authorization.
3. Register Chill API (`AddChillApi`).
4. Register optional ChillSharp modules (Schema, Auth, I18n).
5. Build application, apply middleware (`UseAuthentication`, `UseAuthorization`).
6. Call `MapChillApi()`.
7. Configure OpenAPI/Swagger for client generation if required.
