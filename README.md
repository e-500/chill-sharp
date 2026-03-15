# ChillSharp

Turn an existing EF Core model into a live REST API in minutes.

ChillSharp is built for the moment when your database model already exists, your domain types already exist, and you do not want to spend days writing repetitive controllers, DTO mappers, and CRUD plumbing just to get an application online. Plug in your `DbContext`, map the API, and you are suddenly standing on a deployable backend.

Few lines. Real endpoints. Query, find, create, update, delete. Built-in support for authentication and schema metadata. Ready to run locally, ready to ship in a container, ready to become the data backbone of your app and the foundation for strong, consistent UIs.

[ChillSharp.dev](https://chillsharp.dev/)

## Get The Package

Latest releases:
[GitHub Releases](https://github.com/e-500/chill-sharp/releases/)

## Deploy-Ready In A Few Lines

If you already have an EF Core context, this is the core setup:

```csharp
using ChillSharp.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddChillApi<AppDbContext>();

var app = builder.Build();
app.MapChillApi();
app.Run();
```

That is enough to expose ChillSharp endpoints such as:

- `POST /api/chill/query`
- `POST /api/chill/find`
- `POST /api/chill/create`
- `POST /api/chill/update`
- `POST /api/chill/delete`

If you prefer SQLite, the registration is the same:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));
```

## Why It Feels Fast

- reuse your existing `DbContext` and entity model
- avoid hand-written CRUD controllers
- expose DTO-based endpoints immediately
- add schema metadata for strong and consistent UIs
- layer authentication and authorization with `ChillSharp.Auth`
- call the API from .NET with `ChillSharp.Client`

## Built For Real Applications

ChillSharp is not only about exposing CRUD endpoints quickly. It also helps you keep applications coherent as they grow.

- `ChillSharp.Auth` adds authentication and authorization flows, including protected APIs, Identity-backed accounts, roles, permissions, and root-user bootstrap.
- `ChillSharp.Schema` exposes schema information that UI layers can use to build forms, lists, labels, and editors with a single source of truth.
- Together, auth and schema metadata make it easier to build frontends that are not only fast to develop, but also consistent in behavior, permissions, and presentation.

## Documentation

Start here:
[doc/README.md](doc/README.md)

The documentation index includes:

- model preparation
- context registration
- schema metadata
- authentication
- Docker and environment-variable deployment
- one-to-many relation examples

## Docker

ChillSharp works well in containerized deployments. For a full example using environment variables and root-user bootstrap, see:
[doc/HowTo/05-docker-env-variables.md](doc/HowTo/05-docker-env-variables.md)

## License

This library is released under [AGPLv3](LICENSE.md).

If you need an LGPL-licensed version, please ask.
