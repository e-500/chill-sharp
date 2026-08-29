# ChillSharp Menu Guide

Versione italiana: [Italiano](../it/MenuGuide/README.md)

ChillSharp can store an application menu tree in the schema module rather than hard-coding it in the frontend. Menu management is exposed under `/api/chill-schema`.

## Menu item model

Each item has a stable `Guid`, `PositionNo`, `Title`, optional `Description`, optional `Parent`, `ComponentName`, optional `ComponentConfigurationJson`, and optional `MenuHierarchy`.

- `Parent = null` identifies a root item.
- Children reference their direct parent.
- Siblings sort by `PositionNo`, then `Title`, then `Guid`.
- `ComponentName` identifies the client component to open, for example `CRUD`.

```json
{
  "Guid": "00000000-0000-0000-0000-000000000000",
  "PositionNo": 10,
  "Title": "Posts",
  "Description": "Open the post management screen",
  "Parent": null,
  "ComponentName": "CRUD",
  "ComponentConfigurationJson": "{\"ChillType\":\"Model.Post\"}",
  "MenuHierarchy": "CONTENT.POSTS"
}
```

## Endpoints

- `GET /api/chill-schema/get-menu` returns root items.
- `GET /api/chill-schema/get-menu?parentGuid={guid}` returns one item's direct children.
- `POST /api/chill-schema/set-menu` creates an item when its `Guid` is empty or updates the matching item otherwise. A supplied parent must already exist, and an item cannot be its own parent. Updating an existing item with an empty `MenuHierarchy` preserves its stored value.
- `DELETE /api/chill-schema/delete-menu?menuItemGuid={guid}` deletes the selected item and its complete descendant subtree.

Load the tree one level at a time: request roots first, then request children as a branch is expanded.

## Visibility with `MenuHierarchy`

`MenuHierarchy` accepts one code or comma-separated codes. ChillSharp merges the comma-separated values from the current user and all active roles into an effective prefix set.

- `*` grants access to every menu item.
- With no effective prefix, no items are returned, including items whose hierarchy is empty.
- With at least one effective prefix, items with an empty hierarchy are visible.
- A populated item hierarchy is visible when it starts with at least one effective prefix.

For example, `CONTENT` grants `CONTENT`, `CONTENT.POSTS`, and `CONTENT.REPORTS.MONTHLY`, but not `ADMIN`. An item can expose more than one branch with `CONTENT, REPORTS.MONTHLY`.

Use stable dot-separated codes such as `ADMIN.USERS`, `CONTENT.POSTS`, and `REPORTS.MONTHLY`.
