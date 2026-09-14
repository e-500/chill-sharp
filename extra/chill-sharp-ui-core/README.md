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

## Service worker cache

`@chill-sharp/ui-core` ships `service-worker/chill-sharp-service-worker.js`.
The template and GDF load it from their client-owned `public/sw.js`; future package
upgrades update the shared behavior without replacing client options.

For an existing client, integrate once:

1. Upgrade `@chill-sharp/ui-core` to a package containing the worker (1.1.6 or later).
2. Add this entry to the Angular build assets (and test assets if applicable):
   `{"glob":"*.js","input":"node_modules/@chill-sharp/ui-core/service-worker","output":"/"}`.
3. Create/adapt `public/sw.js`:

   ```js
   self.CHILL_SHARP_SW_OPTIONS = {
     cachePrefix: 'my-app',
     cacheVersion: 'v2',
     cacheTimeoutMs: 10 * 60 * 1000,
     appShell: ['/', '/index.html'],
     legacyCacheNames: []
   };
   importScripts('./chill-sharp-service-worker.js');
   ```

4. Register `/sw.js` with `{ updateViaCache: 'none' }` on page load. Serve both
   worker scripts with `Cache-Control: no-cache, must-revalidate`, not immutable.
   HTTPS (or localhost) is required. Apps hosted below `/` must adjust shell URLs,
   registration URL and scope for their base path.

Do not register a second worker alongside an existing custom worker for the same
scope; integrate the shared worker into the client wrapper. Cache names belonging
to `cachePrefix` with old versions are removed on activation. Use `legacyCacheNames`
for explicitly named old caches with a different prefix. Unrelated caches survive.

Schema, schema-list and entity-options GETs expire at ten minutes, including
cross-origin API requests. Expired entries fetch from the network without using
the browser HTTP cache; expired metadata is not returned as an offline fallback.
Bearer responses vary by Authorization. Other APIs and runtime configuration are
network-only. Static runtime assets use the same lifetime; the precached app shell
remains available as a navigation fallback when offline.

Every `get-schema?update=true` request reaches the server. It clears all cached
schemas, lists and entity options before and after the request, as do `set-schema`
and `set-entity-options`. Reads during a write wait for it to finish, and old
in-flight reads cannot refill the cache after invalidation. The first ordinary GET
after the update or a page reload fetches a fresh schema and starts a new lifetime.

Run `npm run test:service-worker` in `extra/chill-sharp-ui-core` for the regression
tests covering expiration, invalidation, authorization and request races.
