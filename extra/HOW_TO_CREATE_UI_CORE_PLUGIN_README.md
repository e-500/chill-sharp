# How To Create A `ui-core` Plugin README

This guide explains how to document a client-owned extension that works with `@chill-sharp/ui-core`.

Use this when a client repository adds:

- top-level routes through the template plugin folder
- runtime-loaded workspace tasks through `workspaceTaskSources`
- client-specific providers or override placeholders

## Current Plugin Model

`@chill-sharp/ui-core` does not currently expose a single formal `provideChillUiPlugin(...)` API.

The supported extension points are:

1. client route composition in the shell template
2. provider override placeholders in the shell template
3. remote workspace tasks loaded from runtime configuration

A plugin README should describe which of those extension points it uses. Avoid documenting a generic plugin API that does not exist yet.

## Recommended File Name

For a client-owned plugin folder or package, use:

```text
README.md
```

If the repository contains more than one plugin, put a README beside each plugin:

```text
src/app/core/plugins/my-plugin/README.md
```

or, for a standalone remote workspace task package:

```text
my-workspace-task-plugin/README.md
```

## Required Sections

Every plugin README should include these sections.

### Purpose

Describe what the plugin adds in one or two paragraphs.

State whether it is:

- a client route plugin
- a remote workspace task plugin
- a provider override plugin
- a combination of these

### Compatibility

List the expected versions.

Example:

```md
## Compatibility

- Angular: 19.2.x
- @chill-sharp/ui-core: 1.0.131 or newer
- @chill-sharp/ng-client: 1.0.131 or newer
- @chill-sharp/ts-client: 1.0.131 or newer
```

### Installation

Describe how the client app gets the plugin.

For code that lives directly inside the client repo:

```md
This plugin is client-owned source code under `src/app/core/plugins`.
No npm install step is required.
```

For an npm package:

```powershell
npm install @client/my-ui-core-plugin
```

For a local package archive:

```powershell
npm install C:\source\npm-shared\client-my-ui-core-plugin-1.0.0.tgz
```

### Registration

Document the exact registration point.

For template-owned routes, show the route registration function:

```ts
import { Routes } from '@angular/router';
import { ReportsHomeComponent } from './reports-home.component';

export function getClientFeatureRoutes(): Routes {
  return [
    {
      path: 'reports',
      component: ReportsHomeComponent
    }
  ];
}
```

Then show that the shell composes those routes before the core routes:

```ts
export const appRoutes: Routes = [
  ...getClientFeatureRoutes(),
  ...coreRoutes,
  {
    path: '**',
    redirectTo: 'login'
  }
];
```

For provider overrides, show the provider registration:

```ts
import { Provider } from '@angular/core';

export function getClientOverrideProviders(): Provider[] {
  return [
    // Add client-specific providers here when ui-core exposes the matching token.
  ];
}
```

### Runtime Configuration

If the plugin is a remote workspace task, document the runtime config entry.

The client shell reads:

```js
globalThis.__chillSharpUiRuntimeConfig__ = globalThis.__chillSharpUiRuntimeConfig__ ?? {
  workspaceTaskSources: [
    'https://example.test/chillsharp/tasks/'
  ]
};
```

Each source can be either:

- a folder URL containing `workspace-tasks.index.json`
- a direct URL to a JSON index file

### Workspace Task Index

Remote workspace task plugins must publish an index file.

Default file name:

```text
workspace-tasks.index.json
```

Example:

```json
{
  "sourceName": "Client Reports",
  "tasks": [
    {
      "componentName": "client-reports",
      "title": "Client Reports",
      "description": "Open the client reporting workspace task.",
      "remoteEntry": "remoteEntry.js",
      "remoteName": "clientReportsPlugin",
      "exposedModule": "./ClientReportsTask",
      "exportedComponentName": "ClientReportsTaskComponent",
      "showInQuickLaunch": true
    }
  ]
}
```

Field notes:

- `componentName` is the task name used by menu items and workspace routes.
- `remoteEntry` is resolved relative to the source URL when it is not absolute.
- `remoteName` must match the browser global created by the remote bundle.
- `exposedModule` must match the exposed module name in the remote bundle.
- `exportedComponentName` defaults to `default` when omitted.
- `showInQuickLaunch` controls whether the task appears in quick launch.

### Component Contract

Document the component inputs and optional methods used by the task.

Workspace task components should support:

```ts
import { Component, input } from '@angular/core';
import type {
  WorkspaceTaskComponentInterface,
  WorkspaceTaskConfiguration
} from '@chill-sharp/ui-core';

@Component({
  selector: 'client-reports-task',
  standalone: true,
  template: `
    <section>
      <h1>{{ taskTitle() }}</h1>
    </section>
  `
})
export class ClientReportsTaskComponent implements WorkspaceTaskComponentInterface {
  static getComponentConfigurationJsonExample(): WorkspaceTaskConfiguration {
    return {
      reportCode: 'sales-summary'
    };
  }

  readonly visible = input(true);
  readonly componentConfiguration = input<WorkspaceTaskConfiguration | null>(null);
  readonly taskTitle = input('');
  readonly taskDescription = input('');
  readonly toolbarScope = input('');

  isAllSaved(): boolean {
    return true;
  }
}
```

Notes:

- `getComponentConfigurationJsonExample()` is useful for direct or built-in task registration. Remote workspace task indexes do not currently surface this example in the menu editor.
- `visible` lets the component pause expensive work when another workspace task is active.
- `componentConfiguration` receives parsed JSON from menu items or workspace route config.
- `taskTitle`, `taskDescription`, and `toolbarScope` are supplied for remote workspace tasks.
- `isAllSaved()` is optional, but should be implemented when the task can hold unsaved edits.
- `dialogResult()` and `canDialogSubmit()` are only needed when the same component is used inside a workspace dialog.

### Menu Usage

If the task is opened from a menu item, include the expected `componentName` and example configuration.

Example:

```json
{
  "componentName": "client-reports",
  "componentConfigurationJson": "{\"reportCode\":\"sales-summary\",\"defaultRange\":\"last-30-days\"}"
}
```

### Build And Publish

Describe how the plugin is built and where its deployable files go.

For a remote workspace task, the published output must include:

- `workspace-tasks.index.json`
- the remote entry file, usually `remoteEntry.js`
- any JavaScript, CSS, asset, or chunk files referenced by the remote entry

### Local Development

Include the local URLs needed by the client shell.

Example:

```js
globalThis.__chillSharpUiRuntimeConfig__ = {
  workspaceTaskSources: [
    'http://localhost:4301/'
  ]
};
```

Then run the client shell and the plugin host at the same time.

### Troubleshooting

Include plugin-specific checks.

Recommended baseline checks:

- The source URL is reachable from the browser.
- `workspace-tasks.index.json` returns valid JSON.
- `remoteEntry` resolves relative to the source URL.
- The browser global named by `remoteName` exists after `remoteEntry` loads.
- The remote module exports the component named by `exportedComponentName`.
- The task `componentName` matches the menu item or route that opens it.
- CORS headers allow the client shell to load the plugin files.

## README Template

Use this as the starting point for a plugin README.

```md
# <Plugin Name>

## Purpose

<Describe what this plugin adds and whether it is a client route, remote workspace task, provider override, or combination.>

## Compatibility

- Angular: 19.2.x
- @chill-sharp/ui-core: 1.0.131 or newer

## Installation

<Explain whether the plugin is client-owned source, an npm package, or a deployed remote bundle.>

## Registration

<Show the route registration, provider registration, or runtime workspace task source entry.>

## Runtime Configuration

<List required entries in `public/runtime-config.js`, if any.>

## Workspace Task Index

<Include `workspace-tasks.index.json` when this is a remote workspace task.>

## Component Contract

<Document inputs, configuration JSON, optional save/dialog methods, and any service dependencies.>

## Menu Usage

<Show the `componentName` and example `componentConfigurationJson` used by menu items.>

## Build And Publish

<List build commands and deployment output files.>

## Local Development

<List local ports, source URLs, and shell setup.>

## Troubleshooting

<List common failure checks for this plugin.>
```

## Related Docs

- [`extra/README.md`](README.md)
- [`extra/chill-sharp-ui-core/README.md`](chill-sharp-ui-core/README.md)
- [`extra/chill-sharp-ui-template/README.md`](chill-sharp-ui-template/README.md)
- [`extra/chill-sharp-ui-architecture/ARCHITECTURE.md`](chill-sharp-ui-architecture/ARCHITECTURE.md)
