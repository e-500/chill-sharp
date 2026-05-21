# ChillSharp UI Architecture

## Current Architecture

The UI architecture currently implemented under [`ui/`](/c:/source/personal/chill-sharp/chill-sharp/ui) is:

- [`chill-sharp-ui-core`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core): shared Angular library source
- npm package name: `@chill-sharp/ui-core`
- [`chill-sharp-ui-template`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template): Angular client shell template that depends on `@chill-sharp/ui-core`

The original application at `C:\source\personal\chill-sharp-ng-ui\` is the source implementation that was used to extract the shared UI into `chill-sharp-ui-core`. That original project is not part of the target architecture and should not be used as the place for future client-specific development.

The intended long-term model is:

- `chill-sharp-ui-core` is where shared ChillSharp UI behavior lives
- `chill-sharp-ui-template` is the starter shell for a new client repository
- each real client UI becomes its own repository based on the template and consumes `@chill-sharp/ui-core` through npm

## Why This Is the Right Split

This split avoids two unhealthy extremes:

1. keeping all clients in one Angular app with hardcoded client branches
2. copying the entire Angular app for every customer

With the current structure:

- shared functionality can evolve once in `chill-sharp-ui-core`
- each client shell stays thin and independent
- runtime configuration, branding, and local pages stay outside the core
- upgrading a client is explicit through npm versioning

## High-Level View

```text
ui/
  chill-sharp-ui-core
    source for the shared Angular library
    published as @chill-sharp/ui-core

  chill-sharp-ui-template
    starter Angular client shell
    depends on @chill-sharp/ui-core

future:
  customer-a-ui
    independent repo created from chill-sharp-ui-template
    installs @chill-sharp/ui-core

  customer-b-ui
    independent repo created from chill-sharp-ui-template
    installs @chill-sharp/ui-core
```

## Real Repository Responsibilities

### `chill-sharp-ui-core`

[`chill-sharp-ui-core`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core) is the shared Angular library source.

It already contains the real extracted ChillSharp UI implementation, including:

- auth shell and workspace shell layouts
- auth pages
- workspace infrastructure
- CRUD/task infrastructure
- permissions pages
- shared form and table components
- shared services
- shared models
- shared styles

It is exposed as `@chill-sharp/ui-core` through:

- [`package.json`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/package.json)
- [`src/public-api.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/public-api.ts)

### `chill-sharp-ui-template`

[`chill-sharp-ui-template`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template) is the starter Angular app shell for client repositories.

It currently contains:

- Angular bootstrap and routing
- dependency on `@chill-sharp/ui-core`
- runtime environment files
- client config files
- client theme overrides
- a sample client-owned page
- starter plugin and override folders
- starter GitHub CI/CD workflows

It is intentionally thin. It should orchestrate the shared UI, not reimplement it.

## Real `chill-sharp-ui-core` Structure

The current source structure is:

```text
chill-sharp-ui-core/
  src/
    lib/
      layouts/
      lib/
      models/
      pages/
      services/
      tasks/
      workspace/
    styles/
      core-theme.scss
    public-api.ts
```

### `layouts/`

Current shared layouts:

- [`auth-shell.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/layouts/auth-shell.component.ts)
- [`workspace-page.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/layouts/workspace-page.component.ts)

These are the two main visual shells:

- the auth experience for login/register/reset flows
- the main authenticated workspace experience

### `pages/`

Current shared pages include:

- auth pages:
  - [`login-page.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/pages/login-page.component.ts)
  - [`register-page.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/pages/register-page.component.ts)
  - [`reset-password-page.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/pages/reset-password-page.component.ts)
  - [`confirm-reset-page.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/pages/confirm-reset-page.component.ts)
- CRUD page:
  - [`crud-page.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/pages/crud/crud-page.component.ts)
- permissions pages:
  - [`permissions-page.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/pages/permissions/permissions-page.component.ts)
  - [`role-permission.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/pages/permissions/role-permission.component.ts)
  - [`user-permission.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/pages/permissions/user-permission.component.ts)
  - related dialogs/editors in the same folder

### `workspace/`

This folder contains the authenticated workspace UX itself:

- menu
- taskbar
- dialog host
- user profile dialog
- entity options dialog
- menu item dialog
- confirm dialog

This is one of the most important parts of the current architecture. The UI is not just a set of standalone pages; it is a workspace-driven application shell.

### `tasks/`

Current shared task infrastructure includes:

- [`crud-task.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/tasks/crud-task/crud-task.component.ts)

This is how shared workspace tasks are represented inside the workspace, separate from simple top-level routing.

### `services/`

Current shared services include:

- [`chill.service.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/services/chill.service.ts)
- [`workspace.service.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/services/workspace.service.ts)
- [`workspace-task-registry.service.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/services/workspace-task-registry.service.ts)
- [`workspace-dialog.service.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/services/workspace-dialog.service.ts)
- [`workspace-layout.service.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/services/workspace-layout.service.ts)
- [`workspace-toolbar.service.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/services/workspace-toolbar.service.ts)

These services are the real application backbone. In practice:

- `ChillService` handles auth/session, text lookups, schema/data operations, and general integration with the ChillSharp backend
- `WorkspaceService` manages workspace state, open tasks, active tasks, and menu-driven navigation
- `WorkspaceTaskRegistryService` resolves built-in and remote tasks
- the dialog, layout, and toolbar services control the workspace experience

### `lib/`

The `lib/` folder contains the reusable UI building blocks already extracted from the original Angular application, including:

- [`chill-form.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/lib/chill-form.component.ts)
- [`chill-table.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/lib/chill-table.component.ts)
- [`chill-polymorphic-input.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/lib/chill-polymorphic-input.component.ts)
- [`chill-polymorphic-output.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/lib/chill-polymorphic-output.component.ts)
- i18n label/button components
- schema/dialog helpers
- utility directives and option providers

This is the closest thing the current architecture has to a reusable design-system/data-entry layer.

### `models/`

The shared TypeScript models include:

- auth models
- menu models
- schema models
- workspace dialog models
- workspace task models

These contracts are part of the shared API surface of the core package.

### Shared Theme

The shared theme currently lives in:

- [`src/styles/core-theme.scss`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/styles/core-theme.scss)

This file is imported by the template and is the current base visual layer for the UI.

## Real Core Public Entry Points

The important public entry points today are:

- [`provideChillSharpUiCore()`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/provide-chill-sharp-ui-core.ts)
- [`CHILL_SHARP_UI_ROUTES`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/chill-sharp-ui.routes.ts)
- [`ChillSharpUiRootComponent`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/chill-sharp-ui-root.component.ts)

Their responsibilities are:

- `provideChillSharpUiCore()`: registers shared providers, ChillSharp client setup, and startup initializers
- `CHILL_SHARP_UI_ROUTES`: exposes the shared auth and workspace route tree
- `ChillSharpUiRootComponent`: provides the router host component for a client shell

## Real Shared Routes

The shared route tree currently provided by the core includes:

- `/login`
- `/register`
- `/reset-password`
- `/confirm-reset-password`
- `/confirm-reset-password/:token`
- `/workspace`
- `/workspace/:taskId`

This means that auth and workspace routing are already part of the core, not the client template.

## Real Built-In Workspace Tasks

The current built-in workspace task registry contains:

- `permissions`
- `crud`

These are registered in:

- [`workspace-task-registry.service.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/services/workspace-task-registry.service.ts)

So the real architecture is not a generic plugin shell yet. It already includes a concrete built-in workspace application model with predefined task types.

## Remote Workspace Task Loading

The current core also supports runtime-loaded remote tasks.

This is driven by:

- the global runtime config `__chillSharpUiRuntimeConfig__`
- `workspaceTaskSources`
- remote entry loading inside `WorkspaceTaskRegistryService`

This means the current extensibility story has two layers:

1. built-in shared tasks inside `@chill-sharp/ui-core`
2. remote workspace tasks that can be loaded at runtime from external sources

That is the real current extension model for workspace tasks.

## Auth and Session Model

The current auth/session behavior is shared inside the core.

Important current characteristics:

- auth pages live in the core
- auth guards live in the core route definition
- the ChillSharp Angular client is configured by the core providers
- session and user preferences are persisted in browser storage

Current shared storage keys are defined in:

- [`storage-keys.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/src/lib/storage-keys.ts)

This is important architecturally: auth is currently part of the shared product, not intended to be rewritten per client shell.

## Real `chill-sharp-ui-template` Structure

The current template structure is:

```text
chill-sharp-ui-template/
  .github/workflows/
  public/
    env.js
    runtime-config.js
    fonts/
  src/
    app/
      app.component.ts
      app.config.ts
      app.routes.ts
      core/
        overrides/
        plugins/
        providers/
      pages/
        client-home/
    assets/
      branding/
    config/
      app-config.ts
      runtime-config.ts
    environments/
      environment.ts
      environment.prod.ts
    styles.scss
```

## Real Template Responsibilities

The template currently owns the client shell concerns that should stay outside the core:

- app bootstrap
- app-level route composition
- client runtime config
- environment defaults
- branding assets
- client theme overrides
- client-owned pages
- future provider overrides
- CI/CD workflows

### Bootstrap

The template boots Angular from:

- [`src/main.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/src/main.ts)

and configures the app in:

- [`src/app/app.config.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/src/app/app.config.ts)

This file currently:

- calls `provideRouter(appRoutes)`
- calls `provideChillSharpUiCore()`
- registers client-level providers
- sets the document title from client config

### Root Component

The template root component is:

- [`src/app/app.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/src/app/app.component.ts)

It simply hosts:

- `<chill-sharp-ui-root />`

This is exactly what the architecture should do: the client shell stays thin and delegates the shared UI host to the core package.

### Route Composition

The template route composition is in:

- [`src/app/app.routes.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/src/app/app.routes.ts)

Today it does three things:

- imports `CHILL_SHARP_UI_ROUTES` from the core
- adds client-owned routes before the shared routes
- provides the final wildcard redirect

This is the current client-shell extension mechanism for top-level routes.

### Client Plugin Folder

The current plugin placeholder is:

- [`src/app/core/plugins/register-client-plugins.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/src/app/core/plugins/register-client-plugins.ts)

Today this is not a generic plugin contract. It is a simple client route registration point that currently adds:

- `/client-home`

So the real current architecture is:

- top-level client route extensions in the template
- shared workspace tasks in the core
- remote workspace tasks through runtime config

### Client Override Folder

The current override placeholder is:

- [`src/app/core/overrides/register-client-overrides.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/src/app/core/overrides/register-client-overrides.ts)

Right now it returns an empty provider array. That means provider-level override support is planned at the template layer, but there is not yet a formal set of exposed core override tokens documented in the architecture.

This is important: we should describe the current state honestly.

### Client-Owned Page Example

The template currently includes:

- [`src/app/pages/client-home/client-home.component.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/src/app/pages/client-home/client-home.component.ts)

This is an example of the kind of page that should stay in a client repo rather than moving into the shared core.

### Client Config and Runtime Files

The template currently contains:

- [`src/config/app-config.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/src/config/app-config.ts)
- [`src/config/runtime-config.ts`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/src/config/runtime-config.ts)
- [`public/env.js`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/public/env.js)
- [`public/runtime-config.js`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/public/runtime-config.js)

The important runtime globals today are:

- `CHILLSHARP_API_URL`
- `CHILLSHARP_UI_URL`
- `__clientUiTemplateRuntimeConfig__`
- `__chillSharpUiRuntimeConfig__`

This means:

- the template owns client-specific runtime settings
- the core reads the shared workspace runtime settings

### Theme Layer

The template imports the shared core theme from:

- `@chill-sharp/ui-core/styles/core-theme.scss`

and overrides CSS variables in:

- [`src/styles.scss`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/src/styles.scss)

This is the current real theme architecture:

- base theme in the core
- client visual adjustments in the template or client repo

### CI/CD

The template already contains starter GitHub workflows:

- [`ci.yml`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/.github/workflows/ci.yml)
- [`deploy.yml`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/.github/workflows/deploy.yml)

This matches the intended architecture: new client repos should start with a working pipeline skeleton.

## What Belongs in `chill-sharp-ui-core`

Based on the current extracted implementation, these things belong in the core:

- auth shell and auth pages
- workspace shell
- workspace task system
- built-in workspace tasks such as CRUD and permissions
- shared form/table/input/output components
- shared services and models
- route guards and shared routes
- shared theme and styling foundation
- backend integration with ChillSharp services

## What Belongs in `chill-sharp-ui-template` or Future Client Repos

These things belong outside the core:

- client app name
- client branding and logos
- client theme overrides
- client-only pages like `client-home`
- client-specific route additions
- environment-specific deployment values
- future provider overrides that are specific to one client
- CI/CD and deployment logic

## Current Extensibility Model

The real current extensibility model is not yet a full formal plugin platform.

Today we have:

1. client route composition in the template
2. provider override placeholders in the template
3. built-in workspace tasks in the core
4. remote workspace tasks loaded through runtime configuration

What we do not yet have as a fully formalized public architecture:

- a stable `provideChillUiPlugin(...)` API
- a documented set of override tokens for headers, menus, cards, or renderers
- a first-class menu plugin contract exposed from `@chill-sharp/ui-core`

So the architecture should be described as:

- already modular
- already split between core and client shell
- already extensible in several practical ways
- not yet finished as a polished public plugin framework

## Dependency and Upgrade Model

The template and future client repositories depend on the core package through npm:

```bash
npm install @chill-sharp/ui-core@1.0.126
```

This is the correct operational model:

- evolve shared behavior in `chill-sharp-ui-core`
- publish a new `@chill-sharp/ui-core` version
- upgrade each client shell explicitly

## Final Architectural Summary

The real architecture under `ui/` is:

```text
chill-sharp-ui-core
  shared Angular library source
  published as @chill-sharp/ui-core
  contains auth, workspace, CRUD, permissions,
  shared services, shared models, shared components, shared theme

chill-sharp-ui-template
  Angular shell template
  depends on @chill-sharp/ui-core
  contains bootstrap, route composition, client config, branding,
  theme overrides, client-owned pages, override placeholders, CI/CD

future client repos
  created from chill-sharp-ui-template
  install @chill-sharp/ui-core
  contain only client-specific config and customizations
```

This is the architecture that should guide future work:

- do not add client-specific behavior into `chill-sharp-ui-core`
- do not copy shared behavior from the old standard Angular app into each client repo
- keep evolving the shared workspace/auth/CRUD foundation in the core
- keep client shells thin and explicit
- grow the extension model from the current practical hooks toward a more formal plugin API over time
