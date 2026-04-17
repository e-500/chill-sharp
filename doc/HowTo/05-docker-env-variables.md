# HOW-TO: Create a Docker Image and Configure It with Environment Variables

Versione italiana: [Italiano](../it/HowTo/05-docker-env-variables.md)

This example shows how to package a ChillSharp API into a Docker image and configure it at runtime through environment variables instead of hardcoded values.

## Goal

Build one container image that can be reused across environments by changing only environment variables.

## 1. Read configuration from environment-aware `IConfiguration`

ASP.NET Core already maps environment variables into `builder.Configuration`. Use that instead of hardcoding the SQLite path, module toggles, token lifetimes, SMTP settings, or root-user credentials.

For the DTO date/time mapper, ChillSharp also reads `CHILL_SHARP_SYSTEM_TIMEZONE` directly from the process environment. It should be an IANA id such as `Europe/Rome`.

This setting is used for ChillSharp `DateTime` handling and for UTC-to-local normalization of some `DateTimeOffset` inputs. `DateOnly` and `TimeOnly` keep normal .NET string output.

`ChillSharp.Attachment` also reads `CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT` directly. In containers, point it at a mounted volume such as `/attachments`.

```csharp
using ChillSharp.Api;
using ChillSharp.Auth;
using ChillSharp.Auth.Api;
using ChillSharp.Attachment.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var databasePath = builder.Configuration["CHILLSHARP_DB_PATH"] ?? "/data/blogging.db";
var attachmentArchiveRoot = builder.Configuration["CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT"] ?? "/attachments";

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
    options.ProtectedApi = GetBool("CHILLSHARP_API_PROTECTED", true);
    options.EnableSchemaApi = GetBool("CHILLSHARP_ENABLE_SCHEMA", true);
    options.EnableAuthApi = GetBool("CHILLSHARP_ENABLE_AUTH", true);
    options.EnableI18nApi = GetBool("CHILLSHARP_ENABLE_I18N", true);
    options.EnableMcpApi = GetBool("CHILLSHARP_ENABLE_MCP", true);
    options.EnableAttachmentApi = GetBool("CHILLSHARP_ENABLE_ATTACHMENT", false);
});

builder.Services.AddChillAuthIdentityApi<BloggingContext, IdentityUser>(options =>
{
    options.AccessTokenLifetime = TimeSpan.FromMinutes(GetInt("CHILLSHARP_AUTH_ACCESS_TOKEN_MINUTES", 20));
    options.RefreshTokenLifetime = TimeSpan.FromDays(GetInt("CHILLSHARP_AUTH_REFRESH_TOKEN_DAYS", 14));
    options.ReturnPasswordResetTokensInResponse = GetBool("CHILLSHARP_AUTH_RETURN_PASSWORD_RESET_TOKENS", false);
    options.SendPasswordResetEmails = GetBool("CHILLSHARP_AUTH_SEND_PASSWORD_RESET_EMAILS", true);
    options.InitializeRootUserOnStartup = GetBool("CHILLSHARP_AUTH_INITIALIZE_ROOT_USER", true);
    options.CreateChillAuthUserForRoot = GetBool("CHILLSHARP_AUTH_CREATE_ROOT_AUTH_USER", true);
    options.PasswordResetEmailSubject = builder.Configuration["CHILLSHARP_AUTH_PASSWORD_RESET_SUBJECT"] ?? "Reset your password";
    options.PasswordResetUrlBase = builder.Configuration["CHILLSHARP_AUTH_PASSWORD_RESET_URL"];
    options.SmtpHost = builder.Configuration["CHILLSHARP_SMTP_HOST"];
    options.SmtpPort = GetInt("CHILLSHARP_SMTP_PORT", 587);
    options.SmtpEnableSsl = GetBool("CHILLSHARP_SMTP_ENABLE_SSL", true);
    options.SmtpUserName = builder.Configuration["CHILLSHARP_SMTP_USERNAME"];
    options.SmtpPassword = builder.Configuration["CHILLSHARP_SMTP_PASSWORD"];
    options.PasswordResetFromEmail = builder.Configuration["CHILLSHARP_SMTP_FROM_EMAIL"];
    options.PasswordResetFromDisplayName = builder.Configuration["CHILLSHARP_SMTP_FROM_DISPLAY_NAME"];
});

builder.Services.Configure<ChillAttachmentOptions>(options =>
{
    options.ArchiveRoot = attachmentArchiveRoot;
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
Directory.CreateDirectory(attachmentArchiveRoot);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BloggingContext>();
    db.Database.EnsureCreated();
}

app.MapChillApi();
app.Run();

bool GetBool(string name, bool defaultValue)
{
    return bool.TryParse(builder.Configuration[name], out var value) ? value : defaultValue;
}

int GetInt(string name, int defaultValue)
{
    return int.TryParse(builder.Configuration[name], out var value) ? value : defaultValue;
}
```

The MCP and attachment modules are registered by `AddChillApi<TContext>()` only when the `DbContext` implements their required interfaces:

- `ChillSharp.Schema` and `ChillSharp.Mcp`: `IChillSchemaDbContext`
- `ChillSharp.Auth`: `IChillAuthDbContext`
- `ChillSharp.I18n`: `IChillI18nDbContext`
- `ChillSharp.Attachment`: `IChillAttachmentDbContext`

Keep `CHILLSHARP_ENABLE_ATTACHMENT=false` until your context exposes the attachment model and archive storage. Then switch it to `true` and keep `/attachments` mounted.

## 2. Use environment variables for modules and runtime settings

These names are a good container baseline for the current built-in modules:

```text
CHILLSHARP_DB_PATH
CHILL_SHARP_SYSTEM_TIMEZONE
CHILLSHARP_API_PROTECTED
CHILLSHARP_ENABLE_SCHEMA
CHILLSHARP_ENABLE_AUTH
CHILLSHARP_ENABLE_I18N
CHILLSHARP_ENABLE_MCP
CHILLSHARP_ENABLE_ATTACHMENT
CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT
```

`CHILLSHARP_ENABLE_*` variables are example-host variables: your app reads them from `IConfiguration` and maps them to `ChillApiOptions`. `CHILL_SHARP_SYSTEM_TIMEZONE`, `CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT`, and the root-user variables below are read directly by ChillSharp services too.

When enabled, the default endpoints include:

```text
http://localhost:8080/api/chill
http://localhost:8080/api/chill-auth
http://localhost:8080/api/chill-attachment
http://localhost:8080/api/chill-mcp
```

## 3. Use environment variables for the root user

`AddChillAuthIdentityApi(...)` already knows these variables:

```text
CHILLSHARP_AUTH_ROOT_USERNAME
CHILLSHARP_AUTH_ROOT_PASSWORD
CHILLSHARP_AUTH_ROOT_EMAIL
CHILLSHARP_AUTH_ROOT_DISPLAY_NAME
```

When `CreateChillAuthUserForRoot = true`, the root `AuthUser` is also created with `CanManagePermissions = true`.

## 4. Create the Dockerfile

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
ENV CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT=/attachments

VOLUME ["/data"]
VOLUME ["/attachments"]
EXPOSE 8080

ENTRYPOINT ["dotnet", "MyBlogApp.dll"]
```

## 5. Build the image

```bash
docker build -t myblogapp:latest .
```

## 6. Run the container with environment variables

This example starts the API, persists SQLite data and attachment files in local Docker volumes, and creates the root administrator at startup.

```bash
docker run --rm -p 8080:8080 \
  -v myblogapp-data:/data \
  -v myblogapp-attachments:/attachments \
  -e CHILLSHARP_DB_PATH=/data/blogging.db \
  -e CHILL_SHARP_SYSTEM_TIMEZONE=Europe/Rome \
  -e CHILLSHARP_API_PROTECTED=true \
  -e CHILLSHARP_ENABLE_SCHEMA=true \
  -e CHILLSHARP_ENABLE_AUTH=true \
  -e CHILLSHARP_ENABLE_I18N=true \
  -e CHILLSHARP_ENABLE_MCP=true \
  -e CHILLSHARP_ENABLE_ATTACHMENT=false \
  -e CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT=/attachments \
  -e CHILLSHARP_AUTH_ROOT_USERNAME=root \
  -e CHILLSHARP_AUTH_ROOT_PASSWORD=Pass123$ \
  -e CHILLSHARP_AUTH_ROOT_EMAIL=root@example.com \
  -e CHILLSHARP_AUTH_ROOT_DISPLAY_NAME="Root Administrator" \
  myblogapp:latest
```

With the values above, the application is then available at:

```text
http://localhost:8080/api/chill
http://localhost:8080/api/chill-auth
http://localhost:8080/api/chill-mcp
```

Set `CHILLSHARP_ENABLE_ATTACHMENT=true` when the host context implements `IChillAttachmentDbContext`; then the attachment API is available at `http://localhost:8080/api/chill-attachment`.

## 7. Optional `docker compose` example

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
      CHILLSHARP_API_PROTECTED: "true"
      CHILLSHARP_ENABLE_SCHEMA: "true"
      CHILLSHARP_ENABLE_AUTH: "true"
      CHILLSHARP_ENABLE_I18N: "true"
      CHILLSHARP_ENABLE_MCP: "true"
      CHILLSHARP_ENABLE_ATTACHMENT: "false"
      CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT: /attachments
      CHILLSHARP_AUTH_ROOT_USERNAME: root
      CHILLSHARP_AUTH_ROOT_PASSWORD: Pass123$
      CHILLSHARP_AUTH_ROOT_EMAIL: root@example.com
      CHILLSHARP_AUTH_ROOT_DISPLAY_NAME: Root Administrator
    volumes:
      - myblogapp-data:/data
      - myblogapp-attachments:/attachments

volumes:
  myblogapp-data:
  myblogapp-attachments:
```

## Notes

- Keep the image generic and push environment-specific values into runtime configuration.
- Persist SQLite under a mounted volume, otherwise the database is lost when the container is removed.
- For production, inject secrets through your container platform or secret manager instead of hardcoding them in `docker run` history or `compose` files.

Next example: [Handle a one-to-many Blog-Posts relation and fetch it in one client call](05-blog-posts-one-to-many.md)

