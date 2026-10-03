---
name: chillsharp_plugin_development
description: Develop client-owned ChillSharp UI plugins and overrides while keeping shared UI behavior upgradeable.
---

# ChillSharp UI Plugin Development

Use this skill for Angular client features that extend a ChillSharp UI client. Keep application-owned code in the client repository and shared behavior in `@chill-sharp/ui-core`.

## Extension boundaries

- Add feature routes and route-owned components under `src/app/core/plugins`.
- Register them from `register-client-plugins.ts`.
- Add provider replacements under `src/app/core/overrides` and return them from `register-client-overrides.ts`.
- Aggregate client providers through `provideClientTemplateProviders()` in the app bootstrap.

Plugins should consume public exports from `@chill-sharp/ui-core`; do not deep-import package internals or edit copied shared implementation.

## Plugin checklist

- Define a stable route and lazy-load large features where appropriate.
- Keep branding, feature flags, runtime API URLs, and tenant settings in the client-owned config layer.
- Add a focused README when a plugin has registration, permission, or deployment assumptions.
- Test a clean build after changing routes/providers and verify the plugin works with the current local package archives.
