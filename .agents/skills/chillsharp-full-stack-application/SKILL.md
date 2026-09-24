---
name: chillsharp-full-stack-application
description: Build a connected ChillSharp application with an ASP.NET Core API, a data-management UI, and a user-facing frontend. Use when a request is to build a site, product, or application rather than only an API or only a client; use chill-cli to scaffold an initial workspace when needed.
---

# ChillSharp Full-Stack Application

Treat a request to build a new site or application in a ChillSharp workspace as a full-stack request unless the user explicitly limits it to an API, a UI, or a static prototype.

If no suitable ChillSharp workspace exists yet, create one first with `chill new <project-name> --both`. This scaffolds the API, UI, and agent guidance; use `--api` or `--ui` only when the requested scope is deliberately limited.

## Public package catalog

Use these published packages directly. Do not search the local filesystem, clone the ChillSharp repository, or recreate a client library to find an implementation that is already available here.

| Registry | Package | Use it for |
| --- | --- | --- |
| NuGet | `ChillSharp` | The ASP.NET Core backend, including the core API and bundled auth, schema, i18n, MCP, and attachment modules. |
| NuGet | `ChillSharp.Client` | A .NET application that consumes a ChillSharp API. |
| npm | `@chill-sharp/create-app` | Generate an Angular application shell with the standard ChillSharp UI dependencies. |
| npm | `@chill-sharp/ui-core` | The shared Angular management UI, layouts, CRUD pages, and providers. |
| npm | `@chill-sharp/ts-client` | A framework-neutral browser or TypeScript client. |
| npm | `@chill-sharp/ng-client` | Angular client helpers. |
| npm | `@chill-sharp/react-client` | React providers and hooks. |
| npm | `@chill-sharp/vue-client` | Vue plugin and composables. |
| npm | `@chill-sharp/chill-cli` | Create an agent-ready ChillSharp workspace; it is not a runtime dependency. |
| PyPI | `chill-sharp-py-client` | A Python client for a ChillSharp API. |

For a new Angular management UI, prefer `npx --yes @chill-sharp/create-app <app-name>` rather than building a replacement from local source. For another frontend stack, install its matching client package and connect it to the API. Use only the client family needed by the requested application; do not install every package in this table.

For example, add the backend with `dotnet add package ChillSharp`, a browser client with `npm install @chill-sharp/ts-client`, or a Python integration with `pip install chill-sharp-py-client`.

## Required delivered components

Deliver all of these as working parts of the same application:

1. **ASP.NET Core backend.** Create a runnable backend that uses the public `ChillSharp` NuGet package: an EF Core `DbContext` implementing `IChillContext`, ChillSharp entities and query types for the requested data, `AddChillApi`, and `MapChillApi`. Use the registration and model-preparation skills for the exact setup.
2. **Management UI.** Create an authenticated staff/admin experience that reads and writes the backend's real ChillSharp data. Use the ChillSharp schema/menu and client guidance when a ChillSharp UI client is appropriate. Do not substitute a design mock-up, static page, or local-only data editor.
3. **User-facing frontend.** Create the requested public/customer-facing experience and connect it to the same backend API. Its displayed and submitted data must come from real API calls, not fixtures, browser storage, or a parallel mock service.

Keep the frontend(s), API URL, CORS or development proxy, authentication flow, and database configuration coherent so they can be started locally together. For a deliberately unsecured demo, say that choice explicitly; otherwise protect staff data and management routes with ChillSharp authentication and authorization.

## Before declaring the work complete

Verify the backend builds and starts, a ChillSharp endpoint responds, the management UI can complete a representative create or update against the API, and the user-facing frontend can read that persisted data. Record the local start commands and URLs in the project README.

Do not stop after creating only a visual frontend, only a data model, or only a list of proposed next steps. If an existing project already supplies one of the three components, preserve it and implement the missing connected components.
