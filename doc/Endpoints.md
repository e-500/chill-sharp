# ChillSharp Endpoints

This document lists the HTTP, SignalR, and MCP endpoints exposed by the built-in ChillSharp API modules.

## Base Path

ChillSharp endpoints are mounted under a configurable API base path.

Default:

```text
/api
```

Configuration:

```text
CHILLSHARP_API_BASE_PATH=/api
```

or in code:

```csharp
builder.Services.AddChillApi<MyDbContext>(options =>
{
    options.ApiBasePath = "/api";
});

app.MapChillApi();
```

The examples below use the default `/api` base path. If you set `CHILLSHARP_API_BASE_PATH=/backend`, replace `/api` with `/backend`.

## Root And Diagnostics

These endpoints are registered by `MapChillApi()`.

| Method | Path | Description |
| --- | --- | --- |
| GET | `/api` | Basic ChillSharp health response, also matched when callers request `/api/`. |
| GET | `/api/test` | Basic ChillSharp health response. |
| GET | `/api/license` | Returns ChillSharp license and project metadata. |

## Core DTO API

These endpoints are enabled by `AddChillApi<TContext>()` and are available when the base Chill API is registered.

Base route:

```text
/api/chill
```

| Method | Path | Description |
| --- | --- | --- |
| POST | `/api/chill/query` | Executes a dynamic `ChillDtoQuery`. |
| POST | `/api/chill/lookup` | Performs a full-text lookup against the requested entity type. |
| POST | `/api/chill/find` | Finds one entity by type and GUID. |
| POST | `/api/chill/create` | Creates one entity from a `ChillDtoEntity`. |
| POST | `/api/chill/update` | Updates one entity from a `ChillDtoEntity`. |
| POST | `/api/chill/delete` | Deletes one entity identified by a `ChillDtoEntity`. |
| POST | `/api/chill/autocomplete` | Runs autocomplete logic for an entity or query DTO. |
| POST | `/api/chill/validate` | Runs validation for an entity or query DTO. |
| POST | `/api/chill/chunk` | Executes a list of Chill operations in one request. |

When entity ACL services are registered and the caller is authenticated, these endpoints can also enforce entity-level permissions.

## SignalR Notifications

The notification hub is registered by `MapChillApi()`.

| Protocol | Path | Description |
| --- | --- | --- |
| SignalR | `/api/notify` | Hub for entity change notifications. |

Hub methods:

| Method | Parameters | Description |
| --- | --- | --- |
| `Register` | `chillType`, optional `guid` | Subscribes the connection to all changes for a type or one entity. |
| `Unregister` | `chillType`, optional `guid` | Removes a previous subscription. |

Server-to-client method:

| Method | Description |
| --- | --- |
| `EntitiesChanged` | Sent when subscribed entities change. |

## Auth API

Enabled when `ChillApiOptions.EnableAuthApi` is `true` and the context implements `IChillAuthDbContext`.

Base route:

```text
/api/chill-auth
```

### Account Endpoints

| Method | Path | Auth | Description |
| --- | --- | --- | --- |
| POST | `/api/chill-auth/register` | Anonymous | Registers a new Identity account and returns tokens. |
| POST | `/api/chill-auth/login` | Anonymous | Authenticates and returns tokens. |
| POST | `/api/chill-auth/refresh` | Anonymous | Exchanges a refresh token for new tokens. |
| POST | `/api/chill-auth/logout` | Required | Revokes the current session. |
| POST | `/api/chill-auth/change-password` | Required | Changes the current user's password. |
| POST | `/api/chill-auth/request-password-reset` | Anonymous | Requests or generates a password reset token. |
| POST | `/api/chill-auth/reset-password` | Anonymous | Resets a password with a reset token. |

### Current User And Metadata

| Method | Path | Auth | Description |
| --- | --- | --- | --- |
| GET | `/api/chill-auth/get-permissions` | Optional | Returns direct, role, and role-derived permissions for the current user. |
| GET | `/api/chill-auth/get-user-list` | Management | Returns a simplified user list for management UIs. |
| GET | `/api/chill-auth/get-role-list` | Management | Returns a simplified role list for management UIs. |
| GET | `/api/chill-auth/get-module-list` | Management | Returns available logical modules. |
| GET | `/api/chill-auth/get-entity-list?module={module}` | Management | Returns entities for a module. |
| GET | `/api/chill-auth/get-query-list?module={module}` | Management | Returns queries for a module. |
| GET | `/api/chill-auth/get-property-list?chillType={type}` | Management | Returns properties for a Chill type. |

### Users

| Method | Path | Auth | Description |
| --- | --- | --- | --- |
| GET | `/api/chill-auth/users` | Management | Lists authorization users. |
| GET | `/api/chill-auth/users/{userGuid}` | Management | Gets one authorization user. |
| POST | `/api/chill-auth/users` | Management | Creates an authorization user. |
| PUT | `/api/chill-auth/users/{userGuid}` | Management | Updates an authorization user. |
| DELETE | `/api/chill-auth/users/{userGuid}` | Management | Deletes an authorization user. |
| GET | `/api/chill-auth/users/{userGuid}/roles` | Management | Lists roles assigned to a user. |
| PUT | `/api/chill-auth/users/{userGuid}/roles/{roleGuid}` | Management | Assigns a role to a user. |
| DELETE | `/api/chill-auth/users/{userGuid}/roles/{roleGuid}` | Management | Removes a role from a user. |

### Roles

| Method | Path | Auth | Description |
| --- | --- | --- | --- |
| GET | `/api/chill-auth/roles` | Management | Lists roles. |
| GET | `/api/chill-auth/roles/{roleGuid}` | Management | Gets one role. |
| POST | `/api/chill-auth/roles` | Management | Creates a role. |
| PUT | `/api/chill-auth/roles/{roleGuid}` | Management | Updates a role. |
| DELETE | `/api/chill-auth/roles/{roleGuid}` | Management | Deletes a role. |

### Permission Rules

| Method | Path | Auth | Description |
| --- | --- | --- | --- |
| GET | `/api/chill-auth/permissions` | Management | Lists permission rules, optionally filtered by `userGuid` or `roleGuid`. |
| GET | `/api/chill-auth/permissions/{ruleGuid}` | Management | Gets one permission rule. |
| POST | `/api/chill-auth/permissions` | Management | Creates a permission rule. |
| PUT | `/api/chill-auth/permissions/{ruleGuid}` | Management | Updates a permission rule. |
| DELETE | `/api/chill-auth/permissions/{ruleGuid}` | Management | Deletes a permission rule. |

`Management` means the endpoint is protected by `ChillAuthManagementAccessFilter`.

## Schema API

Enabled when `ChillApiOptions.EnableSchemaApi` is `true` and the context implements `IChillSchemaDbContext`.

Base route:

```text
/api/chill-schema
```

| Method | Path | Auth | Description |
| --- | --- | --- | --- |
| GET | `/api/chill-schema/get-schema?ChillType={type}&ChillViewCode={view}&CultureName={culture}` | Depends on global API protection | Gets one schema. |
| GET | `/api/chill-schema/get-schema-list?CultureName={culture}` | Depends on global API protection | Lists entity and query schema summaries. |
| POST | `/api/chill-schema/set-schema` | Schema management | Creates or updates a schema. |
| GET | `/api/chill-schema/get-entity-options?ChillType={type}` | Schema management | Gets schema options for one entity type. |
| POST | `/api/chill-schema/set-entity-options` | Schema management | Creates or updates entity options. |
| GET | `/api/chill-schema/get-menu?ParentGuid={guid}` | Depends on global API protection | Returns menu entries, filtered by auth metadata when available. |
| POST | `/api/chill-schema/set-menu` | Schema management | Creates or updates one menu item. |
| DELETE | `/api/chill-schema/delete-menu?MenuItemGuid={guid}` | Schema management | Deletes one menu item. |

`Schema management` means the endpoint is protected by `ChillSchemaManagementAccessFilter`.

## I18n API

Enabled when `ChillApiOptions.EnableI18nApi` is `true` and the context implements `IChillI18nDbContext`.

Base route:

```text
/api/chill-i18n
```

| Method | Path | Auth | Description |
| --- | --- | --- | --- |
| POST | `/api/chill-i18n/get-text` | Anonymous | Gets one localized text. |
| POST | `/api/chill-i18n/get-multiple-text` | Anonymous | Gets multiple localized texts. |
| PUT | `/api/chill-i18n/set-text` | Depends on global API protection | Creates or updates localized text. |

## Attachment API

Enabled when `ChillApiOptions.EnableAttachmentApi` is `true` and the context implements `IChillAttachmentDbContext`.

Base route:

```text
/api/chill-attachment
```

| Method | Path | Auth | Description |
| --- | --- | --- | --- |
| GET | `/api/chill-attachment/attachment/download?guid={guid}` | Anonymous for public files; authenticated for private files | Downloads an archived attachment. |
| POST | `/api/chill-attachment/attachment/upload` | Depends on global API protection | Uploads one or more files as multipart form data. |

Upload form fields:

| Field | Required | Description |
| --- | --- | --- |
| `attachToChillType` | Yes | Chill type of the entity the attachment belongs to. |
| `attachToGuid` | Yes | GUID of the entity the attachment belongs to. |
| `file` | Yes | One or more uploaded files. |
| `title` | No | Display title. Defaults to the filename without extension. |
| `description` | No | Optional attachment description. |
| `public` | No | Whether anonymous callers can download the file. |

## MCP API

Enabled when `ChillApiOptions.EnableMcpApi` is `true`, `ChillMcpOptions.Enabled` is `true`, and the context implements `IChillSchemaDbContext`.

Default route:

```text
/api/chill-mcp
```

The MCP endpoint is registered through `MapMcp(...)` from the Model Context Protocol ASP.NET Core SDK. Its HTTP behavior follows the MCP SDK transport contract.

You can override the MCP route directly:

```csharp
builder.Services.AddChillMcpApi<MyDbContext>(options =>
{
    options.RoutePattern = "/api/chill-mcp";
});
```

If `RoutePattern` is relative, for example `chill-mcp`, ChillSharp places it under the configured API base path.

## Protection Rules

`ChillApiOptions.ProtectedApi` applies authorization to the mapped controller endpoints and SignalR hub. Some endpoints explicitly allow anonymous access, such as auth login/register, i18n read endpoints, and public attachment downloads.

Module-specific management endpoints add stricter filters:

| Filter | Used By |
| --- | --- |
| `ChillAuthManagementAccessFilter` | Auth users, roles, permissions, and management metadata. |
| `ChillSchemaManagementAccessFilter` | Schema and menu write operations. |

Entity ACL checks can also apply to core DTO and attachment operations when an `IChillEntityAclService` is registered.
