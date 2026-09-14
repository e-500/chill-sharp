---
name: chillsharp-ui-template
description: Build and customize Angular ChillSharp client projects from the UI template, including plugins, overrides, i18n, permissions, and runtime configuration.
---

# ChillSharp UI Client Template

Use this skill for work in `chill-sharp-ui-template` or a client repository created from it.

The template owns runtime configuration, branding, routes, client plugins, and overrides. Shared controls, data access, schema rendering, auth, i18n primitives, and permission evaluation belong in `@chill-sharp/ui-core` and the ChillSharp client packages. Do not copy or patch shared package internals.

Keep `provideClientTemplateProviders()` in the app provider chain. Register feature routes in `src/app/core/plugins/register-client-plugins.ts`; register deliberate provider replacements in `src/app/core/overrides/register-client-overrides.ts`. Use public package APIs only.

Treat backend schema as the source of entity/property metadata rather than duplicating it in Angular. Use runtime API configuration from `public/env.js`. Request localized text with an explicit culture and preserve backend fallback behavior. UI permission checks can hide or disable operations, but the protected backend is the enforcement boundary.

Permission-sensitive features must account for `Module -> Entity -> Property`: entity `Query/Create/Update/Delete` is separate from property `See/Modify`. Never infer write access from a visible field.

Add plugins under the client-owned plugin folder, register providers through the override/provider points, keep tenant/feature flags/branding in runtime config, add focused tests, and run `npm run build` after registration changes. This `.agents/skills` directory travels with the template into client repositories.
