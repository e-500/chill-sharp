---
name: chillsharp-current-user-preferences
description: Configure and use ChillSharp.Auth's cached current-user culture, time zone, date-format, and number-format preferences in IChillContext and entity lifecycle hooks.
---

# ChillSharp Current User Preferences

Use this skill when a ChillSharp feature needs the authenticated user's display culture, time zone, date format, or number format during server-side work. Do not use it for browser-only UI preferences unrelated to `AuthUser`.

## Model

`ChillUserPreferences` is an immutable snapshot with `DisplayCultureName`, `DisplayTimeZone`, `DisplayDateFormat`, and `DisplayNumberFormat`. Entity hooks read it synchronously through:

```csharp
var preferences = context.GetCurrentUserPreferences();
```

Never load `AuthUser` from an `OnCreate`, `OnUpdate`, or other lifecycle hook merely to obtain these values.

## Auth integration

`AddChillAuthApi` registers `IChillAuthUserPreferencesAccessor` and the singleton `IChillAuthUserPreferencesCache`. Login warms the snapshot; `ChillAuthService` refreshes it after an `AuthUser` save and removes it for deleted or renamed external identities.

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
