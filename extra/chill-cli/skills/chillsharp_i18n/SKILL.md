---
name: chillsharp_i18n
description: Configure ChillSharp localized text storage, culture-aware schema labels, and i18n client lookups.
---

# ChillSharp Internationalization

Use this skill when a host needs persisted localized text or culture-aware schema metadata.

## Backend setup

The host `DbContext` must implement `IChillI18nDbContext` and call `modelBuilder.AddChillI18nModel()` from `OnModelCreating`. Register the API with `builder.Services.AddChillI18nApi<TContext>()`; the standard endpoints are `GET /api/chill-i18n/text/{labelGuid}/{cultureName}` and `PUT /api/chill-i18n/text`.

Keep primary and secondary culture names on `IChillContext`. `PrimaryLanguageLabel` and `SecondaryLanguageLabel` are schema fallbacks; they are not a replacement for persisted localized text.

## Client behavior

Use the generated or `ChillSharp.Client` i18n contract to request the desired culture explicitly. Treat a missing translation as a normal fallback case and keep the fallback order consistent with the host context.

## Checks

- Verify the context interface, EF model extension, service registration, and migration are all present.
- Do not put secrets or tenant-specific state in static localization caches.
- Test both primary and secondary cultures, plus an unsupported culture fallback.
