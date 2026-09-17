# ChillSharp DateTime Policy

Versione italiana: [Italiano](../it/DateTimePolicy/README.md)


This document defines the ChillSharp DTO policy for `DateTime` and `DateTimeOffset` values.

The policy applies when ChillSharp reads or writes values through DTO property bags, such as `ChillDtoEntity.Properties` and `ChillDtoQuery.Properties`.

## System Time Zone

ChillSharp uses a configured system time zone when a DTO value does not carry an explicit offset.

Environment variable:

```text
CHILLSHARP_SYSTEM_TIMEZONE
```

Default:

```text
Europe/Rome
```

Use an IANA time-zone id, for example:

```text
Europe/Rome
America/New_York
UTC
```

This configured time zone is not the same thing as `DateTimeKind.Local`. `DateTimeKind.Local` means the operating system local time zone. ChillSharp uses its own configured time zone explicitly.

## Incoming DTO Values

Incoming values are values received from a client and applied to CLR properties.

### DateTimeOffset

`DateTimeOffset` preserves an explicit offset when the client sends one.

Examples:

```text
2026-04-11T14:30:00.0000000+02:00
2026-04-11T12:30:00.0000000Z
```

Policy:

- if the incoming value has `Z`, preserve it as a UTC `DateTimeOffset`
- if the incoming value has an explicit offset, preserve that offset
- if the incoming value has no offset, interpret it as local time in `CHILLSHARP_SYSTEM_TIMEZONE`
- store the resulting value as a `DateTimeOffset` with the resolved offset

Example with `CHILLSHARP_SYSTEM_TIMEZONE=Europe/Rome`:

```text
Incoming: 2026-04-11T14:30:00
Stored:   2026-04-11T14:30:00+02:00
```

### DateTime

`DateTime` represents an instant and is normalized to UTC when read from DTO input.

Policy:

- if the incoming value has `Z`, parse it as UTC
- if the incoming value has an explicit offset, parse it as that instant
- if the incoming value has no offset, interpret it as local time in `CHILLSHARP_SYSTEM_TIMEZONE`
- store the resulting value as a UTC `DateTime`
- set `DateTime.Kind` to `DateTimeKind.Utc`

Example with `CHILLSHARP_SYSTEM_TIMEZONE=Europe/Rome`:

```text
Incoming: 2026-04-11T14:30:00
Stored:   2026-04-11T12:30:00Z
Kind:     Utc
```

Example with an explicit offset:

```text
Incoming: 2026-04-11T14:30:00+02:00
Stored:   2026-04-11T12:30:00Z
Kind:     Utc
```

## Outgoing DTO Values

Outgoing values are CLR values serialized into DTO property bags before returning data to a client.

### DateTimeOffset

ChillSharp serializes `DateTimeOffset` as an ISO 8601 string with its offset.

```text
2026-04-11T14:30:00.0000000+02:00
```

### DateTime

ChillSharp serializes `DateTime` as an ISO 8601 string with an explicit offset.

Policy:

- if the source value is UTC, convert it to `CHILLSHARP_SYSTEM_TIMEZONE` for DTO output
- if the source value is unspecified, interpret it as local time in `CHILLSHARP_SYSTEM_TIMEZONE`
- emit an ISO 8601 string with the resolved offset

Example with `CHILLSHARP_SYSTEM_TIMEZONE=Europe/Rome`:

```text
CLR:      2026-04-11T12:30:00Z
DTO:      2026-04-11T14:30:00.0000000+02:00
```

Depending on the JSON serializer, the `+` character may appear as `\u002B` on the wire:

```json
"2026-04-11T14:30:00.0000000\u002B02:00"
```

That is valid JSON and clients read it back as `+02:00`.

## Database Guidance

This policy is designed to work cleanly with providers such as PostgreSQL/Npgsql.

Recommended mapping:

- use `DateTime` for instant values that should be persisted as UTC
- use `DateTimeOffset` when preserving the incoming offset matters
- use `DateOnly` and `TimeOnly` for calendar dates or times of day that are not instants

For PostgreSQL:

- `DateTime` values produced by DTO parsing are UTC and are suitable for `timestamp with time zone`
- local wall-clock values should not be modeled as `DateTime` unless converting them to UTC is intended

## Server-Managed Audit Fields

ChillSharp server-managed audit fields are ignored when applying incoming entity DTO values:

- `Checksum`
- `LastUpdateUser`
- `LastUpdate`
- `LastUpdateUtcOffset`

Clients may receive these values from DTO output, but sending them back does not overwrite the server-managed entity state.
