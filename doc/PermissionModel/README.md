# Permission Model

Versione italiana: [Italiano](../it/PermissionModel/README.md)

This document describes the authorization model implemented by `ChillSharp.Auth`.

## Purpose

The permission model is designed to answer two questions consistently:

- can the current user perform an entity-level operation
- can the current user see or modify a specific property

The same model supports both:

- server-side enforcement
- client-side capability filtering

## Subjects

Permissions can be assigned to:

- a user
- a role

A user can belong to multiple roles.

## Resource Hierarchy

Permissions are evaluated against a three-level hierarchy:

```text
Module -> Entity -> Property
```

### Module

A module is a logical application area, for example:

- `Accounting`
- `Accounting.General`
- `Blog`
- `Blog.Admin`

Module names can be hierarchical.

### Entity

An entity is a Chill entity name inside a module, for example:

- `Blog`
- `Post`
- `AuthUser`

### Property

A property is a field on an entity, for example:

- `Title`
- `Author`
- `CanManagePermissions`

## Actions

### Entity actions

- `Query`
- `Create`
- `Update`
- `Delete`

### Property actions

- `See`
- `Modify`

Property permissions refine an already-allowed entity operation. They do not replace entity permissions.

## Effects

Each rule has one effect:

- `Allow`
- `Deny`

At the same evaluation level, `Deny` wins over `Allow`.

## Precedence

ChillSharp resolves rules in this order:

1. user property rules
2. user entity rules
3. user module rules
4. role property rules
5. role entity rules
6. role module rules
7. default deny

This combines two principles:

- user rules override role rules
- more specific rules override broader rules

## How Operations Are Evaluated

### Query

To query an entity:

1. the user must have entity `Query`
2. each returned property must also have `See`

If a property is not allowed, the server can remove, null, or mask it depending on the calling surface and implementation.

### Create

To create an entity:

1. the user must have entity `Create`
2. each provided property must have `Modify`

### Update

To update an entity:

1. the user must have entity `Update`
2. each changed property must have `Modify`

### Delete

To delete an entity:

1. the user must have entity `Delete`

Property rules do not matter for delete.

## Default Security Posture

The model is default-deny.

If no rule grants access, access is denied.

This is intentional. It prevents new entities or properties from becoming visible just because they were added to the model.

## Typical Rule Examples

Allow querying all blog entities in a module:

```text
Allow Query Module=Blog
```

Allow updating posts:

```text
Allow Update Module=Blog Entity=Post
```

Block edits to a sensitive property while allowing broader updates:

```text
Allow Update Module=Blog Entity=Post
Deny Modify Module=Blog Entity=Post Property=InternalNotes
```

## Auth Management API

`ChillSharp.Auth` now exposes a management-oriented API that separates:

- client-side capability evaluation
- server-side management of users, roles, and permission rules

### `GET chill-auth/get-permissions`

Returns the current logged-in user's authorization data as a structured payload:

- the current auth user
- the user's direct permissions
- the user's assigned roles
- for each assigned role, that role's permissions

Clients should use this payload to evaluate capabilities locally and:

- disable UI actions
- hide fields
- decide which editors or actions to render

The old public `evaluate/*` endpoints are removed. Evaluation is now expected to happen in the client libraries or UI layer by applying the same precedence rules described in this document.

### Management Endpoints Requiring `CanManagePermissions`

The following endpoints are reserved for privileged users only:

- `GET chill-auth/get-user-list`
- `GET chill-auth/get-user`
- `POST chill-auth/set-user`
- `GET chill-auth/get-role-list`
- `GET chill-auth/get-role`
- `POST chill-auth/set-role`

### `GET chill-auth/get-user-list`

Returns the full user list in a lightweight form suitable for dropdowns and selectors.

### `GET chill-auth/get-user`

Returns a single user as one structured object containing:

- user data
- assigned roles
- user-specific permissions

Role permissions are not expanded here because the UI can load them from the related role payload when needed.

### `POST chill-auth/set-user`

Creates or updates a user with:

- the full assigned role list
- the full list of user-specific permissions

The server synchronizes changes incrementally:

- adds missing roles and removes deleted roles
- adds missing permissions and removes deleted permissions

It does not clear and recreate the full set blindly.

### `GET chill-auth/get-role-list`

Returns the full role list in a lightweight form suitable for dropdowns and selectors.

### `GET chill-auth/get-role`

Returns a single role as one structured object containing:

- role data
- role-specific permissions
- users assigned to the role

### `POST chill-auth/set-role`

Creates or updates a role with:

- the full list of role permissions
- the full list of users assigned to the role

The server synchronizes changes incrementally:

- adds missing permissions and removes deleted permissions
- adds missing user-role assignments and removes deleted assignments

It does not clear and recreate the full set blindly.

## Server Enforcement

Clients may evaluate capabilities locally for UX purposes, but the server remains the source of truth.

Entity-level server authorization continues to be enforced through the existing ChillSharp ACL integration. Management endpoints additionally require `CanManagePermissions` when the caller is authenticated.

## Related Runtime Pieces

The permission model is backed by:

- `AuthUser`
- `AuthRole`
- `AuthUserRole`
- `AuthPermissionRule`

Management endpoints are exposed through `ChillSharp.Auth`.

For registration and account flows, see:

- [AuthenticationModel/README.md](../AuthenticationModel/README.md)


