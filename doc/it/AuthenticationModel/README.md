# Autenticazione E Autorizzazione ChillSharp

Versione originale in inglese: [English](../../AuthenticationModel/README.md)

Questo documento copre il modulo auth a livello di riferimento. Per una configurazione guidata, continua a usare [doc/HowTo/03-authentication.md](../HowTo/03-authentication.md).

## Separazione Del Modulo

`ChillSharp.Auth` espone due aree correlate ma distinte:

- autenticazione account
  Flussi di register, login, refresh, cambio password e reset password basati su ASP.NET Core Identity

- gestione autorizzazione
  utenti auth, ruoli, assegnazioni ruolo, regole di permesso e valutazione dei permessi

## Endpoint Account

Registrati tramite:

```csharp
builder.Services.AddChillAuthIdentityApi<AppDbContext, IdentityUser>();
```

Route tipiche:

- `/api/chill-auth/account/register`
- `/api/chill-auth/account/login`
- `/api/chill-auth/account/refresh`
- `/api/chill-auth/account/change-password`
- `/api/chill-auth/account/request-password-reset`
- `/api/chill-auth/account/reset-password`

Queste sono le route usate internamente da `ChillSharpClient`.

`RegisterAuthIdentityRequest` supporta anche:

- `DisplayName`
- `DisplayCultureName`
- `CreateChillAuthUser`

Quando `CreateChillAuthUser` e abilitato e viene fornito `DisplayCultureName`, il relativo `AuthUser` viene preimpostato automaticamente con:

- `DisplayCultureName`
- `DisplayTimeZone`
- `DisplayDateFormat`
- `DisplayNumberFormat`

Il server ricava questi valori dalla cultura selezionata usando una mappatura best effort del fuso orario piu i separatori di data e numeri della cultura.

## Endpoint Di Gestione Autorizzazione

Registrati tramite:

```csharp
builder.Services.AddChillAuthApi<AppDbContext>();
```

Gruppi di route tipici:

- `/api/chill-auth/users`
- `/api/chill-auth/roles`
- `/api/chill-auth/permissions`

Questi endpoint gestiscono:

- `AuthUser`
- `AuthRole`
- `AuthUserRole`
- `AuthPermissionRule`
- risultati di valutazione dei permessi

`AuthUser` ora include anche preferenze di visualizzazione opzionali per la UI:

- `DisplayCultureName`
- `DisplayTimeZone`
- `DisplayDateFormat`
- `DisplayNumberFormat`

## Requisiti Del Contesto

Il contesto host deve:

- implementare `IChillAuthDbContext`
- includere `modelBuilder.AddChillAuthModel()`

Per account basati su Identity, il contesto deve anche essere uno store EF valido per ASP.NET Core Identity, tipicamente:

```csharp
public class AppDbContext : IdentityDbContext<IdentityUser>, IChillContext, IChillAuthDbContext
{
}
```

## Bootstrap Dell'Utente Root

`AddChillAuthIdentityApi(...)` puo inizializzare un account root all'avvio.

Configurazioni supportate:

- opzioni dirette nel codice
- variabili d'ambiente

Variabili d'ambiente predefinite:

- `CHILLSHARP_AUTH_ROOT_USERNAME`
- `CHILLSHARP_AUTH_ROOT_PASSWORD`
- `CHILLSHARP_AUTH_ROOT_EMAIL`
- `CHILLSHARP_AUTH_ROOT_DISPLAY_NAME`

Quando abilitato, il flusso di bootstrap puo creare anche il collegato `AuthUser` ChillSharp con accesso alla gestione permessi.

## Accesso Alla Gestione Permessi

Essere autenticati non basta per gestire il modulo auth.

Gli endpoint di gestione auth richiedono un utente auth ChillSharp con diritti di gestione permessi, tipicamente:

- `CanManagePermissions = true`

Questo e il problema critico di bootstrap in un database pulito:

- il primo utente Identity registrato puo esistere
- il relativo `AuthUser` puo esistere
- ma nessuno puo ancora avere i diritti per gestire ruoli e permessi

Per questo il bootstrap root o un altro percorso di setup trusted e importante.

## Uso Del Client

Usa il normale Chill base URL:

```csharp
var client = new ChillSharpClient("http://localhost:5000/api/chill");
```

`ChillSharpClient` passa automaticamente a `/api/chill-auth/...` per i metodi auth.

Esempi:

- `RegisterAuthAccount`
- `LoginAuthAccount`
- `RefreshAuthAccount`
- `ChangeAuthPassword`
- `CreateAuthRole`
- `CreateAuthPermissionRule`
- `EvaluateAuthEntityPermission`

I payload di lista utente e dettaglio utente espongono anche:

- `DisplayCultureName`
- `DisplayTimeZone`
- `DisplayDateFormat`
- `DisplayNumberFormat`

## Gestione Dei Token

`ChillSharpClient` conserva:

- access token
- refresh token

Se e presente un refresh token, il client puo rinnovare automaticamente l'access token durante chiamate autenticate successive.

## Relazione Con Il Modello Permessi

Le regole esatte di risoluzione permessi sono documentate separatamente in:

- [PermissionModel/README.md](../PermissionModel/README.md)

Usa quel documento per il modello di precedenza e scope. Usa questo per registrazione e flusso auth runtime.
