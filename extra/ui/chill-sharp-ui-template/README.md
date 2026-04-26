# ChillSharp UI Template

Angular client shell template for ChillSharp UI applications.

## Purpose

This template is the starting point for a client repository. It depends on `@chill-sharp/ui-core` and owns:

- client runtime configuration
- client branding and assets
- client theme overrides
- client-specific pages
- future plugin and override registrations
- CI/CD pipelines

It does not copy the shared implementation from the standard UI. Shared behavior should remain in `@chill-sharp/ui-core`.

## Install

1. Configure access to the private npm registry that hosts `@chill-sharp/ui-core`
2. Install dependencies

```bash
npm install
```

## Run locally

```bash
npm start
```

The template serves on `http://localhost:6202`.

## Core dependency

Upgrade the shared UI explicitly:

```bash
npm install @chill-sharp/ui-core@1.0.111
```

## Template structure

- `src/app`: shell bootstrap and client-owned routes
- `src/config`: client config contracts and defaults
- `src/assets/branding`: client logos and brand assets
- `src/app/core/plugins`: extension registration placeholders
- `src/app/core/overrides`: override registration placeholders
- `.github/workflows`: starter CI/CD pipelines

## Runtime configuration

Two public runtime files are included:

- `public/env.js`: API/UI URLs
- `public/runtime-config.js`: workspace sources and client feature flags

These are safe places for environment-specific deployment replacement.
