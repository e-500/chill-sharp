# ChillSharp UI Architecture

The UI packages now live beside the other extra libraries:

- [`chill-sharp-ui-core`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core)
- [`chill-sharp-ui-template`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template)

This folder keeps the UI architecture notes and a compatibility publish wrapper.

To publish the shared UI package together with its required client dependencies:

```powershell
.\publish-to-shared-folder.ps1
```

That script publishes the latest shared `chill-sharp-ts-client` and `chill-sharp-ng-client` archives first, then runs the `ui-core` publish flow.
