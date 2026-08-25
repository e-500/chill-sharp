---
name: chillsharp-plugin-development
description: Develop client-owned ChillSharp UI plugins and overrides while keeping shared UI behavior upgradeable.
---

# ChillSharp UI Plugin Development

Use this skill for Angular client features that extend a ChillSharp UI client. Keep application-owned code in the client repository and shared behavior in `@chill-sharp/ui-core`.

- Add feature routes/components under `src/app/core/plugins` and register routes from `register-client-plugins.ts`.
- Add provider replacements under `src/app/core/overrides` and return them from `register-client-overrides.ts`.
- Aggregate client providers through `provideClientTemplateProviders()` in the app bootstrap.
- Use public package exports; do not deep-import or patch shared package internals.

Keep runtime API URLs, tenant settings, branding, and feature flags in client configuration. Test a clean build after registration changes.
