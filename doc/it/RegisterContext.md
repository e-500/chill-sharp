# Registrare Un Contesto ChillSharp

Versione originale in inglese: [English](../RegisterContext.md)

Questo documento mostra come collegare i moduli ChillSharp dentro un host ASP.NET Core.

## API Core Minima

Per usare solo la Chill API core:

```csharp
using ChillSharp.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

builder.Services.AddChillApi<AppDbContext>();

var app = builder.Build();
app.MapChillApi();
app.Run();
```

Requisiti:

- `AppDbContext` deve ereditare `DbContext`
- `AppDbContext` deve implementare `IChillContext`

## Cosa Registra `AddChillApi<TContext>()`

La registrazione core configura:

- controller ChillSharp
- risoluzione di `IChillContext` dal contesto host
- `IChillDtoEngine`
- comportamento opzionale di API protetta tramite `ChillApiOptions`

## Proteggere La Core API

Se l'host configura gia autenticazione e autorizzazione, puoi richiedere auth sulla Chill API:

```csharp
builder.Services.AddChillApi<AppDbContext>(options =>
{
    options.ProtectedApi = true;
});
```

Poi usa il middleware standard ASP.NET Core:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.MapChillApi();
```

## Aggiungere Servizi Schema

Per persistere e servire metadati di schema:

```csharp
using ChillSharp.Schema;

builder.Services.AddChillSchema<AppDbContext>();
```

Requisiti del contesto:

- `AppDbContext : IChillSchemaDbContext`
- `modelBuilder.AddChillSchemaModel()`

`ChillSharp.Schema` possiede anche la registrazione della schema cache.

## Aggiungere Gestione Auth

Per esporre gli endpoint di gestione auth senza i flussi account di ASP.NET Core Identity:

```csharp
using ChillSharp.Auth.Api;

builder.Services.AddChillAuthApi<AppDbContext>();
```

Requisiti del contesto:

- `AppDbContext : IChillAuthDbContext`
- `modelBuilder.AddChillAuthModel()`

Questo aggiunge endpoint per:

- auth users
- ruoli
- assegnazioni utente-ruolo
- regole di permesso
- valutazione dei permessi

## Aggiungere I Flussi Account Identity

Per esporre registrazione account, login, refresh token, cambio password e reset password:

```csharp
using ChillSharp.Auth;
using ChillSharp.Auth.Api;
using Microsoft.AspNetCore.Identity;

builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(ChillAuthIdentityDefaults.AuthenticationScheme)
    .AddChillAuthBearer();

builder.Services.AddAuthorization();

builder.Services.AddChillAuthIdentityApi<AppDbContext, IdentityUser>();
```

Questo si appoggia a `AddChillAuthApi<TContext>()`.

## Aggiungere I Servizi I18n

Per esporre endpoint i18n:

```csharp
using ChillSharp.I18n.Api;

builder.Services.AddChillI18nApi<AppDbContext>();
```

Requisiti del contesto:

- `AppDbContext : IChillI18nDbContext`
- `modelBuilder.AddChillI18nModel()`

Il modulo attualmente espone:

- `GET /api/chill-i18n/text/{labelGuid}/{cultureName}`
- `PUT /api/chill-i18n/text`

## Esempio Completo Di Host

```csharp
using ChillSharp.Api;
using ChillSharp.Auth;
using ChillSharp.Auth.Api;
using ChillSharp.I18n.Api;
using ChillSharp.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(ChillAuthIdentityDefaults.AuthenticationScheme)
    .AddChillAuthBearer();
builder.Services.AddAuthorization();

builder.Services.AddChillApi<AppDbContext>(options =>
{
    options.ProtectedApi = true;
});
builder.Services.AddChillSchema<AppDbContext>();
builder.Services.AddChillAuthIdentityApi<AppDbContext, IdentityUser>();
builder.Services.AddChillI18nApi<AppDbContext>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapChillApi();
app.Run();
```

## Comportamento Del Mapping

`app.MapChillApi()` mappa i controller ChillSharp e gli endpoint di supporto. Non sostituisce il setup standard del middleware ASP.NET Core. Ti servono comunque:

- `UseAuthentication()` se auth e abilitata
- `UseAuthorization()` se authorization e abilitata

I controller i18n e auth vengono aggiunti tramite controller discovery quando i relativi moduli sono registrati.

## Ordine Di Startup Consigliato

1. registrare il contesto EF Core
2. registrare Identity se necessario
3. registrare authentication e authorization se necessario
4. registrare `AddChillApi<TContext>()`
5. registrare i moduli opzionali (`Schema`, `Auth`, `I18n`)
6. build dell'app
7. applicare il middleware
8. mappare la Chill API

## OpenAPI / Swagger

ChillSharp non forza Swagger nel tuo host. Se vuoi output OpenAPI per documentazione o generazione client, aggiungilo nell'app host:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
```

Questa e la base consigliata per generare client TypeScript e Python da un host ChillSharp.
