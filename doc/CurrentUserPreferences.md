# Current User Preferences

Versione italiana: [Italiano](./it/CurrentUserPreferences.md)

`ChillSharp.Auth` can make the authenticated user's display preferences available to entity lifecycle hooks without querying `AuthUser` during every save.

The immutable `ChillUserPreferences` snapshot contains:

- `DisplayCultureName`
- `DisplayTimeZone`
- `DisplayDateFormat`
- `DisplayNumberFormat`
- `PreferredTheme`

## Registration And Cache Lifecycle

`AddChillAuthApi<TContext>()` registers a singleton `IChillAuthUserPreferencesCache` and scoped `IChillAuthUserPreferencesAccessor`.

When the user logs in or refreshes a token, ChillSharp loads the matching `AuthUser` once and warms a snapshot keyed by its `ExternalId`. The accessor only reads that in-memory snapshot during requests; it does not query the user table.

`ChillAuthService` refreshes the cached snapshot after creating or updating an `AuthUser`. It invalidates a deleted user's snapshot and both the previous and new keys when `ExternalId` changes.

The cache stores scalar preference values, never a tracked EF Core `AuthUser` instance.

## Current User Preferences API

Authenticated clients can retrieve the same snapshot used by server-side code with:

```http
GET /api/chill-auth/current-user-preferences
Authorization: Bearer <access token>
```

The response is a `ChillUserPreferences` JSON object:

```json
{
  "displayCultureName": "it-IT",
  "displayTimeZone": "Europe/Rome",
  "displayDateFormat": "dd/MM/yyyy",
  "displayNumberFormat": "N2",
  "preferredTheme": "cini"
}
```

Use this endpoint after login and when restoring an authenticated UI session. It is the source of truth for the language/culture, time zone, date format, and number format used by the API client and UI; do not derive those values from the browser, operating system, token claims, or an auth-management endpoint.

The C# client exposes `GetCurrentUserPreferences()`. The Python client exposes `get_current_user_preferences()`, while the TypeScript client and Angular wrapper expose `getCurrentUserPreferences()`. The Vue and React packages additionally provide `useCurrentUserPreferences()`.

In the Angular UI Core package, inject `ChillService` and consume its `userPreferences` signal or the individual `displayCultureName`, `displayTimeZone`, `displayDateFormat`, `displayNumberFormat`, and `preferredTheme` projections. Use them for UI text culture, date and number format/parse operations, UTC-to-local time conversion, and the selected theme.

`PreferredTheme` is an opaque string: the backend stores and returns it without knowing which themes a client provides. UI Core uses the browser's `prefers-color-scheme` choice (`bright` or `dark`) when there is no authenticated preference. Client applications can declare additional selectable themes when they register UI Core:

```ts
provideChillSharpUiCore({ additionalThemes: ['cini'] })
```

The built-in choices are `bright`, `dark`, and `soft`. If an authenticated user's stored value is not available in that client, UI Core falls back to the browser light/dark preference.

## Applying Preferences To MVC Requests

When `AddChillApi` is registered, ChillSharp applies display preferences to MVC requests before model binding. This makes the selected culture available to model binding, controller code, and response formatting without mutating shared culture instances.

For each setting, the authenticated user's cached `AuthUser` preference takes precedence over application configuration and environment variables. Missing or invalid values fall back to the configured value, then to the runtime defaults. `DisplayCultureName` sets both `CurrentCulture` and `CurrentUICulture` for the user. An optional `DisplayUiCultureName` setting can separate UI resource lookup from formatting when there is no user culture preference.

Application configuration uses these keys:

```json
{
    "ChillSharp": {
        "DisplayCultureName": "en-GB",
        "DisplayUiCultureName": "en",
        "DisplayTimeZone": "Europe/Rome",
        "DisplayDateFormat": "dd/MM/yyyy",
        "DisplayNumberFormat": "1.234,56",
        "ThousandSeparator": ".",
        "DecimalSeparator": ","
    }
}
```

Equivalent environment variables are `CHILLSHARP_DISPLAY_CULTURE_NAME`, `CHILLSHARP_DISPLAY_UI_CULTURE_NAME`, `CHILLSHARP_DISPLAY_TIME_ZONE`, `CHILLSHARP_DISPLAY_DATE_FORMAT`, `CHILLSHARP_DISPLAY_TIME_FORMAT`, `CHILLSHARP_DISPLAY_NUMBER_FORMAT`, `CHILLSHARP_THOUSAND_SEPARATOR`, and `CHILLSHARP_DECIMAL_SEPARATOR`. `DisplayTimeFormat` is accepted as an alias for the date-format fallback. The existing `CHILLSHARP_SYSTEM_TIMEZONE` remains a fallback for the time zone.

`DisplayDateFormat` uses a .NET short-date pattern; uppercase `DD` and `YYYY` tokens from existing user preferences are normalized. A `DisplayNumberFormat` sample such as `1.234,56` supplies the grouping and decimal separators. Explicit `ThousandSeparator` and `DecimalSeparator` settings override the application number-format sample; user separators inferred from `AuthUser.DisplayNumberFormat` take precedence over both. If no date or number override is supplied, the selected culture's `DateTimeFormat.ShortDatePattern` and `NumberFormat` are used. The time-zone fallback is `ChillSharpInitOptions.GetSystemTimeZone()`.

## Host DbContext

`IChillContext.GetCurrentUserPreferences()` returns `ChillUserPreferences.Empty` by default. In an auth-enabled custom host context, inject `IChillAuthUserPreferencesAccessor` and delegate to it:

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

The optional constructor parameter keeps design-time creation and tests that construct the context directly compatible. A normal dependency-injection-created context receives the accessor.

`ChillAuthDbContext` already implements this pattern.

## Entity Lifecycle Hooks

Use the snapshot directly from the supplied `IChillContext`. No database access is required in the hook.

```csharp
public override void OnUpdate(IChillContext context)
{
    var preferences = context.GetCurrentUserPreferences();
    var timeZone = preferences.DisplayTimeZone;

    if (!string.IsNullOrWhiteSpace(timeZone))
    {
        // Apply application-specific time-zone behavior.
    }
}
```

All values can be empty for background jobs, unauthenticated calls, or a user without a matching `AuthUser`. Treat them as optional and choose an application fallback. Validate `DisplayTimeZone` before using it as a time-zone identifier.
