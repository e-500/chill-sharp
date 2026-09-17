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

Swagger is available in development at `/swagger`.

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
