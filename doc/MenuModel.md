# ChillSharp Menu Model

This document describes the menu model exposed by `ChillSharp.Schema`.

Use it when you want the UI menu tree to be stored in the backend instead of being hard-coded in the frontend.

## Purpose

The menu model lets the backend store:

- the tree structure of the application menu
- which UI component should open for a menu node
- optional JSON configuration for that component
- a `MenuHierarchy` prefix used to filter visible nodes per user or role

The schema module exposes menu-management endpoints through `/api/chill-schema`.

## Menu Item Structure

Each menu item contains:

- `Guid`
  Stable primary key.

- `Title`
  Required display text. Maximum length: 255 characters.

- `Description`
  Optional long description text.

- `Parent`
  Optional reference to another menu item. `null` means the node is a root item.

- `ComponentName`
  Required UI component identifier to open when the item is selected, for example `CRUD`.

- `ComponentConfigurationJson`
  Optional JSON string with component configuration.

- `MenuHierarchy`
  Required hierarchy prefix used for menu filtering.

## Tree Structure

The menu tree is built through the `Parent` relationship.

- root items have `Parent = null`
- child items point to their parent menu item
- the UI builds the full tree by loading root nodes first and then loading children for each node

Typical flow:

1. call `get-menu` without a parent to load root nodes
2. call `get-menu?parentGuid=...` to load the children of one node
3. repeat as needed while expanding the tree

## Endpoints

### `GET /api/chill-schema/get-menu`

Returns menu items for a specific parent level.

Query parameters:

- `parentGuid`
  Optional. When omitted, the endpoint returns root nodes. When provided, it returns only the direct children of that node.

Examples:

- `/api/chill-schema/get-menu`
  returns root nodes

- `/api/chill-schema/get-menu?parentGuid=8d0946dc-fc2b-4d95-b5ca-6f12d9618a5b`
  returns the direct children of that menu item

### `POST /api/chill-schema/set-menu`

Creates or updates one menu item.

Behavior:

- if `Guid` is empty, a new menu item is created
- if `Guid` already exists, the existing menu item is updated
- if `Parent.Guid` is provided, it must reference an existing menu item
- a menu item cannot be its own parent

Example payload:

```json
{
  "Guid": "00000000-0000-0000-0000-000000000000",
  "Title": "Posts",
  "Description": "Open the post management screen",
  "Parent": null,
  "ComponentName": "CRUD",
  "ComponentConfigurationJson": "{\"ChillType\":\"Model.Post\"}",
  "MenuHierarchy": "SECTION-A.POSTS"
}
```

### `DELETE /api/chill-schema/delete-menu`

Deletes one menu item and all of its descendants.

Query parameters:

- `menuItemGuid`
  Required. Identifies the root node of the subtree to remove.

Behavior:

- the target menu item must exist
- all child nodes and deeper descendants are deleted in the same operation
- sibling nodes and unrelated branches are not affected

Example:

- `/api/chill-schema/delete-menu?menuItemGuid=8d0946dc-fc2b-4d95-b5ca-6f12d9618a5b`
  deletes that node and the full subtree below it

## `MenuHierarchy`

`MenuHierarchy` is the string used to decide whether a menu item is visible to the current user.

The filtering rule is prefix-based.

Examples:

- menu item hierarchy: `SECTION-A`
- menu item hierarchy: `SECTION-A.POSTS`
- menu item hierarchy: `SECTION-B.REPORTS`

If a user or one of the user roles has:

- `*`
  full access to all menu items

- `SECTION-A`
  access to menu items whose `MenuHierarchy` starts with `SECTION-A`

- `SECTION-A.POSTS`
  access limited to menu items whose `MenuHierarchy` starts with `SECTION-A.POSTS`

This means:

- `SECTION-A` matches `SECTION-A`
- `SECTION-A` matches `SECTION-A.POSTS`
- `SECTION-A` matches `SECTION-A.REPORTS.MONTHLY`
- `SECTION-A` does not match `SECTION-B`

## User And Role Filtering

When `get-menu` is called by an authenticated user:

- the server reads `MenuHierarchy` from the current `AuthUser`
- the server also reads `MenuHierarchy` from all active roles assigned to that user
- all allowed prefixes are combined
- if any prefix is `*`, the full menu is returned
- otherwise only items whose `MenuHierarchy` starts with one allowed prefix are returned

If the authenticated user has no valid menu hierarchy, the returned menu is empty.

## Recommended Convention

Use a stable, readable naming convention for `MenuHierarchy`, for example:

- `ADMIN`
- `ADMIN.USERS`
- `ADMIN.ROLES`
- `CONTENT`
- `CONTENT.POSTS`
- `CONTENT.BLOGS`

This keeps authorization easy to reason about and makes role design simpler.
