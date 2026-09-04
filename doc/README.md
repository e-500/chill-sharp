# ChillSharp Documentation

Versione italiana: [Italiano](./it/README.md)

This folder contains the reference documentation for ChillSharp.

`doc/HowTo` is intentionally left as the tutorial section. Use it for guided, incremental examples. The rest of this folder is the reference layer: concepts, registration, permissions, auth, and client-generation workflows.

## Documentation Map

- [ModelPreparation.md](./ModelPreparation.md)
  Prepare an EF Core model so ChillSharp can activate entities, run lifecycle hooks, generate schema metadata, and persist audit fields.

- [ReferenceExistence.md](./ReferenceExistence.md)
  Check whether an EF Core reference has foreign-key values without loading its related entity, including databases without enforced FK constraints.

- [RegisterContext.md](./RegisterContext.md)
  Register ChillSharp modules against a host `DbContext` and map the API surface.

- [Configuration/README.md](./Configuration/README.md)
  Quick reference for the example host configuration options and their environment variables.

- [AttachmentModel/README.md](./AttachmentModel/README.md)
  Attachment entity model, archive layout, configuration, and upload/download endpoint behavior.

- [DateTimeSerialization.md](./DateTimeSerialization.md)
  How ChillSharp serializes and parses `DateTimeOffset`, `DateTime`, `DateOnly`, and `TimeOnly`, including comparisons with default ASP.NET Core behavior.

- [DateTimePolicy/README.md](./DateTimePolicy/README.md)
  Current DTO policy for `DateTime` and `DateTimeOffset`, including configured timezone handling, UTC normalization, and server-managed audit fields.

- [AuthenticationModel/README.md](./AuthenticationModel/README.md)
  Identity-backed account flows, auth-management endpoints, bootstrap strategies, and protected API setup.

- [CurrentUserPreferences.md](./CurrentUserPreferences.md)
  Cached authenticated-user culture, time-zone, date-format, and number-format preferences for `IChillContext` and entity lifecycle hooks.

- [MenuGuide/README.md](./MenuGuide/README.md)
  Backend-managed menu tree, menu endpoints, and `MenuHierarchy` filtering rules.

- [MenuGuide/Relations.md](./MenuGuide/Relations.md)
  Configure EF Core and ChillSharp schema metadata for one-to-many relations.

- [UiCore/README.md](./UiCore/README.md)
  Shared Angular UI scope and CRUD menu-task configuration.

- [PermissionModel/README.md](./PermissionModel/README.md)
  The permission model used by `ChillSharp.Auth`, including precedence, scopes, and how entity/property access is resolved.

- [ComplianceGuide/README.md](./ComplianceGuide/README.md)
  How ChillSharp supports common security and compliance controls such as validation, least-privilege authorization, and audit metadata.

- [AIAssistedDevelopment/README.md](./AIAssistedDevelopment/README.md)
  How ChillSharp supports AI-assisted development by reducing repetitive CRUD code, stabilizing the API surface, and keeping model growth more uniform.

- [Mcp/README.md](./Mcp/README.md)
  Model Context Protocol module, MCP tool behavior, registration, `EnableMCP`, and guidance for preparing an AI-friendly `DbContext`.

- [ClientGeneration/README.md](./ClientGeneration/README.md)
  Generate client libraries from a ChillSharp host for TypeScript and Python using an OpenAPI document exposed by the host application.

- [ChillSharpClient.md](./ChillSharpClient.md)
  Use the .NET `ChillSharp.Client` library for core entity operations, auth, schema/menu, i18n, and attachments.

- [../ext/chill-sharp-ts-client/README.md](../ext/chill-sharp-ts-client/README.md)
  Generic TypeScript client for ChillSharp services.

- [../ext/chill-sharp-react-client/README.md](../ext/chill-sharp-react-client/README.md)
  React provider and hooks built on top of the generic TypeScript client.

- [../ext/chill-sharp-vue-client/README.md](../ext/chill-sharp-vue-client/README.md)
  Vue plugin and composables built on top of the generic TypeScript client.

- [../ext/chill-sharp-ng-client/README.md](../ext/chill-sharp-ng-client/README.md)
  Angular DI helpers and RxJS service built on top of the generic TypeScript client.

- [../ext/chill-sharp-py-client/README.md](../ext/chill-sharp-py-client/README.md)
  Generic Python client for ChillSharp services.

## Main Modules

- `ChillSharp`
  Core entity engine, DTO engine, and HTTP API surface.

- `ChillSharp.Schema`
  Schema generation, persistence, and schema cache.

- `ChillSharp.Attachment`
  Attachment entity model, archive storage, and upload/download endpoints.

- `ChillSharp.Auth`
  Authorization model, permission rules, role/user management, and optional ASP.NET Core Identity integration.

- `ChillSharp.I18n`
  Label and text lookup endpoints plus an in-memory i18n cache.

- `ChillSharp.Client`
  .NET client for ChillSharp and ChillSharp.Auth endpoints.

- `ChillSharp.Mcp`
  MCP server module built on the official C# SDK, exposing ChillSharp schema discovery and query tools for AI clients.

- `ext/chill-sharp-ts-client`
  Generic TypeScript client package.

- `ext/chill-sharp-react-client`
  React integration package layered on top of the TypeScript client.

- `ext/chill-sharp-vue-client`
  Vue integration package layered on top of the TypeScript client.

- `ext/chill-sharp-ng-client`
  Angular integration package layered on top of the TypeScript client.

- `ext/chill-sharp-py-client`
  Generic Python client package.

## Core Concepts

### `IChillContext`

Your EF Core context must implement `IChillContext`. It defines:

- the Chill type prefix used for dynamic activation
- the primary and secondary cultures used to interpret schema labels
- the current user name used by entity audit tracking

Different contexts can coexist with different values. ChillSharp does not assume a single global configuration.

### `ChillEntity`

`ChillEntity` is the recommended base class for model types exposed through ChillSharp. It already provides:

- `Guid`
- `Position`
- `Label`, `ShortLabel`, `FullTextContent`
- `Checksum`, `LastUpdateUser`, `LastUpdate`, `LastUpdateUtcOffset`
- default lifecycle behavior

Lifecycle hooks are:

- `OnCreate`
- `OnUpdate`
- `OnAfterUpdate`
- `OnDelete`
- `OnAfterDelete`
- `OnSelect`
- `OnInflate`
- `OnAutocomplete`

### Schema Metadata

`ChillEntityAttribute` and `ChillPropertyAttribute` provide:

- stable unique keys
- `PrimaryLanguageLabel`
- `SecondaryLanguageLabel`

When ChillSharp builds a schema, it resolves those labels using:

- `CultureInfo.CurrentUICulture`
- `IChillContext.GetPrimaryCultureName()`
- `IChillContext.GetSecondaryCultureName()`

### Audit Fields

After updates, ChillSharp automatically stores:

- `Checksum`
- `LastUpdateUser`
- `LastUpdate`
- `LastUpdateUtcOffset`

The audit logic is enforced through the `IChillEntity` interface path used by `ChillEngine`, so a derived class can override `OnAfterUpdate()` without bypassing the base audit update.

### Query Ordering

`ChillDtoQuery` includes an `Ordering` object that mirrors `Pagination`:

- `PropertyName`
- `Direction`

If the client does not send an explicit ordering, ChillSharp applies `Position` by default. `Position` is part of both `ChillEntity` and `ChillDtoEntity`, and defaults to `0`.

When `PropertyName` points to a referenced Chill entity, ordering is applied using the referenced entity `Label`. This keeps generic list screens readable without requiring clients to know the foreign-key internals.

## API Surface

The core mapped API is exposed by:

- `app.MapChillApi()`

This maps the Chill API controllers and also includes:

- `/api/chill/query`
- `/api/chill/lookup`
- `/api/chill/test`
- `/api/chill/license`

Depending on which modules are registered, the same host can also expose:

- schema services through `ChillSharp.Schema`
- attachment upload/download services through `ChillSharp.Attachment`
- auth/account and permission-management services through `ChillSharp.Auth`
- i18n text endpoints through `ChillSharp.I18n`

## Reference vs How-To

Use this split consistently:

- `doc/HowTo`
  Step-by-step tutorials. Keep these focused and task-oriented.

- the rest of `doc/`
  Reference documentation. Use these files when you need the model, registration, architecture, permission rules, or integration details.

## How-To

The existing tutorials remain unchanged:

- [HowTo/01-simple-blog-sqlite.md](./HowTo/01-simple-blog-sqlite.md)
- [HowTo/02-blog-schema-labels.md](./HowTo/02-blog-schema-labels.md)
- [HowTo/03-authentication.md](./HowTo/03-authentication.md)
- [HowTo/04-blog-posts-one-to-many.md](./HowTo/04-blog-posts-one-to-many.md)
- [HowTo/05-docker-env-variables.md](./HowTo/05-docker-env-variables.md)
- [HowTo/06-chunk-transactions-autocomplete.md](./HowTo/06-chunk-transactions-autocomplete.md)


