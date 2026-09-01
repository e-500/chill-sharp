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

When the user logs in, ChillSharp loads the matching `AuthUser` once and warms a snapshot keyed by its `ExternalId`. The accessor only reads that in-memory snapshot during requests; it does not query the user table.

`ChillAuthService` refreshes the cached snapshot after creating or updating an `AuthUser`. It invalidates a deleted user's snapshot and both the previous and new keys when `ExternalId` changes.

The cache stores scalar preference values, never a tracked EF Core `AuthUser` instance.

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
