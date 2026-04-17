# HOW-TO: Creare Un'Immagine Docker E Configurarla Con Variabili D'Ambiente

Versione originale in inglese: [English](../../HowTo/05-docker-env-variables.md)

Questo esempio mostra come impacchettare una API ChillSharp in un'immagine Docker e configurarla a runtime tramite variabili d'ambiente invece di valori hardcoded.

## Obiettivo

Costruire una sola immagine container riusabile in ambienti diversi cambiando solo le variabili d'ambiente.

## 1. Leggere La Configurazione Da `IConfiguration`

ASP.NET Core mappa gia le variabili d'ambiente in `builder.Configuration`. Usa quello invece di hardcodare il path SQLite, i toggle dei moduli, le durate dei token, le impostazioni SMTP o le credenziali root.

Per il mapper DTO di date e orari, ChillSharp legge anche `CHILLSHARP_SYSTEM_TIMEZONE` direttamente dall'ambiente del processo. Deve essere un id IANA, per esempio `Europe/Rome`.

Questa impostazione viene usata per la gestione `DateTime` di ChillSharp e per la normalizzazione UTC-locale di alcuni input `DateTimeOffset`. `DateOnly` e `TimeOnly` mantengono l'output stringa standard di .NET.

`ChillSharp.Attachment` legge direttamente anche `CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT`. Nei container conviene puntarlo a un volume montato, per esempio `/attachments`.

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

I moduli MCP e Attachment vengono registrati da `AddChillApi<TContext>()` solo quando il `DbContext` implementa le interfacce richieste:

- `ChillSharp.Schema` e `ChillSharp.Mcp`: `IChillSchemaDbContext`
- `ChillSharp.Auth`: `IChillAuthDbContext`
- `ChillSharp.I18n`: `IChillI18nDbContext`
- `ChillSharp.Attachment`: `IChillAttachmentDbContext`

Mantieni `CHILLSHARP_ENABLE_ATTACHMENT=false` finche il context non espone il modello attachment e lo storage di archivio. Poi impostalo a `true` e lascia `/attachments` montato.

## 2. Usare Variabili D'Ambiente Per Moduli E Runtime

Questi nomi sono una buona base container per i moduli integrati attuali:

```text
CHILLSHARP_DB_PATH
CHILLSHARP_SYSTEM_TIMEZONE
CHILLSHARP_API_PROTECTED
CHILLSHARP_ENABLE_SCHEMA
CHILLSHARP_ENABLE_AUTH
CHILLSHARP_ENABLE_I18N
CHILLSHARP_ENABLE_MCP
CHILLSHARP_ENABLE_ATTACHMENT
CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT
```

Le variabili `CHILLSHARP_ENABLE_*` sono variabili dell'host di esempio: l'app le legge da `IConfiguration` e le mappa su `ChillApiOptions`. `CHILLSHARP_SYSTEM_TIMEZONE`, `CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT` e le variabili dell'utente root sotto sono lette direttamente anche dai servizi ChillSharp.

Quando abilitati, gli endpoint predefiniti includono:

```text
http://localhost:8080/api/chill
http://localhost:8080/api/chill-auth
http://localhost:8080/api/chill-attachment
http://localhost:8080/api/chill-mcp
```

## 3. Usare Variabili D'Ambiente Per L'Utente Root

`AddChillAuthIdentityApi(...)` conosce gia queste variabili:

```text
CHILLSHARP_AUTH_ROOT_USERNAME
CHILLSHARP_AUTH_ROOT_PASSWORD
CHILLSHARP_AUTH_ROOT_EMAIL
CHILLSHARP_AUTH_ROOT_DISPLAY_NAME
```

Quando `CreateChillAuthUserForRoot = true`, viene creato anche l'`AuthUser` root con `CanManagePermissions = true`.

## 4. Creare Il Dockerfile

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
ENV CHILLSHARP_SYSTEM_TIMEZONE=Europe/Rome
ENV CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT=/attachments

VOLUME ["/data"]
VOLUME ["/attachments"]
EXPOSE 8080

ENTRYPOINT ["dotnet", "MyBlogApp.dll"]
```

## 5. Build Dell'Immagine

```bash
docker build -t myblogapp:latest .
```

## 6. Avviare Il Container Con Variabili D'Ambiente

Questo esempio avvia l'API, persiste i dati SQLite e i file allegati in volumi Docker locali e crea l'amministratore root all'avvio.

```bash
docker run --rm -p 8080:8080 \
  -v myblogapp-data:/data \
  -v myblogapp-attachments:/attachments \
  -e CHILLSHARP_DB_PATH=/data/blogging.db \
  -e CHILLSHARP_SYSTEM_TIMEZONE=Europe/Rome \
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

Con i valori sopra, l'applicazione sara disponibile a:

```text
http://localhost:8080/api/chill
http://localhost:8080/api/chill-auth
http://localhost:8080/api/chill-mcp
```

Imposta `CHILLSHARP_ENABLE_ATTACHMENT=true` quando il context host implementa `IChillAttachmentDbContext`; a quel punto l'API attachment e disponibile a `http://localhost:8080/api/chill-attachment`.

## 7. Esempio Facoltativo `docker compose`

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
      CHILLSHARP_SYSTEM_TIMEZONE: Europe/Rome
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

## Note

- Mantieni l'immagine generica e sposta i valori specifici dell'ambiente nella configurazione runtime.
- Persisti SQLite sotto un volume montato, altrimenti il database viene perso quando il container viene rimosso.
- In produzione, inietta i secret tramite piattaforma container o secret manager invece di hardcodarli nella history di `docker run` o nei file `compose`.

Esempio successivo: [Gestire una relazione Blog-Posts uno-a-molti e leggerla con una sola chiamata client](04-blog-posts-one-to-many.md)
