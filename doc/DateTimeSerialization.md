# ChillSharp Date And Time Serialization

Versione italiana: [Italiano](it/DateTimeSerialization.md)


This document explains how ChillSharp serializes and parses `DateTimeOffset`, `DateTime`, `DateOnly`, and `TimeOnly` values in DTO payloads.

It also compares ChillSharp behavior with the default ASP.NET Core `System.Text.Json` behavior so you can quickly see what is standard .NET behavior and what is ChillSharp-specific behavior.

## Why This Matters

ChillSharp moves data through DTO property bags rather than strongly typed controller parameters. That means date and time values are converted explicitly inside the DTO mapper.

For most applications the important questions are:

- what string format leaves the server
- what string format the server accepts on input
- whether offsets and time zones are preserved, normalized, or ignored

ChillSharp now follows standard .NET behavior for `DateOnly` and `TimeOnly`, while still accepting full ISO 8601 date-time strings when reading them back into those CLR types.

## Quick Comparison Table

| CLR type | Default ASP.NET Core / `System.Text.Json` | ChillSharp output |
| --- | --- | --- |
| `DateTimeOffset` | ISO 8601 date-time with offset | ISO 8601 date-time with offset |
| `DateTime` | ISO 8601 date-time, based on `DateTime.Kind` | ISO 8601 date-time converted to ChillSharp system time zone |
| `DateOnly` | `yyyy-MM-dd` | `yyyy-MM-dd` |
| `TimeOnly` | `HH:mm:ss[.fffffff]` | `HH:mm:ss.fffffff` |

## ChillSharp System Time Zone

ChillSharp uses a configurable system time zone only for `DateTime` and some `DateTimeOffset` normalization cases.

Environment variable:

```text
CHILLSHARP_SYSTEM_TIMEZONE
```

Default:

```text
Europe/Rome
```

Expected value:

- an IANA time-zone id such as `Europe/Rome`
- another example is `America/New_York`

This setting does **not** change the output format of `DateOnly` or `TimeOnly`.

## Outgoing Serialization

Outgoing serialization happens when ChillSharp reads entity/query CLR values and writes them into DTO `Properties`.

### `DateTimeOffset`

ChillSharp writes `DateTimeOffset` exactly as an ISO 8601 date-time with offset.

Example CLR value:

```csharp
new DateTimeOffset(2026, 4, 11, 14, 30, 0, TimeSpan.FromHours(2))
```

Serialized by ChillSharp:

```json
"2026-04-11T14:30:00.0000000+02:00"
```

This is effectively aligned with normal ASP.NET Core JSON serialization.

### `DateTime`

ChillSharp writes `DateTime` as an ISO 8601 date-time in the configured ChillSharp system time zone.

If the source value is UTC, ChillSharp converts it into the configured system time zone before writing.

Example with `CHILLSHARP_SYSTEM_TIMEZONE=Europe/Rome`:

```csharp
new DateTime(2026, 4, 11, 12, 30, 0, DateTimeKind.Utc)
```

Serialized by ChillSharp:

```json
"2026-04-11T14:30:00.0000000+02:00"
```

Another example with an unspecified `DateTime`:

```csharp
new DateTime(2026, 4, 11, 14, 30, 0, DateTimeKind.Unspecified)
```

Serialized by ChillSharp:

```json
"2026-04-11T14:30:00.0000000+02:00"
```

The difference from plain ASP.NET Core is that ChillSharp applies a configured system time zone consistently when writing `DateTime`.

### `DateOnly`

ChillSharp now keeps standard .NET behavior for `DateOnly`.

Example CLR value:

```csharp
new DateOnly(2026, 4, 11)
```

Serialized by ChillSharp:

```json
"2026-04-11"
```

This is intentionally simple. There is no offset, no time component, and no time-zone conversion on output.

### `TimeOnly`

ChillSharp now keeps standard .NET behavior for `TimeOnly`.

Example CLR value:

```csharp
new TimeOnly(14, 30, 15, 123)
```

Serialized by ChillSharp:

```json
"14:30:15.1230000"
```

There is no date and no time-zone conversion on output.

## Incoming Parsing

Incoming parsing happens when ChillSharp reads DTO `Properties` and applies them onto entity/query CLR objects.

This is the more permissive side of the mapper.

### `DateTimeOffset` input rules

If the incoming JSON contains:

- `Z`
- an explicit UTC offset

and the CLR target is `DateTimeOffset`, ChillSharp behaves like this:

- if the value is UTC (`Z` or `+00:00`), it converts the value into the configured ChillSharp system time zone
- if the value has another explicit offset, it keeps that offset as-is

Example input:

```json
"2026-04-11T12:30:00Z"
```

Stored in a `DateTimeOffset` property with `Europe/Rome` system time zone:

```csharp
2026-04-11 14:30:00 +02:00
```

Example input:

```json
"2026-04-11T12:30:00+01:00"
```

Stored in a `DateTimeOffset` property:

```csharp
2026-04-11 12:30:00 +01:00
```

### `DateTime` input rules

If the CLR target is `DateTime`:

- UTC input is converted into the configured ChillSharp system time zone
- input with an explicit offset is also converted into the configured ChillSharp system time zone
- input without offset is parsed as a normal date-time value

Example input:

```json
"2026-04-11T12:30:00Z"
```

Stored in a `DateTime` property with `Europe/Rome` system time zone:

```csharp
2026-04-11 14:30:00
```

Example input:

```json
"2026-04-11T12:30:00+01:00"
```

Stored in a `DateTime` property with `Europe/Rome` system time zone:

```csharp
2026-04-11 13:30:00
```

### `DateOnly` input rules

If the CLR target is `DateOnly`, ChillSharp extracts only the year, month, and day.

That means it accepts both:

- a plain date string
- a full ISO 8601 date-time string

and ignores the time, offset, and timezone information.

Example input:

```json
"2026-04-11"
```

Stored as:

```csharp
new DateOnly(2026, 4, 11)
```

Example input:

```json
"2026-04-11T23:59:58.321-05:00"
```

Stored as:

```csharp
new DateOnly(2026, 4, 11)
```

This rule is deliberate. `DateOnly` represents only a calendar date, so ChillSharp discards time and zone details when assigning it.

### `TimeOnly` input rules

If the CLR target is `TimeOnly`, ChillSharp extracts only the time part.

That means it accepts both:

- a plain time string
- a full ISO 8601 date-time string

and ignores the date, offset, and timezone information.

Example input:

```json
"14:30:15.1230000"
```

Stored as:

```csharp
new TimeOnly(14, 30, 15, 123)
```

Example input:

```json
"2026-04-11T23:59:58.321-05:00"
```

Stored as:

```csharp
new TimeOnly(23, 59, 58, 321)
```

This is useful when clients send a full timestamp but the target field conceptually represents only a local clock time.

## Side-By-Side Examples

Assume:

```text
CHILLSHARP_SYSTEM_TIMEZONE=Europe/Rome
```

### Example 1: `DateTimeOffset`

CLR value:

```csharp
new DateTimeOffset(2026, 4, 11, 14, 30, 0, TimeSpan.FromHours(2))
```

Default ASP.NET Core output:

```json
"2026-04-11T14:30:00+02:00"
```

ChillSharp output:

```json
"2026-04-11T14:30:00.0000000+02:00"
```

### Example 2: `DateTime` in UTC

CLR value:

```csharp
new DateTime(2026, 4, 11, 12, 30, 0, DateTimeKind.Utc)
```

Default ASP.NET Core output:

```json
"2026-04-11T12:30:00Z"
```

ChillSharp output:

```json
"2026-04-11T14:30:00.0000000+02:00"
```

### Example 3: `DateOnly`

CLR value:

```csharp
new DateOnly(2026, 4, 11)
```

Default ASP.NET Core output:

```json
"2026-04-11"
```

ChillSharp output:

```json
"2026-04-11"
```

### Example 4: `TimeOnly`

CLR value:

```csharp
new TimeOnly(14, 30, 15, 123)
```

Default ASP.NET Core output:

```json
"14:30:15.1230000"
```

ChillSharp output:

```json
"14:30:15.1230000"
```

### Example 5: full timestamp sent into `DateOnly`

Incoming JSON:

```json
"2026-04-11T23:59:58.321-05:00"
```

Stored by ChillSharp in a `DateOnly` property:

```csharp
new DateOnly(2026, 4, 11)
```

### Example 6: full timestamp sent into `TimeOnly`

Incoming JSON:

```json
"2026-04-11T23:59:58.321-05:00"
```

Stored by ChillSharp in a `TimeOnly` property:

```csharp
new TimeOnly(23, 59, 58, 321)
```

## Practical Guidance

- Use `DateTimeOffset` when the offset itself matters and should survive round-trips.
- Use `DateTime` when your application treats a value as local wall-clock time in the configured ChillSharp system time zone.
- Use `DateOnly` for birthdays, accounting dates, business dates, deadlines by calendar day, and similar concepts.
- Use `TimeOnly` for opening hours, appointment clock times, and other values that are intentionally not full timestamps.

## Related Configuration

For the environment variable reference, see:

- [Configuration/README.md](./Configuration/README.md)

For Docker and runtime environment examples, see:

- [HowTo/05-docker-env-variables.md](./HowTo/05-docker-env-variables.md)
