# Preferenze Dell'Utente Corrente

English version: [English](../CurrentUserPreferences.md)

`ChillSharp.Auth` puo rendere disponibili agli hook del ciclo di vita delle entita le preferenze di visualizzazione dell'utente autenticato senza interrogare `AuthUser` a ogni salvataggio.

Lo snapshot immutabile `ChillUserPreferences` contiene:

- `DisplayCultureName`
- `DisplayTimeZone`
- `DisplayDateFormat`
- `DisplayNumberFormat`

## Registrazione E Ciclo Di Vita Della Cache

`AddChillAuthApi<TContext>()` registra il singleton `IChillAuthUserPreferencesCache` e lo scoped `IChillAuthUserPreferencesAccessor`.

Quando l'utente esegue il login, ChillSharp carica una volta l'`AuthUser` corrispondente e inizializza uno snapshot associato al suo `ExternalId`. L'accessor legge solo quello snapshot in memoria durante le richieste; non interroga la tabella utenti.

`ChillAuthService` aggiorna lo snapshot in cache dopo la creazione o l'aggiornamento di un `AuthUser`. Invalida lo snapshot di un utente eliminato ed entrambe le chiavi, precedente e nuova, quando cambia `ExternalId`.

La cache memorizza valori scalari delle preferenze, mai un'istanza EF Core tracciata di `AuthUser`.

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
