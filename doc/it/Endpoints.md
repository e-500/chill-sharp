# Endpoint ChillSharp

Versione originale in inglese: [English](../Endpoints.md)


Questo documento elenca gli endpoint HTTP, SignalR e MCP esposti dai moduli API ChillSharp integrati.

## Percorso base

Gli endpoint ChillSharp sono montati in un percorso base API configurabile.

Predefinito:

```text
/api
```

Configurazione:

```text
CHILLSHARP_API_BASE_PATH=/api
```

o nel codice:

```csharp
builder.Services.AddChillApi<MyDbContext>(options =>
{
    options.ApiBasePath = "/api";
});

app.MapChillApi();
```

Gli esempi seguenti utilizzano il percorso di base predefinito `/api`. Se imposti `CHILLSHARP_API_BASE_PATH=/backend`, sostituisci `/api` con `/backend`.

## Radice e diagnostica

Questi endpoint sono registrati da `MapChillApi()`.

| Metodo | Percorso | Descrizione |
| --- | --- | --- |
| OTTIENI |  | Risposta sanitaria di base di ChillSharp, abbinata anche quando i chiamanti richiedono `/api/`. |
| OTTIENI |  | Risposta sanitaria di base di ChillSharp. |
| OTTIENI |  | Restituisce la licenza ChillSharp e i metadati del progetto. |

## API DTO principale

Questi endpoint sono abilitati da `AddChillApi<TContext>()` e sono disponibili quando viene registrata l'API Chill di base.

Itinerario base:

```text
/api/chill
```

| Metodo | Percorso | Descrizione |
| --- | --- | --- |
| POST |  | Esegue un `ChillDtoQuery` dinamico. |
| POST |  | Esegue una ricerca full-text rispetto al tipo di entità richiesto. |
| POST |  | Trova un'entità per tipo e GUID. |
| POST |  | Crea un'entità da `ChillDtoEntity`. |
| POST |  | Aggiorna un'entità da `ChillDtoEntity`. |
| POST |  | Elimina un'entità identificata da `ChillDtoEntity`. |
| POST |  | Esegue la logica di completamento automatico per un'entità o una query DTO. |
| POST |  | Esegue la convalida per un'entità o una query DTO. |
| POST |  | Esegue un elenco di operazioni di raffreddamento in una richiesta. |

Quando i servizi ACL dell'entità vengono registrati e il chiamante viene autenticato, questi endpoint possono anche applicare autorizzazioni a livello di entità.

## Notifiche SignalR

L'hub di notifica è registrato da `MapChillApi()`.

| Protocollo | Percorso | Descrizione |
| --- | --- | --- |
| SegnaleR |  | Hub per le notifiche di modifica dell'entità. |

Metodi dell'hub:

| Metodo | Parametri | Descrizione |
| --- | --- | --- |
|  | `chillType`, opzionale `guid` | Sottoscrive la connessione a tutte le modifiche per un tipo o un'entità. |
|  | `chillType`, opzionale `guid` | Rimuove un abbonamento precedente. |

Metodo da server a client:

| Metodo | Descrizione |
| --- | --- |
|  | Inviato quando le entità sottoscritte cambiano. |

## API di autenticazione

Abilitato quando `ChillApiOptions.EnableAuthApi` è `true` e il contesto implementa `IChillAuthDbContext`.

Itinerario base:

```text
/api/chill-auth
```

### Endpoint dell'account

| Metodo | Percorso | Aut. | Descrizione |
| --- | --- | --- | --- |
| POST |  | Anonimo | Registra un nuovo account di identità e restituisce i token. |
| POST |  | Anonimo | Autentica e restituisce token. |
| POST |  | Anonimo | Scambia un token di aggiornamento con nuovi token. |
| POST |  | Obbligatorio | Revoca la sessione corrente. |
| POST |  | Obbligatorio | Modifica la password dell'utente corrente. |
| POST |  | Anonimo | Richiede o genera un token di reimpostazione della password. |
| POST |  | Anonimo | Reimposta una password con un token di reimpostazione. |

### Utente corrente e metadati

| Metodo | Percorso | Aut. | Descrizione |
| --- | --- | --- | --- |
| OTTIENI |  | Facoltativo | Restituisce le autorizzazioni dirette, di ruolo e derivate dal ruolo per l'utente corrente. |
| OTTIENI |  | Gestione | Restituisce un elenco utenti semplificato per le interfacce utente di gestione. |
| OTTIENI |  | Gestione | Restituisce un elenco di ruoli semplificato per le interfacce utente di gestione. |
| OTTIENI |  | Gestione | Restituisce i moduli logici disponibili. |
| OTTIENI |  | Gestione | Restituisce le entità per un modulo. |
| OTTIENI |  | Gestione | Restituisce le query per un modulo. |
| OTTIENI |  | Gestione | Restituisce le proprietà per un tipo Chill. |

### Utenti

| Metodo | Percorso | Aut. | Descrizione |
| --- | --- | --- | --- |
| OTTIENI |  | Gestione | Elenca gli utenti autorizzati. |
| OTTIENI |  | Gestione | Ottiene un utente con autorizzazione. |
| POST |  | Gestione | Crea un utente di autorizzazione. |
| METTERE |  | Gestione | Aggiorna un utente di autorizzazione. |
| ELIMINA |  | Gestione | Elimina un utente autorizzato. |
| OTTIENI |  | Gestione | Elenca i ruoli assegnati a un utente. |
| METTERE |  | Gestione | Assegna un ruolo a un utente. |
| ELIMINA |  | Gestione | Rimuove un ruolo da un utente. |

### Ruoli

| Metodo | Percorso | Aut. | Descrizione |
| --- | --- | --- | --- |
| OTTIENI |  | Gestione | Elenca i ruoli. |
| OTTIENI |  | Gestione | Ottiene un ruolo. |
| POST |  | Gestione | Crea un ruolo. |
| METTERE |  | Gestione | Aggiorna un ruolo. |
| ELIMINA |  | Gestione | Elimina un ruolo. |

### Regole di autorizzazione

| Metodo | Percorso | Aut. | Descrizione |
| --- | --- | --- | --- |
| OTTIENI |  | Gestione | Elenca le regole di autorizzazione, facoltativamente filtrate da `userGuid` o `roleGuid`. |
| OTTIENI |  | Gestione | Ottiene una regola di autorizzazione. |
| POST |  | Gestione | Crea una regola di autorizzazione. |
| METTERE |  | Gestione | Aggiorna una regola di autorizzazione. |
| ELIMINA |  | Gestione | Elimina una regola di autorizzazione. |

`Management` significa che l'endpoint è protetto da `ChillAuthManagementAccessFilter`.

## API dello schema

Abilitato quando `ChillApiOptions.EnableSchemaApi` è `true` e il contesto implementa `IChillSchemaDbContext`.

Itinerario base:

```text
/api/chill-schema
```

| Metodo | Percorso | Aut. | Descrizione |
| --- | --- | --- | --- |
| OTTIENI |  | Dipende dalla protezione API globale | Ottiene uno schema. |
| OTTIENI |  | Dipende dalla protezione API globale | Elenca i riepiloghi delle entità e degli schemi di query. |
| POST |  | Gestione dello schema | Crea o aggiorna uno schema. |
| OTTIENI |  | Gestione dello schema | Ottiene le opzioni dello schema per un tipo di entità. |
| POST |  | Gestione dello schema | Crea o aggiorna le opzioni dell'entità. |
| OTTIENI |  | Dipende dalla protezione API globale | Restituisce le voci del menu, filtrate in base ai metadati di autenticazione quando disponibili. |
| POST |  | Gestione dello schema | Crea o aggiorna una voce di menu. |
| ELIMINA |  | Gestione dello schema | Elimina una voce di menu. |

`Schema management` significa che l'endpoint è protetto da `ChillSchemaManagementAccessFilter`.

## API I18n

Abilitato quando `ChillApiOptions.EnableI18nApi` è `true` e il contesto implementa `IChillI18nDbContext`.

Itinerario base:

```text
/api/chill-i18n
```

| Metodo | Percorso | Aut. | Descrizione |
| --- | --- | --- | --- |
| POST |  | Anonimo | Ottiene un testo localizzato. |
| POST |  | Anonimo | Ottiene più testi localizzati. |
| METTERE |  | Dipende dalla protezione API globale | Crea o aggiorna il testo localizzato. |

## API per gli allegati

Abilitato quando `ChillApiOptions.EnableAttachmentApi` è `true` e il contesto implementa `IChillAttachmentDbContext`.

Itinerario base:

```text
/api/chill-attachment
```

| Metodo | Percorso | Aut. | Descrizione |
| --- | --- | --- | --- |
| OTTIENI |  | Anonimo per i file pubblici; autenticato per file privati ​​| Scarica un allegato archiviato. |
| POST |  | Dipende dalla protezione API globale | Carica uno o più file come dati del modulo multiparte. |

Carica i campi del modulo:

| Campo | Obbligatorio | Descrizione |
| --- | --- | --- |
|  | Sì | Tipo di raffreddamento dell'entità a cui appartiene l'allegato. |
|  | Sì | GUID dell'entità a cui appartiene l'allegato. |
|  | Sì | Uno o più file caricati. |
|  | No | Visualizza il titolo. Il valore predefinito è il nome file senza estensione. |
|  | No | Descrizione dell'allegato facoltativa. |
|  | No | Se i chiamanti anonimi possono scaricare il file. |

## API MCP

Abilitato quando `ChillApiOptions.EnableMcpApi` è `true`, `ChillMcpOptions.Enabled` è `true` e il contesto implementa `IChillSchemaDbContext`.

Percorso predefinito:

```text
/api/chill-mcp
```

L'endpoint MCP viene registrato tramite `MapMcp(...)` dal Model Context Protocol ASP.NET Core SDK. Il suo comportamento HTTP segue il contratto di trasporto dell'SDK MCP.

Puoi sovrascrivere direttamente il percorso MCP:

```csharp
builder.Services.AddChillMcpApi<MyDbContext>(options =>
{
    options.RoutePattern = "/api/chill-mcp";
});
```

Se `RoutePattern` è relativo, ad esempio `chill-mcp`, ChillSharp lo inserisce nel percorso base API configurato.

## Regole di protezione

`ChillApiOptions.ProtectedApi` applica l'autorizzazione agli endpoint del controller mappato e all'hub SignalR. Alcuni endpoint consentono esplicitamente l'accesso anonimo, ad esempio login/registrazione di autenticazione, endpoint di lettura i18n e download di allegati pubblici.

Gli endpoint di gestione specifici del modulo aggiungono filtri più rigidi:

| Filtra | Utilizzato da |
| --- | --- |
|  | Utenti di autenticazione, ruoli, autorizzazioni e metadati di gestione. |
|  | Operazioni di scrittura di schemi e menu. |

I controlli ACL dell'entità possono essere applicati anche alle operazioni DTO principali e agli allegati quando è registrato un `IChillEntityAclService`.
