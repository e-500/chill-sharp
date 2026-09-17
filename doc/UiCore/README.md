# ChillSharp UI Core

Versione italiana: [Italiano](../it/UiCore/README.md)

`@chill-sharp/ui-core` is the shared Angular workspace UI for ChillSharp applications. It supplies the workspace shell, authenticated navigation, schema-driven forms and tables, CRUD tasks, permissions screens, and the menu-task registry. Application repositories keep their tenant branding, routes, runtime configuration, and client-owned extensions outside this package.

The package reads schema and menu metadata from the ChillSharp API. The server controls entity metadata and persisted menu entries; the UI Core renders the standard experience from that metadata. Use configuration JSON to tailor an individual menu task without copying shared UI code.

## Guides

- [CRUD menu configuration](./CRUD.md): configure a `CRUD` menu item and its JSON options.
- [Menu guide](../MenuGuide/README.md): persisted menu items and `MenuHierarchy` visibility.
- [Relation configuration](../MenuGuide/Relations.md): schema relations that UI Core can turn into nested CRUD tasks.
