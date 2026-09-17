# ChillSharp Menu

ChillSharp can store an application menu tree in the schema module and expose it through `/api/chill-schema/get-menu`.

## Menu Items

Each menu item contains:

- `Guid`: stable identifier.
- `PositionNo`: sort order among siblings.
- `Title`: display label.
- `Description`: optional longer text.
- `Parent`: optional parent menu item. `null` means a root item.
- `ComponentName`: frontend component identifier.
- `ComponentConfigurationJson`: optional component configuration.
- `MenuHierarchy`: optional visibility rule.

`MenuHierarchy` can be:

- empty or `null`
- one hierarchy code, for example `CONTENT`
- a comma-separated list, for example `CONTENT, REPORTS.MONTHLY`

## Endpoints

`GET /api/chill-schema/get-menu`

Returns root menu items.

`GET /api/chill-schema/get-menu?parentGuid={guid}`

Returns direct children of one menu item.

`POST /api/chill-schema/set-menu`

Creates or updates one menu item. The endpoint requires schema-management access.

`DELETE /api/chill-schema/delete-menu?menuItemGuid={guid}`

Deletes one menu item and all descendants. The endpoint requires schema-management access.

## Visibility Rules

Menu access is evaluated for logged users when auth services are available.

1. The server reads `MenuHierarchy` from the current user.
2. The server reads `MenuHierarchy` from all active roles assigned to the user.
3. Values are split by comma, trimmed, and merged into one effective set.
4. Empty values do not add anything to the effective set.
5. If the effective set contains `*`, the user can see all menu items.
6. If the effective set is empty, the user cannot see any menu item, including menu items with empty `MenuHierarchy`.
7. If the effective set has one or more hierarchy codes, the user can see menu items with empty `MenuHierarchy`.
8. If the effective set has one or more hierarchy codes, the user can see menu items whose own `MenuHierarchy` starts with one of those codes.

Menu items may also contain comma-separated `MenuHierarchy` values. A menu item is visible when at least one of its values starts with one effective user or role hierarchy code.

## Examples

User:

```text
CONTENT, REPORTS.MONTHLY
```

Active role:

```text
ADMIN.USERS
```

Effective set:

```text
CONTENT
REPORTS.MONTHLY
ADMIN.USERS
```

Visible menu item hierarchies:

```text
CONTENT
CONTENT.POSTS
REPORTS.MONTHLY
REPORTS.MONTHLY.SALES
ADMIN.USERS
ADMIN.USERS.INVITES
```

Not visible:

```text
REPORTS.YEARLY
ADMIN.ROLES
```

Visible because the user has at least one effective hierarchy:

```text
null
empty string
```

## Recommended Naming

Use stable, readable hierarchy codes:

- `ADMIN`
- `ADMIN.USERS`
- `ADMIN.ROLES`
- `CONTENT`
- `CONTENT.POSTS`
- `REPORTS.MONTHLY`
