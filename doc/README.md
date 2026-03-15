# ChillSharp Documentation

ChillSharp is a .NET library built on top of Entity Framework Core that can expose an existing data model as a REST API with very little setup.

Its main goal is to reduce the amount of boilerplate needed to build data-driven applications while keeping the model centered on your existing `DbContext`.

## Core Capabilities

- expose EF Core entities through ready-to-use REST endpoints
- query, find, create, update, delete, and batch-process entities through DTOs
- generate and persist schema metadata for client-driven UIs
- plug into an existing `DbContext` instead of requiring a separate API model
- provide a .NET client library for calling ChillSharp APIs
- integrate authentication and authorization features through `ChillSharp.Auth`

## Main Modules

- `ChillSharp`
  The core engine and API layer for DTO-based CRUD and query operations.

- `ChillSharp.Client`
  A client library for calling ChillSharp and ChillSharp.Auth endpoints from .NET applications.

- `ChillSharp.Schema`
  Schema services for reading and persisting DTO schema definitions.

- `ChillSharp.Auth`
  Authentication and authorization support, including users, roles, permission rules, Identity-backed accounts, refresh tokens, and protected API integration.

## Typical Use Cases

- rapid CRUD API development on top of an existing database model
- internal business applications with dynamic forms and grids
- admin backends that need metadata-driven UI generation
- applications that need field-level authorization and role-based access control

## How-To

- [HowTo/01-simple-blog-sqlite.md](/c:/source/personal/chill-sharp/chill-sharp/doc/HowTo/01-simple-blog-sqlite.md)
  Build a minimal SQLite-backed ChillSharp API around a single `Blog` entity using parameterless `ChillEntity` and `ChillProperty` attributes.

- [HowTo/02-blog-schema-labels.md](/c:/source/personal/chill-sharp/chill-sharp/doc/HowTo/02-blog-schema-labels.md)
  Add schema labels to the `Blog` model, enable `ChillSharp.Schema`, and read generated schema metadata from client and server code.

- [HowTo/03-authentication.md](/c:/source/personal/chill-sharp/chill-sharp/doc/HowTo/03-authentication.md)
  Protect a ChillSharp API with ASP.NET Core Identity, authenticate through `ChillSharpClient`, and reuse or refresh tokens for protected calls.

- [HowTo/04-blog-posts-one-to-many.md](/c:/source/personal/chill-sharp/chill-sharp/doc/HowTo/04-blog-posts-one-to-many.md)
  Model a `Blog`-`Post` one-to-many relation and fetch a blog with its posts in one `ChillSharpClient` query.

- [HowTo/05-docker-env-variables.md](/c:/source/personal/chill-sharp/chill-sharp/doc/HowTo/05-docker-env-variables.md)
  Build a reusable Docker image for a ChillSharp API and configure the database path and root user through environment variables.

## Documentation Index

- [ModelPreparation.md](/c:/source/personal/chill-sharp/chill-sharp/doc/ModelPreparation.md)
- [RegisterContext.md](/c:/source/personal/chill-sharp/chill-sharp/doc/RegisterContext.md)
- [AuthenticationModel/README.md](/c:/source/personal/chill-sharp/chill-sharp/doc/AuthenticationModel/README.md)
- [PermissionModel/README.md](/c:/source/personal/chill-sharp/chill-sharp/doc/PermissionModel/README.md)
