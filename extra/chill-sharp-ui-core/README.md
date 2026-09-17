# @chill-sharp/ui-core

Private Angular package that contains the shared ChillSharp UI implementation extracted from the standard `chill-sharp-ng-ui` application.

## Scope

This package contains:

- shared layouts
- shared pages
- shared services
- workspace/task infrastructure
- reusable ChillSharp form and table components
- shared models and runtime helpers
- base theme styles

This package intentionally does not contain a client-specific shell application.

Client shells and client-owned plugins should live outside this package.

## Install

```bash
npm install @chill-sharp/ui-core
```

## Build And Release

See [`HOW_TO_BUILD.md`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/HOW_TO_BUILD.md) for the workflow to rebuild, version, and publish the library after a change.

## Plugin Documentation

`@chill-sharp/ui-core` currently supports client extension through template-owned routes, provider override placeholders, and runtime-loaded remote workspace tasks. It does not yet expose a single formal plugin registration API.

See [`../HOW_TO_CREATE_UI_CORE_PLUGIN_README.md`](../HOW_TO_CREATE_UI_CORE_PLUGIN_README.md) before documenting a client plugin or remote workspace task package.

## Theme import

Import the shared theme from your client shell:

```scss
@import '@chill-sharp/ui-core/styles/core-theme.scss';
```

## Package entry points

- `ChillSharpUiRootComponent`: router host component for the shell
- `CHILL_SHARP_UI_ROUTES`: default route tree for the standard ChillSharp UI
- `provideChillSharpUiCore()`: shared providers and initializers

## Status

This is the initial extraction of the current Angular implementation into a reusable `ui-core` package. Client shells should consume it and own only bootstrap, configuration, branding, theme overrides, and local plugins.
