# Client Plugins

Use this folder for client-owned extension registration.

Typical examples:

- client-only routes
- extra menu integration when the core exposes plugin hooks
- additional providers required by local features

`register-client-plugins.ts` currently demonstrates the simplest template-owned extension: a client route.

Included examples:

- [`hello-plugin`](hello-plugin/README.md): route plugin that renders `Hello {{name}}` from `hello-plugin/:name`

For plugin README guidance, see [`../../../../../HOW_TO_CREATE_UI_CORE_PLUGIN_README.md`](../../../../../HOW_TO_CREATE_UI_CORE_PLUGIN_README.md).
