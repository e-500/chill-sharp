# Modulo MCP ChillSharp

Versione originale in inglese: [English](../../Mcp/README.md)


Questo documento descrive il modulo `ChillSharp.Mcp`, come registrarlo in un host ASP.NET Core e come preparare un `DbContext` e un modello in modo che gli agenti IA possano utilizzare lo schema esposto e la superficie delle query in modo efficiente.

`ChillSharp.Mcp` utilizza l'SDK MCP C# ufficiale ed espone un server Model Context Protocol supportato dal contesto ChillSharp.

Per una guida mirata sulla connessione di questo server MCP da ChatGPT, vedere [HOW-TO: connettere ChillSharp MCP a ChatGPT](ChatGPT.md).

## Obiettivi

Dopo la configurazione, un client MCP può:

- scoprire gli schemi abilitati per MCP esposti dal tuo host
- ispezionare l'entità completa e gli schemi di query prima di inviare richieste
- leggere le descrizioni MCP a livello di schema e di proprietà
- esegui solo le query che esponi esplicitamente tramite `EnableMCP`
- eseguire operazioni DTO come ricerca, ricerca, creazione, aggiornamento, eliminazione, completamento automatico, convalida e blocco
- operare con autorizzazioni utente autenticate dal portatore e limitazioni della chiave API

## Strumenti registrati

Il modulo registra questi strumenti MCP:

- 
- 
- 
- 
- 
- 
- 
- 
- 
- 
- 
- 
- 

### `ChillSharp get-schema-list`

Restituisce solo gli schemi abilitati per MCP.

Utilizzalo come punto di ingresso per il rilevamento. Indica all'IA quali entità e query devono essere utilizzate tramite MCP.

### `ChillSharp get-schema`

Restituisce l'intero `ChillDtoSchema` per un'entità o un tipo di query abilitato per MCP.

Questo è lo strumento di introspezione più importante. Include:

- metadati dello schema
- informazioni sul tipo correlato alla query
- metadati di relazione dedotti da raccolte annotate con `ChillRelationAttribute`
- `MCPDescription` a livello di schema
- tutte le proprietà dello schema
- `MCPDescription` a livello di proprietà per ogni proprietà
- informazioni sul tipo di riferimento
- `simplePropertyType`, una stringa di tipo agent-friendly per la costruzione del carico utile

In pratica, ecco come apprende un’intelligenza artificiale:

- cosa rappresenta l'oggetto
- cosa significa ciascuna proprietà
- quale query restituisce quale tipo di entità
- quali proprietà sono riferimenti ad altri tipi di Chill
- quale forma di valore richiede ciascuna proprietà della richiesta

Gli agenti non dovrebbero inventare oggetti di richiesta. Utilizza `get-schema` come contratto, copia i nomi esatti delle proprietà dallo schema e invia valori che corrispondono a `simplePropertyType` di ciascuna proprietà.

Per gli schemi di entità, `Relations` descrive le raccolte di relazioni figlio che l'interfaccia utente può collegare automaticamente in fase di esecuzione. Ogni voce di relazione include:

- `ChillType`, il tipo di entità figlio o di relazione esposto dalla raccolta
- `ChillQuery`, il tipo di query da utilizzare per la ricerca secondaria filtrata quando è possibile risolverla
- `FixedValues`, valori predefiniti da inserire durante la creazione di un'entità figlio
- `FixedQueryValues`, filtri di query predefiniti da applicare durante la navigazione di entità correlate esistenti
- `RelationLabel`, l'etichetta GUID e i testi predefiniti derivati ​​da `ChillRelationAttribute` della raccolta

Quando una relazione può essere ricollegata al genitore corrente tramite un riferimento figlio annotato, ChillSharp emette il valore magico `@{mock}` all'interno di `FixedValues` e `FixedQueryValues`. I client dell'interfaccia utente sostituiscono quel token con l'entità principale corrente per il nome della proprietà FK/riferimento corrispondente.

I valori `simplePropertyType` comuni sono:

| tipoPropertysemplice | Valore del carico utile |
| --- | --- |
|  | Stringa GUID |
|  | Numero JSON senza decimali |
|  | Numero JSON |
|  | stringa della data |
|  | stringa temporale |
|  | stringa data-ora |
|  | stringa di durata o valore numerico accettato dall'host |
|  | JSON booleano |
| ,  | Stringa JSON |
|  | Oggetto JSON, array o stringa JSON in base al contratto di campo |
|  | Riferimento `ChillDtoEntity` con `ChillType` e `Guid` |
|  | array di riferimenti `ChillDtoEntity` |
|  | query DTO corrispondente allo schema di query a cui si fa riferimento |

### `ChillSharp query`

Esegue una query ChillSharp solo quando la relativa entità restituita è abilitata per MCP.

Il flusso di lavoro consigliato è:

1. chiamare `ChillSharp get-schema-list`
2. chiamare `ChillSharp get-schema` sul tipo di query selezionato
3. leggere descrizioni, proprietà e tipo restituito
4. invia un payload `ChillDtoQuery` a `ChillSharp query`

L'oggetto `Properties` deve contenere solo nomi di proprietà di input accettati dallo schema di query. Per ogni valore, seguire `simplePropertyType`; ad esempio, inviare una stringa per `string`, un numero per `int` o `decimal` e un riferimento `ChillDtoEntity` per `chill-entity`.

Leggere `MCPDescription` di ciascuna proprietà della query per dedurre come viene eseguita la ricerca dell'input. Le descrizioni dovrebbero indicare all'agente se una proprietà si comporta come un valore esatto, contiene una ricerca di testo in stile, un limite di intervallo, un riferimento di ricerca, un selettore di stato o un'altra regola di query personalizzata. Se la descrizione manca o non specifica il comportamento di corrispondenza, presuppone che la corrispondenza esatta sia uguale.

Ogni query Chill supporta anche `Properties.FullTextSearch`. Utilizzalo per la ricerca di parole chiave generiche nell'obiettivo della query quando l'utente non richiede un filtro strutturato specifico.

`FullTextSearch` cerca l'entità `FullTextContent` generata da ChillSharp. Il testo senza virgolette senza selettori avanzati è normalizzato, suddiviso in spazi bianchi e abbinato a AND, quindi ogni token deve essere presente. Le parentesi più gli operatori `and`/`or` autonomi al di fuori delle virgolette consentono la ricerca booleana raggruppata. Cerca le parole letterali `and` o `or` racchiudendole tra virgolette corrispondenti. Il testo racchiuso tra virgolette singole o doppie corrispondenti viene cercato come una frase normalizzata con limiti di parole:

| Cerca testo | Significato |
| --- | --- |
|  | Abbina i record contenenti sia `la` che `nazione` come token, in qualsiasi posizione. |
|  | Abbina record contenenti sia `la` che `nazione` oppure record contenenti `roma`. |
|  | Cerca la parola chiave letterale `and` anziché l'operatore booleano. |
|  | Abbina la frase esatta come parole intere, ad esempio `bla bla la nazione bla bla`, ma non `bla bla della nazione bla bla`. |
| `"*la nazione"` o `"%la nazione"` | Rilassa il confine sinistro, in modo che `della nazione` possa corrispondere. |
| `"la nazione*"` o `"la nazione%"` | Rilassa il confine destro, in modo che un suffisso possa corrispondere. |
| `"la*nazione"` o `"la%nazione"` | Tratta il carattere jolly centrale come un separatore di token e applica la normale corrispondenza dei token AND. |

### `ChillSharp lookup`

Esegue una ricerca generica di testo completo rispetto a uno schema di entità abilitato per MCP.

Utilizza un payload `ChillDtoQuery` con:

- `ChillType` impostato su un tipo di entità come `Model.Blog`
- `Properties.FullTextSearch` contenente il testo della ricerca
- `ResultProperties`, `Pagination` e `Ordering` opzionali

`Properties.FullTextSearch` utilizza la stessa frase tra virgolette e le stesse regole dei caratteri jolly descritte in `ChillSharp query`.

### `ChillSharp find`

Trova un'entità abilitata per MCP in base a `ChillType` e `Guid`.

Utilizza un payload `ChillDtoEntity` con:

- `ChillType` impostato su un tipo di entità come `Model.Blog`
- `Guid` impostato sull'identificatore del record

Lo strumento restituisce `null` quando non esiste alcun record corrispondente.

### `ChillSharp create`

Crea una nuova entità abilitata per MCP e restituisce il valore `ChillDtoEntity` persistente.

Utilizza prima `ChillSharp get-schema`, quindi invia un payload `ChillDtoEntity` con:

- `ChillType` impostato su un tipo di entità come `Model.Blog`
- facoltativo `Guid` quando il client sceglie l'identificatore
- `Properties` contenente valori di campo annotati

### `ChillSharp update`

Aggiorna un'entità esistente abilitata per MCP e restituisce l'oggetto `ChillDtoEntity` aggiornato.

Utilizza un payload `ChillDtoEntity` con:

- `ChillType` impostato su un tipo di entità come `Model.Blog`
- `Guid` impostato su un record esistente
- `Properties` contenente i campi da aggiornare

### `ChillSharp delete`

Elimina un'entità esistente abilitata per MCP identificata da `ChillType` e `Guid`.

Questa è un'operazione mutante. Un client dovrebbe normalmente chiamare prima `ChillSharp find` per confermare il record esatto prima della cancellazione.

### `ChillSharp autocomplete-entity`

Applica la logica di completamento automatico dell'entità ChillSharp senza modifiche persistenti.

Utilizzalo prima di `create` o `update` quando il modello di entità calcola etichette, URL, riferimenti o altri valori derivati.

### `ChillSharp autocomplete-query`

Applica la logica di completamento automatico delle query ChillSharp senza eseguire la query.

Utilizzarlo quando gli input della query hanno valori dipendenti o calcolati.

### `ChillSharp validate-entity`

Convalida un DTO di entità abilitato per MCP e restituisce errori di convalida di ChillSharp senza modifiche persistenti.

Utilizzarlo prima di `create` o `update` quando il modello host espone regole di convalida.

### `ChillSharp validate-query`

Convalida un DTO di query abilitato per MCP e restituisce errori di convalida di ChillSharp senza eseguire la query.

Utilizzarlo prima di `query` quando il tipo di query espone regole di convalida.

### `ChillSharp chunk`

Esegue un elenco di elementi `ChillOperation` e restituisce l'elenco delle operazioni aggiornato.

I verbi supportati sono:

- 
- 
- 
- 
- 
- 
- 
- 
- 

Ogni operazione viene controllata per la visibilità MCP prima dell'esecuzione di qualsiasi operazione. Se un'operazione prende di mira uno schema non abilitato per MCP, l'intero blocco viene rifiutato.

Per le operazioni `query`, `autocomplete` e `validate` che utilizzano un payload di query, impostare `Query`. Per le operazioni sulle entità, impostare `Entity`.

## Configurazione di base dell'host

```csharp
using ChillSharp.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

builder.Services.AddChillApi<AppDbContext>(options =>
{
    options.ProtectedApi = true;
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapChillApi();
app.Run();
```

Quando `EnableMcpApi` rimane `true`, il modulo MCP è abilitato per impostazione predefinita come parte di `AddChillApi<TContext>()`.

## URL di connessione dell'agente

Gli agenti e i client MCP si connettono all'endpoint di trasporto HTTP MCP, non ai normali endpoint REST di ChillSharp.

Con la configurazione predefinita, utilizzare:

```text
{host}/api/chill-mcp
```

Esempi locali:

```text
http://localhost:5000/api/chill-mcp
https://localhost:5001/api/chill-mcp
```

Non configurare gli agenti per utilizzare `/api/chill`, `/api/chill/query` o l'URL Swagger. Questi sono normali endpoint API REST. Per impostazione predefinita, l'endpoint SDK MCP è `/api/chill-mcp`.

L'URL finale si basa su due impostazioni:

- `ChillApiOptions.ApiBasePath`, predefinito `/api`
- `ChillMcpOptions.RoutePattern`, predefinito `/api/chill-mcp`

La route MCP predefinita è normalizzata al percorso base API corrente. Ciò significa:

| Percorso base API | Percorso MCP da utilizzare |
| --- | --- |
|  |  |
|  |  |
| percorso base vuoto |  |

Se configuri un percorso MCP personalizzato:

```csharp
builder.Services.AddChillMcpApi<AppDbContext>(options =>
{
    options.RoutePattern = "mcp";
});
```

quindi il percorso è relativo al percorso base dell'API ChillSharp, quindi il percorso base dell'API predefinito produce:

```text
{host}/api/mcp
```

Se configuri un percorso assoluto:

```csharp
builder.Services.AddChillMcpApi<AppDbContext>(options =>
{
    options.RoutePattern = "/mcp";
});
```

quindi gli agenti dovrebbero connettersi a:

```text
{host}/mcp
```

Quando `ProtectedApi = true`, l'endpoint MCP richiede l'autenticazione. Configura l'agente o il client MCP per inviare:

```http
Authorization: Bearer <access-token>
```

## Disabilita MCP a livello globale

```csharp
builder.Services.AddChillApi<AppDbContext>(options =>
{
    options.EnableMcpApi = false;
});
```

## Registra direttamente il modulo

Se hai bisogno della registrazione diretta del modulo:

```csharp
using ChillSharp.Mcp.Api;

builder.Services.AddChillMcpApi<AppDbContext>(options =>
{
    options.Enabled = true;
    options.RoutePattern = "/api/chill-mcp";
});
```

## Requisiti del contesto

Il contesto host deve:

- eredita da `DbContext`
- implementare `IChillContext`
- implementare `IChillSchemaDbContext`
- includere il modello di schema Chill in `OnModelCreating`

Forma tipica:

```csharp
using ChillSharp;
using ChillSharp.Schema;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext, IChillContext, IChillSchemaDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public string GetChillTypePrefix()
    {
        return "MyCompany.MyProduct.Data";
    }

    public string GetPrimaryCultureName()
    {
        return "en-US";
    }

    public string GetSecondaryCultureName()
    {
        return "it-IT";
    }

    public string GetCurrentUserName()
    {
        return Environment.UserName;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddChillSchemaModel();
    }
}
```

## Autenticazione

L'endpoint MCP è pensato per essere eseguito dietro l'autenticazione della portante.

Se l'host utilizza:

```csharp
builder.Services.AddChillApi<AppDbContext>(options =>
{
    options.ProtectedApi = true;
});
```

quindi anche l'endpoint MCP mappato richiede l'autenticazione.

Questo è importante perché l'esposizione MCP dovrebbe in genere essere limitata a un utente o a una chiave API, non a chiamanti anonimi.

## Connessione ChatGPT OAuth

Quando connetti ChatGPT a un server MCP remoto protetto, configura ChatGPT con l'endpoint MCP HTTPS pubblico:

```text
https://your-domain.example/api/chill-mcp
```

Se si usa il modulo di autenticazione ChillSharp supportato da ASP.NET Core Identity, ChillSharp espone un flusso di codice di autorizzazione OAuth incorporato con PKCE per ChatGPT e altri client MCP remoti.

Gli endpoint OAuth predefiniti sono:

| Scopo | URL |
| --- | --- |
| Metadati del server di autorizzazione OAuth |  |
| Metadati delle risorse protette MCP |  |
| Registrazione dinamica del cliente |  |
| Autorizzazione e consenso dell'utente |  |
| Scambio gettoni |  |

Il flusso è:

1. ChatGPT rileva i metadati delle risorse protette e del server di autorizzazione.
2. ChatGPT si registra dinamicamente come client OAuth pubblico.
3. L'utente viene reindirizzato alla pagina di autorizzazione di ChillSharp.
4. L'utente accede con l'account identità ASP.NET Core.
5. ChillSharp reindirizza ChatGPT con un codice di autorizzazione.
6. ChatGPT scambia il codice e il verificatore PKCE con un token di accesso al portatore ChillSharp.
7. ChatGPT chiama l'endpoint MCP con:

```http
Authorization: Bearer <access-token>
```

Pertanto OAuth viene utilizzato per il consenso dell'utente e l'acquisizione di token. Il server MCP stesso convalida comunque il token di connessione risultante tramite il normale gestore di autenticazione della connessione di ChillSharp.

Configurazione protetta tipica:

```csharp
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
});
```

Gli endpoint OAuth sono abilitati per impostazione predefinita per il modulo di autenticazione supportata da identità. Puoi configurarli tramite `ChillIdentityApiOptions`:

```csharp
builder.Services.AddChillApi<AppDbContext, IdentityUser>(options =>
{
    options.ProtectedApi = true;
    options.OAuthBasePath = "/api/chill-auth/oauth";
    options.OAuthProtectedResourcePath = "/api/chill-mcp";
    options.OAuthAuthorizationCodeLifetime = TimeSpan.FromMinutes(5);
});
```

Se disabiliti o sostituisci gli endpoint OAuth integrati, puoi comunque utilizzare ChillSharp come server di risorse MCP purché il tuo gestore di autenticazione convalidi il token di connessione finale e ChatGPT possa completare un flusso di codice di autorizzazione OAuth altrove.

## Come funziona `EnableMCP`

Gli strumenti MCP espongono solo schemi la cui visibilità MCP è abilitata.

Uno schema è considerato abilitato per MCP quando:

- `schema.EnableMCP` è `true`
- Le opzioni dell'entità runtime abilitano MCP per quel tipo di Chill

Per gli schemi di query, la visibilità MCP è controllata dalla relativa entità restituita. Una query che restituisce `Model.Invoice` è visibile ed eseguibile tramite MCP solo quando `Model.Invoice` è abilitato per MCP. Abilitando solo il tipo di query non viene pubblicata un'entità nascosta.

Ciò significa:

- `get-schema-list` mostra solo gli schemi abilitati
- `get-schema` restituisce solo gli schemi abilitati
- `query` esegue solo le query la cui relativa entità restituita è abilitata
- Gli strumenti di entità operano solo su schemi di entità abilitati
- `chunk` controlla ogni query o entità mirata prima di eseguire il batch

Ciò fornisce un meccanismo esplicito di pubblicazione/annullamento della pubblicazione per le funzionalità del database rivolte all'intelligenza artificiale.

## Preparazione di un contesto Db per un consumo efficiente dell'intelligenza artificiale

Questa è la parte più importante del modulo.

Un'intelligenza artificiale non capisce il tuo modello come fa un compagno di squadra umano. Dipende fortemente dai metadati, dalla denominazione, dalle descrizioni e da una superficie di query vincolata. Un database può essere tecnicamente esposto tramite MCP ed essere comunque difficile da utilizzare bene per un'intelligenza artificiale.

Se vuoi che un'intelligenza artificiale utilizzi un host ChillSharp in modo efficiente, prepara il modello intenzionalmente.

## 1. Utilizzare nomi chiari per i tipi di Chill

Nomi di tipo breve come `Model.Blog`, `Model.Invoice` e `Query.PostSearchQuery` sono più facili da ragionare per un'intelligenza artificiale rispetto ai nomi opachi.

Preferisco:

- 
- 
- 
- 

Evita nomi che richiedono la conoscenza del team interno per essere decodificati.

Meno efficiente:

- 
- 
- 

## 2. Annota intenzionalmente ogni proprietà esposta

Utilizza `[ChillProperty]` in modo coerente sulle proprietà che desideri nella superficie rivolta all'intelligenza artificiale.

Ciò influisce:

- generazione dello schema
- interrogare le aspettative di carico utile
- Mappatura DTO
- l'elenco dei campi che un'intelligenza artificiale vede quando ispeziona uno schema

Se una proprietà è importante per query, ricerche, filtri o risultati, in genere dovrebbe essere annotata esplicitamente.

## 3. Scrivi un testo `MCPDescription` efficace sulle entità

Le descrizioni di entità e query non sono decorazioni. Sono il modo in cui un'intelligenza artificiale apprende il significato del business.

Buone descrizioni a livello di entità spiegano:

- qual è l'oggetto
- quando dovrebbe essere interrogato
- cosa rappresenta in termini aziendali
- se si tratta di un record primario, di una tabella di ricerca o di una superficie derivata/di sola query

Esempio:

```csharp
[ChillEntity(
    UniquePropertyKeyString: "4E16F6C0-6B95-4D67-98BC-9F4D0D63EAF1",
    PrimaryLanguageLabel: "Invoice",
    SecondaryLanguageLabel: "Fattura",
    EnableMCP = true,
    MCPDescription = "Customer invoice header. Use this schema to inspect invoice number, issue date, customer, total amount, and payment state.")]
public class Invoice : ChillEntity
{
}
```

Questo è molto più utile di:

- 
- 

## 4. Scrivi un testo `MCPDescription` efficace sulle proprietà

Le descrizioni delle proprietà contano ancora di più.

Quando un'IA riceve `get-schema`, ciascuna proprietà può portare il proprio `MCPDescription`. Questa è spesso la differenza tra una query corretta e una sbagliata.

Le buone descrizioni delle proprietà spiegano:

- il significato aziendale
- contenuto consentito o previsto
- unità o formato
- se il campo è una ricerca, un riferimento, uno stato, un codice o un testo libero
- se il campo viene restituito, filtrabile, calcolato o informativo
- per le proprietà della query, se la corrispondenza è esatta, contiene stile, basata su intervallo, basata su ricerca o personalizzata

Quando `MCPDescription` di una proprietà della query non spiega il comportamento di corrispondenza, gli agenti dovrebbero presupporre che la corrispondenza esatta sia uguale. Se desideri un comportamento contiene, prefisso, intervallo, fuzzy o specifico del dominio, specificalo esplicitamente nella descrizione.

Esempio:

```csharp
[ChillProperty(
    UniquePropertyKeyString: "50B1BB6C-D794-41E4-A85C-D4F9D7A6FA7E",
    PrimaryLanguageLabel: "Invoice number",
    SecondaryLanguageLabel: "Numero fattura",
    MCPDescription = "Human-readable accounting document number shown to users and used in external communication.")]
public string InvoiceNumber { get; set; } = string.Empty;
```

E:

```csharp
[ChillProperty(
    UniquePropertyKeyString: "A18E7754-D8F7-45FE-B8A8-EA762A4EC9E6",
    PrimaryLanguageLabel: "Payment status",
    SecondaryLanguageLabel: "Stato pagamento",
    MCPDescription = "Current payment lifecycle status. Expected values are Draft, Issued, PartiallyPaid, Paid, and Cancelled.")]
public string PaymentStatus { get; set; } = string.Empty;
```

Queste descrizioni vengono restituite da `ChillSharp get-schema`.

## 5. Preferisci tipi di query appositamente creati rispetto all'esposizione di tutto

L’intelligenza artificiale funziona meglio quando ha un numero limitato di query ben descritte anziché un’enorme superficie ambigua.

Preferisci diversi tipi di query chiari come:

- 
- 
- 

invece di forzare l'intelligenza artificiale a dedurre tutto da una query generica generica.

Ogni query dovrebbe avere:

- un nome chiaro
- uno scopo chiaro
- proprietà di input ben descritte
- un tipo di entità correlata prevedibile

## 6. Mantieni gli input delle query limitati e significativi

Una query con venti input opzionali e significati vaghi è difficile per gli esseri umani e più difficile per l’intelligenza artificiale.

Preferisci una superficie di query in cui ogni input abbia uno scopo forte.

Bene:

- 
- 
- 
- 

Meno buono:

- 
- 
- 
- 

## 7. Esporre intenzionalmente i riferimenti

I riferimenti sono utili perché dicono a un'intelligenza artificiale come si relazionano tabelle ed entità.

Se una proprietà fa riferimento a un altro tipo di Chill, assicurati che:

- il riferimento è rappresentato tramite metadati Chill
- il tipo di destinazione ha uno schema utile
- la descrizione dell'immobile spiega la relazione

Esempio:

- 
- 

Ciò aiuta un'intelligenza artificiale a navigare nel grafico del tuo database invece di trattare ogni oggetto come isolato.

## 8. Mantieni le etichette utili

`Label`, `ShortLabel` e i nomi visualizzati dello schema aiutano un'intelligenza artificiale a scegliere l'oggetto giusto quando esistono molti tipi correlati.

Una buona etichetta è:

- stabile
- leggibile dall'uomo
- derivato dall'identità aziendale del record

Esempi:

- numero di fattura
- nome del cliente
- codice prodotto e titolo

Ciò migliora sia il comportamento dell'interfaccia utente che la comprensione dell'intelligenza artificiale.

## 9. Separare gli oggetti solo interni dagli oggetti rivolti verso l'IA

Non tutte le entità dovrebbero essere abilitate per MCP.

Una buona regola è:

- abilitare MCP solo per oggetti comprensibili e sicuri da esporre a un flusso di lavoro AI
- mantenere disabilitate le entità infrastrutturali di basso livello, le tabelle di registro o gli elementi interni sensibili a meno che non vi sia un motivo reale per pubblicarli

Ciò riduce la confusione, lo spreco di token e l'uso improprio accidentale.

## 10. Progetta tenendo presente i limiti dei permessi

L'utente autenticato della chiave API può essere limitato da autorizzazioni e altre limitazioni.

Ciò significa che un buon host rivolto all’intelligenza artificiale dovrebbe allinearsi:

- Schemi abilitati per MCP
- visibilità delle query
- autorizzazioni di autenticazione
- Proprietà della chiave API

Se diversi client necessitano di visibilità diversa, utilizza identità o profili di autorizzazione diversi anziché un'unica superficie MCP globale senza restrizioni.

## 11. Pensa in "ordine di lettura dell'IA"

Un tipico flusso di lavoro dell'agente è:

1. elencare gli schemi
2. scegline uno per nome e descrizione
3. ispezionare lo schema e le descrizioni delle proprietà
4. dedurre il tipo di entità correlata
5. creare una query
6. leggere i risultati

Quindi il modello dovrebbe supportare quella sequenza in modo pulito.

Chiediti:

- l'agente può identificare lo schema corretto leggendo il nome e la descrizione?
- può comprendere le proprietà senza la conoscenza tribale nascosta?
- può dire quale query restituisce quale entità?
- può evitare schemi irrilevanti?

In caso contrario, arricchisci i metadati.

## 12. Ottimizza per meno viaggi di andata e ritorno

I sistemi di intelligenza artificiale pagano un prezzo per ogni fase di scoperta.

Per mantenere efficienti i consumi:

- Fornire descrizioni dettagliate dello schema
- descrivere bene le proprietà la prima volta
- mantenere focalizzate le superfici delle query
- esporre le proprietà dei risultati comunemente necessarie
- evitare di forzare l'agente a indovinare i significati e riprovare

Metadati validi riducono l'utilizzo dei token, riducono i tentativi e producono risultati più affidabili.

## Lista di controllo pratica per l'intelligenza artificiale

Prima di esporre un modello tramite `ChillSharp.Mcp`, verificare che:

- i nomi delle entità sono chiari
- I nomi delle query sono chiari
- tutte le proprietà rivolte all'IA sono annotate con `[ChillProperty]`
- Gli schemi abilitati per MCP hanno utili `MCPDescription`
- le proprietà importanti hanno utili `MCPDescription`
- le query sono mirate e mirate
- i riferimenti sono descritti
- Le etichette sono significative
- gli schemi sensibili o rumorosi rimangono non MCP
- I limiti di autorizzazione e autorizzazione corrispondono al caso d'uso AI previsto

## Esempio di frammento di modello compatibile con l'intelligenza artificiale

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

    [ChillProperty(
        UniquePropertyKeyString: "A18E7754-D8F7-45FE-B8A8-EA762A4EC9E6",
        PrimaryLanguageLabel: "Customer",
        SecondaryLanguageLabel: "Cliente",
        MCPDescription = "Customer that owns this invoice.",
        ReferenceChillTypeQuery = "Query.CustomerQuery")]
    public Customer? Customer { get; set; }

    [ChillProperty(
        UniquePropertyKeyString: "D6A6A0B6-3C22-4E18-B2AE-34D6EBE56EC8",
        PrimaryLanguageLabel: "Payment status",
        SecondaryLanguageLabel: "Stato pagamento",
        MCPDescription = "Current payment lifecycle status such as Draft, Issued, Paid, or Cancelled.")]
    public string PaymentStatus { get; set; } = string.Empty;
}
```

## Documenti correlati

- [Istruzioni per la connessione ChatGPT](ChatGPT.md)
- [../README.md](../README.md)
- [../RegisterContext.md](../RegisterContext.md)
- [../ModelPreparation.md](../ModelPreparation.md)
- [../AIAssistedDevelopment/README.md](../AIAssistedDevelopment/README.md)
- [../../ChillSharp.Mcp/README.md](../../ChillSharp.Mcp/README.md)
