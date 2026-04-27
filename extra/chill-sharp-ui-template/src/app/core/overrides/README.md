# Client Overrides

Use this folder for client-specific provider replacements and future override registrations exposed by `@chill-sharp/ui-core`.

Typical examples:

- branded header providers
- custom page title strategy
- specialized service implementations
- feature-flag-driven replacements

`register-client-overrides.ts` returns an empty provider array by default so each client repo can opt into overrides intentionally.
