# Riferimento alla configurazione di ChillSharp

Versione originale in inglese: [English](../../Configuration/README.md)


Questo documento elenca le variabili di ambiente attualmente utilizzate da ChillSharp e dall'host ChillSharp di esempio in `ChillSharp.Examples/BloggingApiService`.

Utilizzalo come riferimento rapido durante la configurazione di Docker, `docker compose` o un'altra destinazione di distribuzione.

All'avvio, `AddChillApi<TContext>()` scrive tutte le variabili di ambiente del processo `CHILLSHARP_` e `CHILL_SHARP_` nella console. I nomi delle variabili contenenti `PASSWORD` vengono mascherati come `********`.

## Ospitalità

| Opzione | Variabile ENV | Descrizione | Predefinito |
| --- | --- | --- | --- |
| URL di ascolto ASP.NET Core |  | URL associati dall'host ASP.NET Core. |  |
| Ambiente ASP.NET Core |  | Nome dell'ambiente ASP.NET Core standard. | `Development` nell'esempio `.env` |

## API principale

| Opzione | Variabile ENV | Descrizione | Predefinito |
| --- | --- | --- | --- |
| Percorso del database SQLite |  | Percorso del file utilizzato dal database SQLite di esempio `BloggingContext`. |  |
| Cultura primaria |  | Valore restituito da `IChillContext.GetPrimaryCultureName()`. |  |
| Cultura secondaria |  | Valore restituito da `IChillContext.GetSecondaryCultureName()`. |  |
| API principale protetta |  | Richiede l'autenticazione per l'API ChillSharp principale quando `true`. | `true` quando l'autenticazione è abilitata |
| Fuso orario del sistema DTO |  | ID fuso orario IANA utilizzato dagli helper di analisi e serializzazione ChillSharp DTO `DateTime` e `DateTimeOffset`. | ZZGETTONE PROTETTO3ZZ |

## Attiva/disattiva il modulo

| Opzione | Variabile ENV | Descrizione | Predefinito |
| --- | --- | --- | --- |
| Abilita modulo schema |  | Registra i servizi `ChillSharp.Schema`. |  |
| Abilita modulo di autenticazione |  | Registra l'account `ChillSharp.Auth` e i servizi di gestione dell'autenticazione. |  |
| Abilita modulo i18n |  | Registra i servizi `ChillSharp.I18n`. |  |
| Abilita modulo MCP |  | Registra i servizi `ChillSharp.Mcp` e mappa l'endpoint MCP quando il contesto host supporta i metadati dello schema. |  |
| Abilita modulo allegati |  | Registra i servizi e gli endpoint `ChillSharp.Attachment` quando il contesto host supporta gli allegati. | `false` nell'host di esempio, imposta `true` quando il contesto implementa gli allegati |

## Archiviazione degli allegati

| Opzione | Variabile ENV | Descrizione | Predefinito |
| --- | --- | --- | --- |
| Radice archivio allegati |  | Cartella principale utilizzata da `ChillSharp.Attachment` per leggere e archiviare file archiviati. | `attachments` nella directory di base dell'host |

## Token di autenticazione e flussi di password

| Opzione | Variabile ENV | Descrizione | Predefinito |
| --- | --- | --- | --- |
| Durata del token di accesso |  | Minuti prima della scadenza del token di accesso portatore ChillSharp. Leggere direttamente da `ChillAuthIdentityApiOptions` e `ChillIdentityApiOptions` a meno che l'host non sovrascriva `AccessTokenLifetime` nel codice. | ZZGETTONE PROTETTO4ZZ |
| Durata del token di aggiornamento |  | Giorni prima della scadenza del token di aggiornamento. Leggere direttamente da `ChillAuthIdentityApiOptions` e `ChillIdentityApiOptions` a meno che l'host non sovrascriva `RefreshTokenLifetime` nel codice. | ZZGETTONE PROTETTO4ZZ |
| Restituisci il token di reimpostazione nella risposta API |  | Include `userId` e `resetToken` nella risposta `/api/chill-auth/account/request-password-reset` quando `true`. | `false` nell'host di esempio |
| Invia e-mail di reimpostazione della password |  | Invia un'e-mail di reimpostazione della password tramite SMTP quando `true`. | `false` nel codice, `true` nell'esempio `.env` |
| Oggetto dell'e-mail di reimpostazione della password |  | Oggetto utilizzato per le email di reimpostazione della password. |  |
| URL di reimpostazione password |  | URL frontend facoltativo utilizzato per creare un collegamento selezionabile per la reimpostazione della password con `userId` e `resetToken`. | non impostato |

## Consegna con reimpostazione password SMTP

| Opzione | Variabile ENV | Descrizione | Predefinito |
| --- | --- | --- | --- |
| Host SMTP |  | Nome host del server SMTP utilizzato per la consegna senza risposta con reimpostazione della password. | non impostato |
| Porta SMTP |  | Porta del server SMTP. |  |
| SMTP SSL/TLS |  | Abilita SSL/TLS sul client SMTP quando `true`. |  |
| Nome utente SMTP |  | Nome utente di autenticazione SMTP. | non impostato |
| Password SMTP |  | Password di autenticazione SMTP. | non impostato |
| E-mail del mittente senza risposta |  | Indirizzo e-mail del mittente utilizzato per le e-mail di reimpostazione della password. | non impostato |
| Nome visualizzato del mittente senza risposta |  | Nome visualizzato del mittente utilizzato per le e-mail di reimpostazione della password. | non impostato |

Quando `CHILLSHARP_AUTH_SEND_PASSWORD_RESET_EMAILS=true`, l'host SMTP e l'e-mail del mittente devono essere configurati altrimenti il ​​flusso di reimpostazione non riuscirà.

## Bootstrap utente root

Queste variabili vengono lette da `ChillAuthRootUserInitializer<TUser>` durante l'avvio quando è abilitata l'inizializzazione dell'utente root.

| Opzione | Variabile ENV | Descrizione | Predefinito |
| --- | --- | --- | --- |
| Inizializza l'utente root |  | Crea l'utente identità root all'avvio quando le credenziali sono disponibili. |  |
| Crea utente di autenticazione ChillSharp collegato |  | Crea inoltre l'oggetto ChillSharp `AuthUser` collegato con accesso alla gestione delle autorizzazioni. |  |
| Nome utente root |  | Nome di accesso per l'amministratore bootstrap. | non impostato |
| Password di root |  | Password per l'amministratore del bootstrap. | non impostato |
| E-mail di root |  | E-mail facoltativa per l'amministratore bootstrap. | non impostato |
| Nome visualizzato radice |  | Nome visualizzato copiato nel ChillSharp `AuthUser` collegato. | `Root` nel codice |

## Note

- La maggior parte delle variabili elencate qui utilizzano il prefisso `CHILLSHARP_*` dell'host di esempio. `CHILLSHARP_SYSTEM_TIMEZONE` è una variabile di runtime ChillSharp principale utilizzata direttamente dalla mappatura data/ora DTO.
- L'output della console di avvio include entrambe le variabili `CHILLSHARP_*` e `CHILL_SHARP_*`. I valori `PASSWORD` vengono mascherati, ma gli altri valori vengono stampati così come sono.
- `CHILLSHARP_AUTH_ACCESS_TOKEN_MINUTES` e `CHILLSHARP_AUTH_REFRESH_TOKEN_DAYS` sono impostazioni predefinite di autenticazione ChillSharp integrate. Sono accettati valori interi positivi; i valori non validi, zero o negativi rientrano nei valori predefiniti del codice.
- `CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT` viene letto direttamente da `ChillSharp.Attachment` e dovrebbe puntare a un volume persistente in Docker.
- `CHILLSHARP_SYSTEM_TIMEZONE` prevede un ID fuso orario IANA come `Europe/Rome` o `America/New_York`.
- `CHILLSHARP_SYSTEM_TIMEZONE` influisce su `DateTime` e alcuni percorsi di normalizzazione `DateTimeOffset`. `DateOnly` e `TimeOnly` mantengono l'output di stringa .NET standard.
- Le variabili `CHILLSHARP_*` elencate qui vengono utilizzate da ChillSharp stesso o dal codice di avvio dell'host di esempio.
- Se crei la tua applicazione host, puoi mantenere questi nomi o la configurazione della mappa in modo diverso nel tuo codice di avvio.
- Per esempi di distribuzione, vedere anche [doc/HowTo/05-docker-env-variables.md](../HowTo/05-docker-env-variables.md).
- Per riferimenti ed esempi completi sulla serializzazione di data/ora, vedere [doc/DateTimeSerialization.md](../DateTimeSerialization.md).
