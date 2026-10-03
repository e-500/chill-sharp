---
name: chillsharp-current-user-preferences
description: Configure, expose, and consume ChillSharp.Auth's cached current-user culture, time zone, date-format, number-format, and theme preferences in server logic and UI clients.
---

# ChillSharp Current User Preferences

Use this skill when a ChillSharp feature needs the authenticated user's display culture, time zone, date format, number format, or preferred theme. It covers server-side lifecycle hooks and UI/client consumption of the authoritative `AuthUser` preferences. Do not use it for browser-only preferences unrelated to `AuthUser`.

## Model

`ChillUserPreferences` is an immutable snapshot with `DisplayCultureName`, `DisplayTimeZone`, `DisplayDateFormat`, `DisplayNumberFormat`, and `PreferredTheme`. Entity hooks read it synchronously through:

```csharp
var preferences = context.GetCurrentUserPreferences();
```

Never load `AuthUser` from an `OnCreate`, `OnUpdate`, or other lifecycle hook merely to obtain these values.

## Auth integration

`AddChillAuthApi` registers `IChillAuthUserPreferencesAccessor` and the singleton `IChillAuthUserPreferencesCache`. Login and token refresh warm the snapshot; `ChillAuthService` refreshes it after an `AuthUser` save and removes it for deleted or renamed external identities.

For a custom host DbContext, inject the scoped accessor and implement the two `IChillContext` methods below. Keep the accessor optional only when the context must also be constructible outside DI, such as for migrations or tests.

```csharp
private readonly IChillAuthUserPreferencesAccessor? _userPreferencesAccessor;

public ChillUserPreferences GetCurrentUserPreferences() =>
    _userPreferencesAccessor?.Current ?? ChillUserPreferences.Empty;

public string GetDefaultUserCultureName()
{
    var cultureName = GetCurrentUserPreferences().DisplayCultureName;
    return string.IsNullOrWhiteSpace(cultureName) ? GetPrimaryCultureName() : cultureName;
}
```

The accessor is cache-only: when there is no authenticated principal or no warmed snapshot, it returns `ChillUserPreferences.Empty`. Do not add a fallback database query to lifecycle hooks.

## API and client use

The authenticated endpoint `GET /api/chill-auth/current-user-preferences` returns the same `ChillUserPreferences` snapshot as JSON:

```json
{
  "displayCultureName": "it-IT",
  "displayTimeZone": "Europe/Rome",
  "displayDateFormat": "dd/MM/yyyy",
  "displayNumberFormat": "N2",
  "preferredTheme": "cini"
}
```

Use this endpoint after authentication and when restoring a session; do not infer the active display culture or time zone from the browser, operating system, token claims, or auth-management user endpoints. The C#, Python, TypeScript, Angular, Vue, and React clients expose it as `GetCurrentUserPreferences`, `get_current_user_preferences`, or `getCurrentUserPreferences`; the Vue and React packages also provide `useCurrentUserPreferences`.

In Angular UI Core, inject `ChillService` and read its `userPreferences` signal (or its `displayCultureName`, `displayTimeZone`, `displayDateFormat`, `displayNumberFormat`, and `preferredTheme` projections). Use these values to select UI language, format and parse dates and numbers, convert UTC timestamps, and select the client theme. The backend treats `PreferredTheme` as an opaque string. UI Core defaults unauthenticated users to browser `prefers-color-scheme` light/dark and clients register extra selectable themes with `provideChillSharpUiCore({ additionalThemes: ['theme-name'] })`.

## Entity use

Use `DisplayTimeZone` as an IANA zone identifier only after validating it with the application's time-zone policy. Treat all fields as optional and provide an explicit fallback where one is required.

```csharp
public override void OnUpdate(IChillContext context)
{
    var timeZone = context.GetCurrentUserPreferences().DisplayTimeZone;
    // Apply application-specific behavior only when timeZone is available.
}
```

For full setup and cache behavior, read [CurrentUserPreferences.md](../../../doc/CurrentUserPreferences.md).
