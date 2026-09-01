# Current User Preferences

Versione italiana: [Italiano](./it/CurrentUserPreferences.md)

`ChillSharp.Auth` can make the authenticated user's display preferences available to entity lifecycle hooks without querying `AuthUser` during every save.

The immutable `ChillUserPreferences` snapshot contains:

- `DisplayCultureName`
- `DisplayTimeZone`
- `DisplayDateFormat`
- `DisplayNumberFormat`

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
  "displayNumberFormat": "N2"
}
```

Use this endpoint after login and when restoring an authenticated UI session. It is the source of truth for the language/culture, time zone, date format, and number format used by the API client and UI; do not derive those values from the browser, operating system, token claims, or an auth-management endpoint.

The C# client exposes `GetCurrentUserPreferences()`. The Python client exposes `get_current_user_preferences()`, while the TypeScript client and Angular wrapper expose `getCurrentUserPreferences()`. The Vue and React packages additionally provide `useCurrentUserPreferences()`.

In the Angular UI Core package, inject `ChillService` and consume its `userPreferences` signal or the individual `displayCultureName`, `displayTimeZone`, `displayDateFormat`, and `displayNumberFormat` projections. Use them for UI text culture, date and number format/parse operations, and UTC-to-local time conversion. The values can be empty for an unauthenticated operation or when no `AuthUser` preference snapshot exists, so define a fallback at the usage point.

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
