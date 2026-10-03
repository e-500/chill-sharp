---
name: chillsharp-full-stack-application
description: Build a connected ChillSharp application with an ASP.NET Core API and a UI backed by persisted data. Use for site, product, or application requests in a ChillSharp workspace, including internal dashboards; scaffold an initial workspace when needed.
---

# ChillSharp Full-Stack Application

Treat a request to build a site or application in a ChillSharp workspace as a request to build it with ChillSharp. Use the existing project as a starting point, even when it contains only a bare ASP.NET Core template or an earlier application written without ChillSharp. Add the ChillSharp backend to that project and connect the requested UI to it. A runnable plain ASP.NET Core application does not fulfill a ChillSharp application request.

If no workspace exists yet, use `chill new <project-name> --both` to create one. If the current folder was prepared with skills only, scaffold the requested application inside that folder. The generated API is a minimal `dotnet new webapi` project, not an implemented ChillSharp backend. Replace that starting point with the model, registration, endpoints, and authorization the application needs. If a package or setup problem blocks ChillSharp integration, report the blocker and the work completed; do not silently switch to a parallel non-ChillSharp implementation.

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

Deliver these working parts as the requested scope requires:

1. **ASP.NET Core backend.** Create a runnable backend that uses the public `ChillSharp` NuGet package: an EF Core `DbContext` implementing `IChillContext`, ChillSharp entities and query types for the requested data, `AddChillApi`, and `MapChillApi`. Use the registration and model-preparation skills for the exact setup.
2. **Management UI.** Create an authenticated experience for managing the backend's real ChillSharp data. Use the ChillSharp schema/menu and client guidance when a ChillSharp UI client is appropriate. Do not substitute a design mock-up, static page, or local-only data editor.
3. **User-facing UI.** Connect the requested researcher, customer, or public-facing workflow to the same backend API. An internal application can use one UI for management and day-to-day work; a separate public frontend is needed only when the requested users or workflows call for it. Displayed and submitted data must come from real API calls, not fixtures, browser storage, or a parallel mock service.

Keep the frontend(s), API URL, CORS or development proxy, authentication flow, and database configuration coherent so they can be started locally together. For a deliberately unsecured demo, say that choice explicitly; otherwise protect staff data and management routes with ChillSharp authentication and authorization. When access differs by dataset or record, enforce that boundary in server-side queries and writes for both UI and AI access; hiding controls in the UI is insufficient. Consult the permissions and MCP skills for the relevant mechanisms.

## Before declaring the work complete

Verify that the backend references the `ChillSharp` package, its context implements `IChillContext`, `AddChillApi` and `MapChillApi` run, and a ChillSharp endpoint responds. Verify that the requested UI reads and writes persisted data through the protected API. For restricted data, test that an approved user can complete the allowed workflow and cannot retrieve restricted records through direct API calls or AI access. Record the local start commands and URLs in the project README.

Do not stop after creating only a visual frontend, only a data model, or only a list of proposed next steps. Preserve existing components that satisfy these requirements and replace or extend those that do not.
