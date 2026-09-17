---
name: chillsharp-plugin-development
description: Develop client-owned ChillSharp UI plugins and overrides while keeping shared UI behavior upgradeable.
---

# ChillSharp UI Plugin Development

For Angular client features, keep application-owned code in the client repository and shared behavior in `@chill-sharp/ui-core`.

- Put feature routes/components under `src/app/core/plugins` and register routes from `register-client-plugins.ts`.
- Put provider replacements under `src/app/core/overrides` and return them from `register-client-overrides.ts`.
- Aggregate client providers through `provideClientTemplateProviders()`.
- Use public package exports; do not deep-import or patch shared package internals.
