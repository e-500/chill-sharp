# COME FARE: Connetti ChillSharp MCP a ChatGPT

Versione originale in inglese: [English](../../Mcp/ChatGPT.md)


Questa guida mostra come esporre un server MCP ChillSharp protetto su HTTPS e collegarlo da ChatGPT utilizzando il flusso OAuth ChillSharp integrato.

## Obiettivo

Consenti a ChatGPT di connettersi a:

```text
https://your-domain.example/api/chill-mcp
```

e fare in modo che ogni richiesta MCP venga eseguita con le stesse limitazioni di utente, ruolo e autorizzazione ChillSharp utilizzate dalla normale API autenticata dalla portante.

## Requisiti

-Un dominio HTTPS pubblico che può raggiungere l'host ASP.NET Core
- Un `DbContext` che supporta ChillSharp, metadati dello schema e autenticazione
- Identità ASP.NET Core configurata per i tuoi utenti
- 
- Almeno un'entità o query abilitata per MCP

ChatGPT non può connettersi direttamente a `localhost`. Per lo sviluppo locale, esponi l'app con un tunnel HTTPS pubblico e utilizza l'URL pubblico in ChatGPT.

## 1. Preparare il DbContext

Il contesto deve supportare il normale modello ChillSharp, i metadati dello schema per il rilevamento MCP e le tabelle di autenticazione per utenti, ruoli, autorizzazioni e sessioni token.

```csharp
using ChillSharp;
using ChillSharp.Auth;
using ChillSharp.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MyApp;

public class AppDbContext : IdentityDbContext<IdentityUser>, IChillContext, IChillAuthDbContext, IChillSchemaDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public string GetChillTypePrefix() => "MyApp";

    public string GetPrimaryCultureName() => "en-US";

    public string GetSecondaryCultureName() => "it-IT";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddChillAuthModel();
        modelBuilder.AddChillSchemaModel();
    }
}
```

Se la tua app utilizza anche i18n o allegati, conserva anche le registrazioni dei modelli.

## 2. Registra identità, autenticazione del portatore, ChillSharp e MCP

La registrazione `AddChillApi<TContext, TUser>()` supportata dall'identità abilita gli endpoint di autenticazione, il modulo MCP e gli endpoint OAuth di cui ChatGPT ha bisogno.

```csharp
using ChillSharp.Api;
using ChillSharp.Auth.Api;
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

builder.Services.AddChillApi<AppDbContext, IdentityUser>(options =>
{
    options.ProtectedApi = true;

    // Defaults shown explicitly for clarity.
    options.EnableMcpApi = true;
    options.EnableOAuthEndpoints = true;
    options.OAuthBasePath = "/api/chill-auth/oauth";
    options.OAuthProtectedResourcePath = "/api/chill-mcp";
    options.OAuthAuthorizationCodeLifetime = TimeSpan.FromMinutes(5);
});
```

## 3. Mappare l'API

Il middleware di autenticazione e autorizzazione deve essere eseguito prima di `MapChillApi()`.

```csharp
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapChillApi();
app.Run();
```

Con le impostazioni predefinite, questo espone:

| Scopo | URL |
| --- | --- |
| Server MCP |  |
| Metadati di autorizzazione OAuth |  |
| Metadati delle risorse protette MCP |  |
| Registrazione dinamica del cliente |  |
| Pagina di autorizzazione e consenso |  |
| Endpoint token |  |

## 4. Crea o avvia un utente

ChatGPT accede tramite la pagina di autorizzazione OAuth di ChillSharp usando gli stessi utenti ASP.NET Core Identity usati dalla normale autenticazione portante ChillSharp.

Per un primo sistema protetto, puoi eseguire il bootstrap di un utente root:

```csharp
builder.Services.AddChillApi<AppDbContext, IdentityUser>(options =>
{
    options.ProtectedApi = true;
    options.InitializeRootUserOnStartup = true;
    options.CreateChillAuthUserForRoot = true;
    options.RootUserName = "root";
    options.RootPassword = "Pass123$";
    options.RootEmail = "root@example.com";
    options.RootDisplayName = "Root Administrator";
});
```

Quando `CreateChillAuthUserForRoot = true`, ChillSharp crea anche il record `AuthUser` collegato utilizzato dai controlli delle autorizzazioni.

## 5. Abilita solo schemi MCP sicuri

ChatGPT può vedere solo gli schemi abilitati per MCP. Contrassegna solo le entità e le query sicure e utili per l'accesso all'intelligenza artificiale.

```csharp
using ChillSharp.Annotations;
using ChillSharp.EF;

[ChillEntity(
    UniquePropertyKeyString: "4E16F6C0-6B95-4D67-98BC-9F4D0D63EAF1",
    PrimaryLanguageLabel: "Invoice",
    SecondaryLanguageLabel: "Fattura",
    EnableMCP = true,
    MCPDescription = "Customer invoice header. Use it to inspect invoice identity, customer, dates, totals, and payment state.")]
public class Invoice : ChillEntity
{
    [ChillProperty(
        UniquePropertyKeyString: "50B1BB6C-D794-41E4-A85C-D4F9D7A6FA7E",
        PrimaryLanguageLabel: "Invoice number",
        SecondaryLanguageLabel: "Numero fattura",
        MCPDescription = "Human-readable invoice number used by accountants and customers.")]
    public string InvoiceNumber { get; set; } = string.Empty;
}
```

Utilizza testo `MCPDescription` chiaro su entità, query e proprietà. ChatGPT fa molto affidamento su tali descrizioni quando si scelgono gli strumenti e si creano payload di query.

ChatGPT non dovrebbe inventare oggetti di richiesta ChillSharp. Il flusso di lavoro previsto è:

1. chiamare `ChillSharp get-schema-list`
2. chiamare `ChillSharp get-schema` per l'entità o la query
3. utilizzare nomi di proprietà dello schema esatti in `Properties`
4. abbinare i valori a `simplePropertyType` di ciascuna proprietà

Ad esempio, utilizza stringhe JSON per `string`, numeri JSON per `int` e `decimal`, booleani JSON per `bool` e riferimenti `ChillDtoEntity` con `ChillType` e `Guid` per le proprietà `chill-entity`. Per `ResultProperties`, utilizzare oggetti come `{ "name": "InvoiceNumber" }` dallo schema di entità restituito.

Per le proprietà della query, ChatGPT dovrebbe leggere `MCPDescription` di ciascuna proprietà per dedurre il comportamento di ricerca. Se una proprietà della query non ha una descrizione o la descrizione non spiega il comportamento di corrispondenza, presuppone che la corrispondenza esatta sia uguale. Ogni query Chill accetta anche `Properties.FullTextSearch`; usalo per la ricerca di parole chiave generiche quando l'utente non richiede un filtro strutturato specifico.

`Properties.FullTextSearch` effettua ricerche contro ChillSharp `FullTextContent`. Il testo senza virgolette senza selettori avanzati viene normalizzato, suddiviso in spazi bianchi e abbinato con AND, quindi ogni token deve essere presente. Le parentesi più gli operatori `and`/`or` autonomi al di fuori delle virgolette consentono la ricerca booleana raggruppata, ad esempio `[la and nazione] or roma`. Cerca le parole letterali `and` o `or` racchiudendole tra virgolette corrispondenti, ad esempio `"and"`. Le virgolette singole o doppie corrispondenti cercano una frase normalizzata con limiti di parole: `"la nazione"` corrisponde a `bla bla la nazione bla bla` ma non a `bla bla della nazione bla bla`. Un carattere jolly `*` o `%` iniziale o finale racchiuso tra virgolette rilassa quel lato del confine, ad esempio `"*la nazione"` o `"%la nazione"` può corrispondere a `della nazione` e `"la nazione*"` può corrispondere a un suffisso. Se `*` o `%` appare nel mezzo di una frase tra virgolette, ChillSharp lo tratta come separatori di token e applica la normale corrispondenza AND dei token.

## 6. Connettiti da ChatGPT

In ChatGPT, aggiungi un connettore personalizzato o un server MCP remoto utilizzando l'URL MCP HTTPS pubblico:

```text
https://your-domain.example/api/chill-mcp
```

ChatGPT dovrebbe rilevare i metadati OAuth, registrarsi come client OAuth pubblico e aprire la pagina di autorizzazione di ChillSharp.

Dopo che l'utente ha effettuato l'accesso e ha acconsentito:

1. ChatGPT riceve un codice di autorizzazione.
2. ChatGPT scambia il codice e il verificatore PKCE con un token di accesso al portatore ChillSharp.
3. ChatGPT chiama l'endpoint MCP con:

```http
Authorization: Bearer <access-token>
```

OAuth è solo il flusso di consenso e acquisizione di token. L'endpoint MCP utilizza ancora la normale autenticazione della portante ChillSharp.

## Comportamento dei permessi

Gli utenti OAuth non sono utenti separati.

Il flusso OAuth autentica un utente ASP.NET Core Identity ed emette lo stesso tipo di token di connessione ChillSharp usato dall'accesso normale. Il token contiene le stesse attestazioni dell'identificatore utente, quindi ChillSharp risolve lo stesso `AuthUser.ExternalId` e applica gli stessi ruoli, regole di autorizzazione, autorizzazioni dello schema e limitazioni API.

Ciò significa:

- un utente bloccato da una normale operazione protetta di ChillSharp viene bloccato anche tramite ChatGPT
- un utente con ruolo limitato mantiene le stesse limitazioni tramite MCP
- La visibilità MCP richiede ancora `EnableMCP`; la visibilità della query segue la relativa entità restituita
- Gli ambiti OAuth attualmente non creano un livello di autorizzazione separato

## URL pubblici utili da testare

Aprili dall'esterno della rete del tuo server:

```text
https://your-domain.example/.well-known/oauth-authorization-server
https://your-domain.example/.well-known/oauth-protected-resource
https://your-domain.example/api/chill-mcp
```

L'URL MCP deve rifiutare l'accesso anonimo quando `ProtectedApi = true` e la risposta deve pubblicizzare i metadati delle risorse protette OAuth nell'intestazione `WWW-Authenticate`.

## Risoluzione dei problemi

### ChatGPT non può raggiungere il server

Verificare che l'URL MCP utilizzi HTTPS pubblico e non sia una rete privata o un indirizzo `localhost`.

### ChatGPT non avvia OAuth

Controllo:

- 
- 
- `AddChillAuthBearer()` è registrato
- `UseAuthentication()` e `UseAuthorization()` vengono eseguiti prima di `MapChillApi()`
- `/.well-known/oauth-authorization-server` è raggiungibile tramite HTTPS
- `/.well-known/oauth-protected-resource` è raggiungibile tramite HTTPS

### L'accesso riesce ma le azioni MCP sono vietate

L'accesso all'identità è riuscito, ma ChillSharp `AuthUser` collegato o i suoi ruoli non consentono l'operazione richiesta. Controlla `AuthUser.ExternalId`, assegnazioni di ruoli e regole di autorizzazione.

### ChatGPT non vede schemi utili

Verifica che l'entità o la query di destinazione sia abilitata per MCP e contenga descrizioni utili:

- 
- entità/query `MCPDescription`
- `MCPDescription` a livello di proprietà
- tipi di query mirati per flussi di lavoro AI comuni

### Più istanze dell'app perdono le registrazioni OAuth

Il registro client OAuth dinamico integrato è attualmente in memoria. Per la produzione a più istanze o le registrazioni stabili al riavvio, mantenere le registrazioni del client OAuth nel database di autenticazione.

## Lista di controllo della produzione

- Utilizza solo HTTPS
- Mantieni `ProtectedApi = true`
- Abilita MCP solo su schemi sicuri
- Utilizza superfici di query mirate invece di esporre tutto
- Assegna a ChatGPT un utente o un ruolo con privilegi minimi
- Esamina gli strumenti di modifica come creazione, aggiornamento, eliminazione e blocco
- Mantieni le registrazioni del client OAuth se si esegue più di un'istanza dell'app
- Mantieni breve la durata dei token di accesso

## Documenti correlati

- [Riferimento modulo MCP](README.md)
- [Procedura per l'autenticazione](../HowTo/03-authentication.md)
- [Preparazione del modello](../ModelPreparation.md)
