# ChillSharp.Template

Starter backend project for ChillSharp package users.

This project keeps the backend intentionally small:

- one `Example` entity with `Code` and `Title`
- one `ExampleQuery`
- one partial logic file showing Chill hooks
- one `DbSet<Example>` inside the context
- SQLite + Swagger + Chill schema/MCP support enabled out of the box

## Run

```powershell
dotnet run --project .\ChillSharp.Template\ChillSharp.Template.csproj
```

The template listens on `https://localhost:6002` in development. The built-in ChillSharp status endpoint is available at `https://localhost:6002/api`, and the Chill API base URL is `https://localhost:6002/api/chill`.

Swagger is available in development at `https://localhost:6002/swagger`.

## Upgrade local ChillSharp package

To copy the latest local `ChillSharp.<version>.nupkg` from the shared NuGet folder into `nupkgs/` and update the package reference, run:

```powershell
.\upgrade.ps1
```

The script suggests `C:\source\nuget-shared` first and asks you to confirm it or change the folder before continuing.

## Default Chill types

- `Model.Example`
- `Query.ExampleQuery`

## Files to extend first

- `Program.cs`: host registration
- `ChillSharpTemplateContext.cs`: shared context configuration
- `Model/Context/ChillSharpTemplateContext.Example.cs`: example `DbSet`
- `Model/Example.cs`: entity properties
- `Model/Logic/Example.cs`: entity behavior
- `Model/Query/ExampleQuery.cs`: query filters
