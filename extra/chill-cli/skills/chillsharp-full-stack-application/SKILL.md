---
name: chillsharp-full-stack-application
description: Build a connected ChillSharp application with an ASP.NET Core API, a data-management UI, and a user-facing frontend. Use when a request is to build a site, product, or application rather than only an API or only a client.
---

# ChillSharp Full-Stack Application

Treat a request to build a new site or application in a ChillSharp workspace as a full-stack request unless the user explicitly limits it to an API, a UI, or a static prototype.

## Required delivered components

Deliver all of these as working parts of the same application:

1. **ASP.NET Core backend.** Create a runnable backend that uses ChillSharp: an EF Core `DbContext` implementing `IChillContext`, ChillSharp entities and query types for the requested data, `AddChillApi`, and `MapChillApi`. Use the registration and model-preparation skills for the exact setup.
2. **Management UI.** Create an authenticated staff/admin experience that reads and writes the backend's real ChillSharp data. Use the ChillSharp schema/menu and client guidance when a ChillSharp UI client is appropriate. Do not substitute a design mock-up, static page, or local-only data editor.
3. **User-facing frontend.** Create the requested public/customer-facing experience and connect it to the same backend API. Its displayed and submitted data must come from real API calls, not fixtures, browser storage, or a parallel mock service.

Keep the frontend(s), API URL, CORS or development proxy, authentication flow, and database configuration coherent so they can be started locally together. For a deliberately unsecured demo, say that choice explicitly; otherwise protect staff data and management routes with ChillSharp authentication and authorization.

## Before declaring the work complete

Verify the backend builds and starts, a ChillSharp endpoint responds, the management UI can complete a representative create or update against the API, and the user-facing frontend can read that persisted data. Record the local start commands and URLs in the project README.

Do not stop after creating only a visual frontend, only a data model, or only a list of proposed next steps. If an existing project already supplies one of the three components, preserve it and implement the missing connected components.
