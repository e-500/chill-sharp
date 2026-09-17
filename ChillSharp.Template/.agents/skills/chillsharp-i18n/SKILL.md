---
name: chillsharp-i18n
description: Configure ChillSharp localized text storage, culture-aware schema labels, and i18n client lookups.
---

# ChillSharp Internationalization

The host context must implement `IChillI18nDbContext` and call `modelBuilder.AddChillI18nModel()` from `OnModelCreating`. Register `builder.Services.AddChillI18nApi<TContext>()`; the standard endpoints are `GET /api/chill-i18n/text/{labelGuid}/{cultureName}` and `PUT /api/chill-i18n/text`.

Keep primary and secondary culture names on `IChillContext`. `PrimaryLanguageLabel` and `SecondaryLanguageLabel` are schema fallbacks, not a replacement for persisted localized text. Test primary, secondary, and unsupported cultures after creating the EF migration.
