# HOW-TO: Usare L'Autenticazione Con ChillSharp

Versione originale in inglese: [English](../../HowTo/03-authentication.md)

Questo esempio mostra la configurazione di autenticazione minima ma utile per una API ChillSharp: proteggere l'API, abilitare il modulo auth, registrare un account, fare login e lasciare che `ChillSharpClient` riusi e rinnovi i token automaticamente.

## Obiettivo

Esporre una API ChillSharp protetta basata su ASP.NET Core Identity e autenticarsi con `ChillSharpClient`.

## 1. Usare Un Contesto Che Supporti Identity E ChillSharp Auth

Il contesto deve supportare il normale modello ChillSharp e le tabelle auth.

```csharp
using ChillSharp;
using ChillSharp.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MyBlogApp;

public class BloggingContext : IdentityDbContext<IdentityUser>, IChillContext, IChillAuthDbContext
{
    public BloggingContext(DbContextOptions<BloggingContext> options) : base(options)
    {
    }

    public string GetChillTypePrefix() => "MyBlogApp";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddChillAuthModel();
    }
}
```

## 2. Registrare Identity, Authentication E I Servizi Auth ChillSharp

Proteggi la normale ChillSharp API e aggiungi sopra gli endpoint auth.

```csharp
using ChillSharp.Api;
using ChillSharp.Auth;
using ChillSharp.Auth.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BloggingContext>(options =>
    options.UseSqlite("Data Source=blogging-auth.db"));

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
    options.ReturnPasswordResetTokensInResponse = true;
    options.InitializeRootUserOnStartup = true;
    options.CreateChillAuthUserForRoot = true;
    options.RootUserName = "root";
    options.RootPassword = "Pass123$";
    options.RootEmail = "root@example.com";
    options.RootDisplayName = "Root Administrator";
});
```

Quando `CreateChillAuthUserForRoot = true`, all'avvio viene creato anche il relativo `AuthUser` ChillSharp e `CanManagePermissions = true` viene impostato per quell'utente root.

Puoi fornire gli stessi valori di bootstrap anche tramite variabili d'ambiente invece di hardcodarli:

```text
CHILLSHARP_AUTH_ROOT_USERNAME=root
CHILLSHARP_AUTH_ROOT_PASSWORD=Pass123$
CHILLSHARP_AUTH_ROOT_EMAIL=root@example.com
CHILLSHARP_AUTH_ROOT_DISPLAY_NAME=Root Administrator
```

## 3. Abilitare Il Middleware E Mappare L'API

`MapChillApi()` espone sia gli endpoint normali ChillSharp sia quelli auth una volta registrati i servizi auth.

```csharp
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BloggingContext>();
    db.Database.EnsureCreated();
}

app.MapChillApi();
app.Run();
```

## 4. Usare L'Utente Root Per Gestire I Permessi

L'inizializzatore root-user e la via piu semplice di bootstrap per un sistema protetto nuovo, perche l'utente auth ChillSharp collegato viene creato con la gestione permessi abilitata.

```csharp
var rootClient = new ChillSharpClient("http://localhost:5000/api/chill");

var rootLogin = rootClient.LoginAuthAccount(new LoginAuthIdentityRequest
{
    UserNameOrEmail = "root",
    Password = "Pass123$"
});

var authUsers = rootClient.GetAuthUsers();
Console.WriteLine(authUsers.Count);
```

Quel login puo chiamare endpoint di gestione auth come utenti, ruoli e regole di permesso perche l'`AuthUser` root generato ha `CanManagePermissions = true`.

## 5. Registrare Il Primo Account Normale

Crea il client con il normale Chill base URL. Le chiamate auth passano automaticamente da `/api/chill` a `/api/chill-auth`.

```csharp
using ChillSharp.Auth.Contracts;
using ChillSharp.Client;

var client = new ChillSharpClient("http://localhost:5000/api/chill");

var registerResponse = client.RegisterAuthAccount(new RegisterAuthIdentityRequest
{
    UserName = "admin",
    Email = "admin@example.com",
    Password = "Pass123$",
    DisplayName = "Administrator",
    DisplayCultureName = "it-IT",
    CreateChillAuthUser = true
});
```

Dopo una registrazione riuscita, il client salva internamente access token e refresh token restituiti.

Se `DisplayCultureName` viene fornito e `CreateChillAuthUser = true`, il relativo `AuthUser` viene inizializzato con valori derivati dalla cultura per:

- `DisplayTimeZone`
- `DisplayDateFormat`
- `DisplayNumberFormat`

Per esempio, `it-IT` tipicamente produce preset come:

- `DisplayTimeZone = "W. Europe Standard Time"`
- `DisplayDateFormat = "DD/MM/YYYY"`
- `DisplayNumberFormat = "1.000,00"`

Questi sono preset server-side e possono essere modificati in seguito tramite la gestione utenti auth.

## 6. Fare Login Esplicito

Se l'account esiste gia, fai login con lo stesso `ChillSharpClient`.

```csharp
var loginResponse = client.LoginAuthAccount(new LoginAuthIdentityRequest
{
    UserNameOrEmail = "admin",
    Password = "Pass123$"
});
```

## 7. Chiamare Endpoint Protetti

Una volta autenticato, lo stesso client puo chiamare gli endpoint ChillSharp protetti.

```csharp
using ChillSharp.Client.Dto;

var query = new ChillDtoQuery
{
    ChillType = "Query.BlogQuery"
};
query.ResultProperties = ChillDtoProperty.Build("Guid", "Name");

var result = client.Query(query);
Console.WriteLine(result.Results.Count);
```

## 8. Lasciare Che Il Client Rinnovi I Token Automaticamente

Non devi allegare manualmente il bearer token a ogni richiesta. Se il client ha gia un refresh token, le chiamate autenticate rinnovano l'access token quando serve.

```csharp
var roles = client.GetAuthRoles();
```

Puoi anche fare refresh esplicito:

```csharp
var refreshed = client.RefreshAuthAccount();
```

## Note

- Usa `options.ProtectedApi = true` su `AddChillApi<TContext>(...)` se gli endpoint ChillSharp devono richiedere autenticazione.
- L'utente root creato da `AddChillAuthIdentityApi(...)` e il percorso bootstrap amministrativo. Quando `CreateChillAuthUserForRoot = true`, il relativo `AuthUser` ChillSharp viene creato con `CanManagePermissions = true`.
- `CreateChillAuthUser = true` crea l'`AuthUser` ChillSharp collegato, ma non concede automaticamente permessi amministrativi.
- `DisplayCultureName` in registrazione preimposta `DisplayTimeZone`, `DisplayDateFormat` e `DisplayNumberFormat` per il relativo `AuthUser`.
- In produzione, inizializza il primo amministratore in modo deliberato, ad esempio con root-user initialization o un flusso trusted in fase di installazione.

Esempio successivo: [Creare un'immagine Docker e configurarla con variabili d'ambiente](05-docker-env-variables.md)
