# ChillSharp Documentation

This folder contains the reference documentation for ChillSharp.

`doc/HowTo` is intentionally left as the tutorial section. Use it for guided, incremental examples. The rest of this folder is the reference layer: concepts, registration, permissions, auth, and client-generation workflows.

## Documentation Map

- [ModelPreparation.md](./ModelPreparation.md)
  Prepare an EF Core model so ChillSharp can activate entities, run lifecycle hooks, generate schema metadata, and persist audit fields.

- [RegisterContext.md](./RegisterContext.md)
  Register ChillSharp modules against a host `DbContext` and map the API surface.

- [AuthenticationModel/README.md](./AuthenticationModel/README.md)
  Identity-backed account flows, auth-management endpoints, bootstrap strategies, and protected API setup.

- [PermissionModel/README.md](./PermissionModel/README.md)
  The permission model used by `ChillSharp.Auth`, including precedence, scopes, and how entity/property access is resolved.

- [ClientGeneration/README.md](./ClientGeneration/README.md)
  Generate client libraries from a ChillSharp host for TypeScript and Python using an OpenAPI document exposed by the host application.

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

- `ChillSharp.Auth`
  Authorization model, permission rules, role/user management, and optional ASP.NET Core Identity integration.

- `ChillSharp.I18n`
  Label and text lookup endpoints plus an in-memory i18n cache.

- `ChillSharp.Client`
  .NET client for ChillSharp and ChillSharp.Auth endpoints.

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
- `Label`, `ShortLabel`, `FullTextContent`
- `Checksum`, `LastUpdateUser`, `LastUpdateUtc`
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
- `LastUpdateUtc`

The audit logic is enforced through the `IChillEntity` interface path used by `ChillEngine`, so a derived class can override `OnAfterUpdate()` without bypassing the base audit update.

## API Surface

The core mapped API is exposed by:

- `app.MapChillApi()`

This maps the Chill API controllers and also includes:

- `/api/chill/test`
- `/api/chill/license`

Depending on which modules are registered, the same host can also expose:

- schema services through `ChillSharp.Schema`
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

