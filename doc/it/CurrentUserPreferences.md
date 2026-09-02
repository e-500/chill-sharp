# Preferenze Dell'Utente Corrente

English version: [English](../CurrentUserPreferences.md)

`ChillSharp.Auth` puo rendere disponibili agli hook del ciclo di vita delle entita le preferenze di visualizzazione dell'utente autenticato senza interrogare `AuthUser` a ogni salvataggio.

Lo snapshot immutabile `ChillUserPreferences` contiene:

- `DisplayCultureName`
- `DisplayTimeZone`
- `DisplayDateFormat`
- `DisplayNumberFormat`
- `PreferredTheme`

## Registrazione E Ciclo Di Vita Della Cache

`AddChillAuthApi<TContext>()` registra il singleton `IChillAuthUserPreferencesCache` e lo scoped `IChillAuthUserPreferencesAccessor`.

Quando l'utente esegue il login o rinnova un token, ChillSharp carica una volta l'`AuthUser` corrispondente e inizializza uno snapshot associato al suo `ExternalId`. L'accessor legge solo quello snapshot in memoria durante le richieste; non interroga la tabella utenti.

`ChillAuthService` aggiorna lo snapshot in cache dopo la creazione o l'aggiornamento di un `AuthUser`. Invalida lo snapshot di un utente eliminato ed entrambe le chiavi, precedente e nuova, quando cambia `ExternalId`.

La cache memorizza valori scalari delle preferenze, mai un'istanza EF Core tracciata di `AuthUser`.

## API delle preferenze dell'utente corrente

I client autenticati possono recuperare lo stesso snapshot utilizzato dal codice server con:

```http
GET /api/chill-auth/current-user-preferences
Authorization: Bearer <access token>
```

La risposta e un oggetto JSON `ChillUserPreferences`:

```json
{
  "displayCultureName": "it-IT",
  "displayTimeZone": "Europe/Rome",
  "displayDateFormat": "dd/MM/yyyy",
  "displayNumberFormat": "N2",
  "preferredTheme": "cini"
}
```

Usa questo endpoint dopo il login e durante il ripristino di una sessione UI autenticata. E la fonte autorevole per lingua/cultura, fuso orario, formato data e formato numerico usati dal client API e dalla UI; non ricavare tali valori dal browser, dal sistema operativo, dalle claim del token o da un endpoint di gestione dell'autenticazione.

Il client C# espone `GetCurrentUserPreferences()`. Il client Python espone `get_current_user_preferences()`, mentre il client TypeScript e il wrapper Angular espongono `getCurrentUserPreferences()`. I pacchetti Vue e React forniscono inoltre `useCurrentUserPreferences()`.

Nel pacchetto Angular UI Core, inietta `ChillService` e usa il signal `userPreferences` oppure le singole proiezioni `displayCultureName`, `displayTimeZone`, `displayDateFormat`, `displayNumberFormat` e `preferredTheme`. Usali per la cultura dei testi UI, le operazioni di formato/analisi di date e numeri, la conversione degli orari UTC in locali e il tema selezionato.

`PreferredTheme` e una stringa opaca: il backend la salva e la restituisce senza conoscere i temi forniti dal client. UI Core usa la scelta del browser `prefers-color-scheme` (`bright` o `dark`) quando non esiste una preferenza autenticata. Le applicazioni client possono dichiarare temi aggiuntivi selezionabili durante la registrazione di UI Core:

```ts
provideChillSharpUiCore({ additionalThemes: ['cini'] })
```

Le scelte integrate sono `bright`, `dark` e `soft`. Se il valore salvato di un utente autenticato non e disponibile nel client, UI Core torna alla preferenza chiara/scura del browser.

## DbContext Host

Per impostazione predefinita `IChillContext.GetCurrentUserPreferences()` restituisce `ChillUserPreferences.Empty`. In un contesto host personalizzato con auth abilitata, inietta `IChillAuthUserPreferencesAccessor` e delega a esso:

```csharp
using ChillSharp;
using ChillSharp.Auth.Services;

public class AppDbContext : DbContext, IChillContext, IChillAuthDbContext
{
    private readonly IChillAuthUserPreferencesAccessor? _userPreferencesAccessor;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IChillAuthUserPreferencesAccessor? userPreferencesAccessor = null)
        : base(options)
    {
        _userPreferencesAccessor = userPreferencesAccessor;
    }

    public ChillUserPreferences GetCurrentUserPreferences() =>
        _userPreferencesAccessor?.Current ?? ChillUserPreferences.Empty;

    public string GetDefaultUserCultureName()
    {
        var cultureName = GetCurrentUserPreferences().DisplayCultureName;
        return string.IsNullOrWhiteSpace(cultureName)
            ? GetPrimaryCultureName()
            : cultureName;
    }
}
```

Il parametro opzionale del costruttore mantiene compatibile la creazione a design-time e i test che costruiscono direttamente il contesto. Un contesto creato normalmente dalla dependency injection riceve l'accessor.

`ChillAuthDbContext` implementa gia questo schema.

## Hook Del Ciclo Di Vita Delle Entita

Usa lo snapshot direttamente dall'`IChillContext` fornito. Nell'hook non e necessario alcun accesso al database.

```csharp
public override void OnUpdate(IChillContext context)
{
    var preferences = context.GetCurrentUserPreferences();
    var timeZone = preferences.DisplayTimeZone;

    if (!string.IsNullOrWhiteSpace(timeZone))
    {
        // Applica il comportamento relativo al fuso orario dell'applicazione.
    }
}
```

Tutti i valori possono essere vuoti per job in background, chiamate non autenticate o un utente senza un `AuthUser` corrispondente. Considerali opzionali e scegli un fallback dell'applicazione. Valida `DisplayTimeZone` prima di usarlo come identificatore di fuso orario.
