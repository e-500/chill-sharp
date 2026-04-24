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

## Install

```bash
npm install @chill-sharp/ui-core
```

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
