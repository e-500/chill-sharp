# HOW-TO: Creare Un'Immagine Docker E Configurarla Con Variabili D'Ambiente

Versione originale in inglese: [English](../../HowTo/05-docker-env-variables.md)

Questo esempio mostra come impacchettare una API ChillSharp in un'immagine Docker e configurarla a runtime tramite variabili d'ambiente invece di valori hardcoded.

## Obiettivo

Costruire una sola immagine container riusabile in ambienti diversi cambiando solo le variabili d'ambiente.

## 1. Leggere La Configurazione Da `IConfiguration`

ASP.NET Core mappa gia le variabili d'ambiente in `builder.Configuration`. Usa quello invece di hardcodare il path SQLite o le credenziali root.

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

## 2. Usare Variabili D'Ambiente Per L'Utente Root

`AddChillAuthIdentityApi(...)` conosce gia queste variabili:

```text
CHILLSHARP_AUTH_ROOT_USERNAME
CHILLSHARP_AUTH_ROOT_PASSWORD
CHILLSHARP_AUTH_ROOT_EMAIL
CHILLSHARP_AUTH_ROOT_DISPLAY_NAME
```

Quando `CreateChillAuthUserForRoot = true`, viene creato anche l'`AuthUser` root con `CanManagePermissions = true`.

## 3. Creare Il Dockerfile

Questa immagine compila l'applicazione una volta ed esegue il risultato con l'immagine runtime ASP.NET Core.

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

VOLUME ["/data"]
EXPOSE 8080

ENTRYPOINT ["dotnet", "MyBlogApp.dll"]
```

## 4. Build Dell'Immagine

```bash
docker build -t myblogapp:latest .
```

## 5. Avviare Il Container Con Variabili D'Ambiente

Questo esempio avvia l'API, persiste i dati SQLite in un volume Docker locale e crea l'amministratore root all'avvio.

```bash
docker run --rm -p 8080:8080 \
  -v myblogapp-data:/data \
  -e CHILLSHARP_DB_PATH=/data/blogging.db \
  -e CHILLSHARP_AUTH_ROOT_USERNAME=root \
  -e CHILLSHARP_AUTH_ROOT_PASSWORD=Pass123$ \
  -e CHILLSHARP_AUTH_ROOT_EMAIL=root@example.com \
  -e CHILLSHARP_AUTH_ROOT_DISPLAY_NAME="Root Administrator" \
  myblogapp:latest
```

L'applicazione sara disponibile a:

```text
http://localhost:8080/api/chill
http://localhost:8080/api/chill-auth
```

## 6. Esempio Facoltativo `docker compose`

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
      CHILLSHARP_AUTH_ROOT_USERNAME: root
      CHILLSHARP_AUTH_ROOT_PASSWORD: Pass123$
      CHILLSHARP_AUTH_ROOT_EMAIL: root@example.com
      CHILLSHARP_AUTH_ROOT_DISPLAY_NAME: Root Administrator
    volumes:
      - myblogapp-data:/data

volumes:
  myblogapp-data:
```

## Note

- Mantieni l'immagine generica e sposta i valori specifici dell'ambiente nella configurazione runtime.
- Persisti SQLite sotto un volume montato, altrimenti il database viene perso quando il container viene rimosso.
- In produzione, inietta i secret tramite piattaforma container o secret manager invece di hardcodarli nella history di `docker run` o nei file `compose`.

Esempio successivo: [Gestire una relazione Blog-Posts uno-a-molti e leggerla con una sola chiamata client](04-blog-posts-one-to-many.md)
