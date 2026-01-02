# **ChillSharp**
***Simplifying Enterprise Data Management and API Development***

[https://chillsharp.dev/](ChillSharp.dev)

ChillSharp is a powerful .NET library designed to streamline the management of complex data models and accelerate the development of web APIs. Built on top of Entity Framework Core, ChillSharp abstracts the complexity of database interactions while providing a lightweight, web-friendly layer for querying, creating, updating, and deleting entities through Data Transfer Objects (DTOs).

## 💾 Get the latest NuGet package

👉 [Link to this GitHub Releases page](https://github.com/e-500/chill-sharp/releases/)

# **How to build** your API server in minutes

### Sql Server
```csharp
using ChillSharp;
using ChillSharp.Dto;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Assume you already have your DbContext (e.g., AppDbContext) configured
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add ChillSharp API using your existing DbContext
builder.Services.AddChillApi<AppDbContext>();

var app = builder.Build();

// Map ChillSharp API endpoints automatically
app.MapChillApi();

app.Run();

```

### SqLite
```csharp
using ChillSharp;
using ChillSharp.Dto;
using Microsoft.EntityFrameworkCore;

var apiServer = Task.Run(() =>
{
    // Standard EF Core initialization
    ctx = new AtlasContext();
    ctx.Database.Migrate();
    var builder = WebApplication.CreateBuilder(new string[0]);
    builder.Services.AddDbContext<AtlasContext>(options =>
            options.UseSqlite($"Data Source={ctx.DbPath}"));

    // Chill API initialization
    builder.Services.AddChillApi<AtlasContext>();
    var app = builder.Build();

    // Map endpoints
    app.MapChillApi();

    // GO !!!!
    app.Run();
});
apiServer.Wait(5000);

```

## ✅ How It Works

1. **DbContext Ready:** You already have a fully configured EF Core context (`AppDbContext`).
2. **Plug & Play API:** `AddChillApi<AppDbContext>()` registers all controllers and ChillSharp services.
3. **Automatic Routing:** `MapChillApi()` exposes endpoints for query, create, update, and delete operations using DTOs.
4. **DTO-Friendly:** All entities are automatically converted to `ChillDtoEntity` for web-friendly JSON responses.

## 🚀 Result

Within **less than 10 lines**, your existing database is fully exposed via REST API endpoints:

* `POST /api/chill/query` → Execute dynamic queries
* `POST /api/chill/find` → Find an entity by GUID
* `POST /api/chill/create` → Create a new entity
* `POST /api/chill/update` → Update an entity
* `POST /api/chill/delete` → Delete an entity

No extra boilerplate. No manual controllers. Just plug your DbContext and go.

# **Dockerize** your API server

```docker
# Use official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Use runtime-only image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Expose port
EXPOSE 5000

# Start the app
ENTRYPOINT ["dotnet", "ChillSharpDemo.dll"]
```

# Summary

## Key Features:

- **DTO-First Design:** Work with simple, serializable DTOs instead of complex EF Core entities, making it ideal for REST APIs, microservices, and front-end applications.
- **Dynamic Queries:** Easily build and execute flexible queries using ChillSharp’s query objects, supporting filtering, sorting, and pagination with minimal boilerplate.
- **Entity Lifecycle Management:** Automaes entity lifecycle events (create, update, delete) and manages computed fields such as labels, short labels, and full-text content.
- **Seamless EF Core Integration:** Fully compatible with existing DbContext implementations, allowing you to leverage the full power of EF Core while keeping your API layer clean and maintainable.
- **Type-Safe and Extensible:** Supports polymorphic entities, type-safe queries, and dynamic entity activation while ensuring compile-time safety and extensibility.

## Business Benefits:

- Reduces development time for CRUD APIs and complex data operations.
- Improves maintainability by separating DTOs from database entities.
- Enhances performance for web and cloud applications by optimizing queries and serialization.
- Provides a robust foundation for large-scale enterprise applications with minimal custom code.

## Use Cases:

- Rapid REST API development for enterprise applications.
- Microservices architectures requiring lightweight, serializable entities.
- Complex data systems needing flexible queries and dynamic entity handling.
- Applications that require consistent lifecycle management and metadata for entities.

## Conclusion:
ChillSharp enables developers to focus on business logic rather than database plumbing, providing a consistent, extensible, and web-friendly framework for .NET applications. It bridges the gap between complex EF Core entities and clean, consumable API layers, accelerating development while ensuring reliability and scalability.

---
This library is released under [AGPLv3](LICENSE.md) license
If you need a LGPL licensed version, please ask! We are happy to help.
