# UI Workspace

The `ui` workspace contains:

- [`chill-sharp-ui-core`](/c:/source/personal/chill-sharp/chill-sharp/extra/ui/chill-sharp-ui-core)
- [`chill-sharp-ui-template`](/c:/source/personal/chill-sharp/chill-sharp/extra/ui/chill-sharp-ui-template)

To publish the shared UI package together with its required client dependencies:

```powershell
.\publish-to-shared-folder.ps1
```

That script publishes the latest shared `chill-sharp-ts-client` and `chill-sharp-ng-client` archives first, then runs the `ui-core` publish flow.
