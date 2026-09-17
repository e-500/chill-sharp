# HOW-TO: Create a Docker Image and Configure It with Environment Variables

Versione italiana: [Italiano](../it/HowTo/05-docker-env-variables.md)

This example shows how to package a ChillSharp API into a Docker image and configure it at runtime through environment variables instead of hardcoded values.

## Goal

Build one container image that can be reused across environments by changing only environment variables.

## 1. Read configuration from environment-aware `IConfiguration`

ASP.NET Core already maps environment variables into `builder.Configuration`. Use that instead of hardcoding the SQLite path or root-user credentials.

For the DTO date/time mapper, ChillSharp also reads `CHILL_SHARP_SYSTEM_TIMEZONE` directly from the process environment. It should be an IANA id such as `Europe/Rome`.

This setting is used for ChillSharp `DateTime` handling and for UTC-to-local normalization of some `DateTimeOffset` inputs. `DateOnly` and `TimeOnly` keep normal .NET string output.

```csharp
using ChillSharp.Api;
using ChillSharp.Auth;
using ChillSharp.Auth.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var databasePath = builder.Configuration["CHILLSHARP_DB_PATH"] ?? "/data/blogging.db";

builder.Services.AddDbContext<BloggingContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<BloggingContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(ChillAuthIdentityDefaults.AuthenticationScheme)
    .AddChillAuthBearer();

builder.Services.AddAuthorization();

builder.Services.AddChillApi<BloggingContext>(options =>
{
    options.ProtectedApi = true;
});

builder.Services.AddChillAuthIdentityApi<BloggingContext, IdentityUser>(options =>
{
    options.ReturnPasswordResetTokensInResponse = false;
    options.InitializeRootUserOnStartup = true;
    options.CreateChillAuthUserForRoot = true;
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BloggingContext>();
    db.Database.EnsureCreated();
}

app.MapChillApi();
app.Run();
```

## 2. Use environment variables for the root user

`AddChillAuthIdentityApi(...)` already knows these variables:

```text
CHILLSHARP_AUTH_ROOT_USERNAME
CHILLSHARP_AUTH_ROOT_PASSWORD
CHILLSHARP_AUTH_ROOT_EMAIL
CHILLSHARP_AUTH_ROOT_DISPLAY_NAME
```

When `CreateChillAuthUserForRoot = true`, the root `AuthUser` is also created with `CanManagePermissions = true`.

## 3. Create the Dockerfile

This image builds the application once and runs it with the ASP.NET Core runtime image.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MyBlogApp.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/out ./

ENV ASPNETCORE_URLS=http://+:8080
ENV CHILLSHARP_DB_PATH=/data/blogging.db
ENV CHILL_SHARP_SYSTEM_TIMEZONE=Europe/Rome

VOLUME ["/data"]
EXPOSE 8080

ENTRYPOINT ["dotnet", "MyBlogApp.dll"]
```

## 4. Build the image

```bash
docker build -t myblogapp:latest .
```

## 5. Run the container with environment variables

This example starts the API, persists SQLite data in a local Docker volume, and creates the root administrator at startup.

```bash
docker run --rm -p 8080:8080 \
  -v myblogapp-data:/data \
  -e CHILLSHARP_DB_PATH=/data/blogging.db \
  -e CHILL_SHARP_SYSTEM_TIMEZONE=Europe/Rome \
  -e CHILLSHARP_AUTH_ROOT_USERNAME=root \
  -e CHILLSHARP_AUTH_ROOT_PASSWORD=Pass123$ \
  -e CHILLSHARP_AUTH_ROOT_EMAIL=root@example.com \
  -e CHILLSHARP_AUTH_ROOT_DISPLAY_NAME="Root Administrator" \
  myblogapp:latest
```

The application is then available at:

```text
http://localhost:8080/api/chill
http://localhost:8080/api/chill-auth
```

## 6. Optional `docker compose` example

```yaml
services:
  myblogapp:
    image: myblogapp:latest
    build: .
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_URLS: http://+:8080
      CHILLSHARP_DB_PATH: /data/blogging.db
      CHILL_SHARP_SYSTEM_TIMEZONE: Europe/Rome
      CHILLSHARP_AUTH_ROOT_USERNAME: root
      CHILLSHARP_AUTH_ROOT_PASSWORD: Pass123$
      CHILLSHARP_AUTH_ROOT_EMAIL: root@example.com
      CHILLSHARP_AUTH_ROOT_DISPLAY_NAME: Root Administrator
    volumes:
      - myblogapp-data:/data

volumes:
  myblogapp-data:
```

## Notes

- Keep the image generic and push environment-specific values into runtime configuration.
- Persist SQLite under a mounted volume, otherwise the database is lost when the container is removed.
- For production, inject secrets through your container platform or secret manager instead of hardcoding them in `docker run` history or `compose` files.

Next example: [Handle a one-to-many Blog-Posts relation and fetch it in one client call](05-blog-posts-one-to-many.md)

