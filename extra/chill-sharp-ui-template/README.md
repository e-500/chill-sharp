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

If this project was created through [`extra/publish.ps1`](/c:/source/personal/chill-sharp/extra/publish.ps1), the required ChillSharp `.tgz` packages are copied into a local `packages/` folder so remote build servers can restore them without `C:\source\npm-shared`.

Install dependencies with:

```bash
npm install
```

To copy the latest local ChillSharp `.tgz` archives from the shared npm folder into `packages/` and update `package.json`, run:

```powershell
.\upgrade.ps1
```

The script suggests `C:\source\npm-shared` first and asks you to confirm it or change the folder before continuing.

## Run locally

```bash
npm start
```

The template serves on `http://localhost:6202`.

## Core dependency

Upgrade the shared UI explicitly:

```bash
npm install @chill-sharp/ui-core@1.0.124
```

## Template structure

- `packages`: embedded ChillSharp package archives copied into generated client repos
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
