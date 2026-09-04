# Documentazione ChillSharp

Versione originale in inglese: [English](../README.md)

Questa cartella contiene la documentazione di riferimento di ChillSharp.

`doc/HowTo` resta intenzionalmente la sezione tutorial. Usala per esempi guidati e incrementali. Il resto di questa cartella e la versione di riferimento: concetti, registrazione, permessi, autenticazione e workflow di generazione client.

## Mappa Della Documentazione

- [ModelPreparation.md](./ModelPreparation.md)
  Prepara un modello EF Core in modo che ChillSharp possa attivare le entita, eseguire gli hook del ciclo di vita, generare metadati di schema e salvare i campi di audit.

- [ReferenceExistence.md](./ReferenceExistence.md)
  Verifica se un riferimento EF Core ha valori di chiave esterna senza caricare l'entita correlata, anche con database senza vincoli FK applicati.

- [AutomaticQuery/README.md](./AutomaticQuery/README.md)
  Costruisce filtri compatibili con il provider per valori CLR, riferimenti a entità Chill, percorsi annidati e collezioni, mantenendo la pipeline di query standard.

- [RegisterContext.md](./RegisterContext.md)
  Registra i moduli ChillSharp su un `DbContext` host e mappa la superficie API.

- [AuthenticationModel/README.md](./AuthenticationModel/README.md)
  Flussi account basati su Identity, endpoint di gestione auth, strategie di bootstrap e configurazione di API protette.

- [CurrentUserPreferences.md](./CurrentUserPreferences.md)
  Preferenze in cache di cultura, fuso orario e formati dell'utente autenticato per `IChillContext` e gli hook del ciclo di vita delle entita.

- [PermissionModel/README.md](./PermissionModel/README.md)
  Il modello di permessi usato da `ChillSharp.Auth`, incluse precedenza, scope e modalita di risoluzione dell'accesso a entita e proprieta.

- [ClientGeneration/README.md](./ClientGeneration/README.md)
  Genera librerie client da un host ChillSharp per TypeScript e Python usando un documento OpenAPI esposto dall'applicazione host.

- [../../ext/chill-sharp-ts-client/README.md](../../ext/chill-sharp-ts-client/README.md)
  Client TypeScript generico per servizi ChillSharp.

- [../../ext/chill-sharp-react-client/README.md](../../ext/chill-sharp-react-client/README.md)
  Provider e hook React costruiti sopra il client TypeScript generico.

- [../../ext/chill-sharp-vue-client/README.md](../../ext/chill-sharp-vue-client/README.md)
  Plugin e composable Vue costruiti sopra il client TypeScript generico.

- [../../ext/chill-sharp-ng-client/README.md](../../ext/chill-sharp-ng-client/README.md)
  Helper DI Angular e servizio RxJS costruiti sopra il client TypeScript generico.

- [../../ext/chill-sharp-py-client/README.md](../../ext/chill-sharp-py-client/README.md)
  Client Python generico per servizi ChillSharp.

## Moduli Principali

- `ChillSharp`
  Motore core per entita, motore DTO e superficie HTTP API.

- `ChillSharp.Schema`
  Generazione schema, persistenza e cache dello schema.

- `ChillSharp.Auth`
  Modello di autorizzazione, regole di permesso, gestione ruoli/utenti e integrazione opzionale con ASP.NET Core Identity.

- `ChillSharp.I18n`
  Endpoint di lookup per label e testi piu cache i18n in memoria.

- `ChillSharp.Client`
  Client .NET per gli endpoint ChillSharp e ChillSharp.Auth.

- `ext/chill-sharp-ts-client`
  Pacchetto client TypeScript generico.

- `ext/chill-sharp-react-client`
  Pacchetto di integrazione React sopra il client TypeScript.

- `ext/chill-sharp-vue-client`
  Pacchetto di integrazione Vue sopra il client TypeScript.

- `ext/chill-sharp-ng-client`
  Pacchetto di integrazione Angular sopra il client TypeScript.

- `ext/chill-sharp-py-client`
  Pacchetto client Python generico.

## Concetti Base

### `IChillContext`

Il tuo contesto EF Core deve implementare `IChillContext`. Definisce:

- il prefisso Chill type usato per l'attivazione dinamica
- le culture primaria e secondaria usate per interpretare le label di schema
- il nome utente corrente usato dal tracciamento di audit delle entita

Possono coesistere contesti diversi con valori diversi. ChillSharp non assume una singola configurazione globale.

### `ChillEntity`

`ChillEntity` e la classe base consigliata per i tipi di modello esposti tramite ChillSharp. Fornisce gia:

- `Guid`
- `Label`, `ShortLabel`, `FullTextContent`
- `Checksum`, `LastUpdateUser`, `LastUpdate`, `LastUpdateUtcOffset`
- comportamento di ciclo di vita predefinito

Gli hook disponibili sono:

- `OnCreate`
- `OnUpdate`
- `OnAfterUpdate`
- `OnDelete`
- `OnAfterDelete`
- `OnSelect`
- `OnInflate`
- `OnAutocomplete`

### Metadati Di Schema

`ChillEntityAttribute` e `ChillPropertyAttribute` forniscono:

- chiavi univoche stabili
- `PrimaryLanguageLabel`
- `SecondaryLanguageLabel`

Quando ChillSharp costruisce uno schema, risolve quelle label usando:

- `CultureInfo.CurrentUICulture`
- `IChillContext.GetPrimaryCultureName()`
- `IChillContext.GetSecondaryCultureName()`

### Campi Di Audit

Dopo gli aggiornamenti, ChillSharp salva automaticamente:

- `Checksum`
- `LastUpdateUser`
- `LastUpdate`
- `LastUpdateUtcOffset`

La logica di audit e applicata tramite il percorso `IChillEntity` usato da `ChillEngine`, quindi una classe derivata puo ridefinire `OnAfterUpdate()` senza saltare l'aggiornamento di audit della base.

## Superficie API

La superficie API core viene esposta da:

- `app.MapChillApi()`

Questo mappa i controller Chill API e include anche:

- `/api/chill/test`
- `/api/chill/license`

A seconda dei moduli registrati, lo stesso host puo esporre anche:

- servizi schema tramite `ChillSharp.Schema`
- servizi auth/account e di gestione permessi tramite `ChillSharp.Auth`
- endpoint testo i18n tramite `ChillSharp.I18n`

## Riferimento Vs How-To

Usa questa separazione in modo coerente:

- `doc/HowTo`
  Tutorial passo-passo. Mantienili focalizzati e orientati al task.

- il resto di `doc/`
  Documentazione di riferimento. Usa questi file quando ti servono modello, registrazione, architettura, regole di permesso o dettagli di integrazione.

## How-To

I tutorial esistenti sono disponibili anche in italiano:

- [HowTo/01-simple-blog-sqlite.md](./HowTo/01-simple-blog-sqlite.md)
- [HowTo/02-blog-schema-labels.md](./HowTo/02-blog-schema-labels.md)
- [HowTo/03-authentication.md](./HowTo/03-authentication.md)
- [HowTo/04-blog-posts-one-to-many.md](./HowTo/04-blog-posts-one-to-many.md)
- [HowTo/05-docker-env-variables.md](./HowTo/05-docker-env-variables.md)
- [HowTo/06-chunk-transactions-autocomplete.md](./HowTo/06-chunk-transactions-autocomplete.md)
