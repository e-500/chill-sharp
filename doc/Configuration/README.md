# ChillSharp Configuration Reference

This document lists the environment variables currently used by the example ChillSharp host in `ChillSharp.Examples/BloggingApiService`.

Use it as a quick reference when configuring Docker, `docker compose`, or another deployment target.

## Hosting

| Option | ENV variable | Description | Default |
| --- | --- | --- | --- |
| ASP.NET Core listen URLs | `ASPNETCORE_URLS` | URLs bound by the ASP.NET Core host. | `http://+:8080` |
| ASP.NET Core environment | `ASPNETCORE_ENVIRONMENT` | Standard ASP.NET Core environment name. | `Development` in the example `.env` |

## Core API

| Option | ENV variable | Description | Default |
| --- | --- | --- | --- |
| SQLite database path | `CHILLSHARP_DB_PATH` | File path used by the example `BloggingContext` SQLite database. | `/data/blogging.db` |
| Primary culture | `CHILLSHARP_PRIMARY_CULTURE` | Value returned by `IChillContext.GetPrimaryCultureName()`. | `en-GB` |
| Secondary culture | `CHILLSHARP_SECONDARY_CULTURE` | Value returned by `IChillContext.GetSecondaryCultureName()`. | `it-IT` |
| Protected core API | `CHILLSHARP_API_PROTECTED` | Requires authentication for the core ChillSharp API when `true`. | `true` when auth is enabled |
| DTO system time zone | `CHILL_SHARP_SYSTEM_TIMEZONE` | IANA time-zone id used by ChillSharp DTO `DateTime` and `DateTimeOffset` parsing and serialization helpers. | `Europe/Rome` |

## Module Toggles

| Option | ENV variable | Description | Default |
| --- | --- | --- | --- |
| Enable schema module | `CHILLSHARP_ENABLE_SCHEMA` | Registers `ChillSharp.Schema` services. | `true` |
| Enable auth module | `CHILLSHARP_ENABLE_AUTH` | Registers `ChillSharp.Auth` account and auth-management services. | `true` |
| Enable i18n module | `CHILLSHARP_ENABLE_I18N` | Registers `ChillSharp.I18n` services. | `true` |

## Auth Tokens And Password Flows

| Option | ENV variable | Description | Default |
| --- | --- | --- | --- |
| Access-token lifetime | `CHILLSHARP_AUTH_ACCESS_TOKEN_MINUTES` | Minutes before a bearer access token expires. | `20` |
| Refresh-token lifetime | `CHILLSHARP_AUTH_REFRESH_TOKEN_DAYS` | Days before a refresh token expires. | `14` |
| Return reset token in API response | `CHILLSHARP_AUTH_RETURN_PASSWORD_RESET_TOKENS` | Includes `userId` and `resetToken` in `/api/chill-auth/account/request-password-reset` response when `true`. | `false` in the example host |
| Send password-reset emails | `CHILLSHARP_AUTH_SEND_PASSWORD_RESET_EMAILS` | Sends a password-reset email through SMTP when `true`. | `false` in code, `true` in the example `.env` |
| Password-reset email subject | `CHILLSHARP_AUTH_PASSWORD_RESET_SUBJECT` | Subject used for password-reset emails. | `Reset your password` |
| Password-reset URL | `CHILLSHARP_AUTH_PASSWORD_RESET_URL` | Optional frontend URL used to build a clickable password-reset link with `userId` and `resetToken`. | unset |

## SMTP Password-Reset Delivery

| Option | ENV variable | Description | Default |
| --- | --- | --- | --- |
| SMTP host | `CHILLSHARP_SMTP_HOST` | SMTP server host name used for no-reply password-reset delivery. | unset |
| SMTP port | `CHILLSHARP_SMTP_PORT` | SMTP server port. | `587` |
| SMTP SSL/TLS | `CHILLSHARP_SMTP_ENABLE_SSL` | Enables SSL/TLS on the SMTP client when `true`. | `true` |
| SMTP user name | `CHILLSHARP_SMTP_USERNAME` | SMTP authentication user name. | unset |
| SMTP password | `CHILLSHARP_SMTP_PASSWORD` | SMTP authentication password. | unset |
| No-reply sender email | `CHILLSHARP_SMTP_FROM_EMAIL` | Sender email address used for password-reset emails. | unset |
| No-reply sender display name | `CHILLSHARP_SMTP_FROM_DISPLAY_NAME` | Sender display name used for password-reset emails. | unset |

When `CHILLSHARP_AUTH_SEND_PASSWORD_RESET_EMAILS=true`, the SMTP host and sender email must be configured or the reset flow will fail.

## Root User Bootstrap

These variables are read by `ChillAuthRootUserInitializer<TUser>` during startup when root-user initialization is enabled.

| Option | ENV variable | Description | Default |
| --- | --- | --- | --- |
| Initialize root user | `CHILLSHARP_AUTH_INITIALIZE_ROOT_USER` | Creates the root Identity user at startup when credentials are available. | `true` |
| Create linked ChillSharp auth user | `CHILLSHARP_AUTH_CREATE_ROOT_AUTH_USER` | Also creates the linked ChillSharp `AuthUser` with permission-management access. | `true` |
| Root user name | `CHILLSHARP_AUTH_ROOT_USERNAME` | Login name for the bootstrap administrator. | unset |
| Root password | `CHILLSHARP_AUTH_ROOT_PASSWORD` | Password for the bootstrap administrator. | unset |
| Root email | `CHILLSHARP_AUTH_ROOT_EMAIL` | Optional email for the bootstrap administrator. | unset |
| Root display name | `CHILLSHARP_AUTH_ROOT_DISPLAY_NAME` | Display name copied into the linked ChillSharp `AuthUser`. | `Root` in code |

## Notes

- Most variables listed here use the example host's `CHILLSHARP_*` prefix. `CHILL_SHARP_SYSTEM_TIMEZONE` is a core ChillSharp runtime variable used directly by DTO date/time mapping.
- `CHILL_SHARP_SYSTEM_TIMEZONE` expects an IANA time-zone id such as `Europe/Rome` or `America/New_York`.
- `CHILL_SHARP_SYSTEM_TIMEZONE` affects `DateTime` and some `DateTimeOffset` normalization paths. `DateOnly` and `TimeOnly` keep standard .NET string output.
- The `CHILLSHARP_*` variables listed here are the ones currently consumed by the example host and the built-in root-user initializer.
- If you build your own host application, you can keep these names or map configuration differently in your own startup code.
- For deployment examples, also see [doc/HowTo/05-docker-env-variables.md](../HowTo/05-docker-env-variables.md).
- For the full date/time serialization reference and examples, see [doc/DateTimeSerialization.md](../DateTimeSerialization.md).
