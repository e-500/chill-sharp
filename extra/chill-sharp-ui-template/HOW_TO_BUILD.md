# How To Build `chill-sharp-ui-template`

From [`extra/chill-sharp-ui-template`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template):

```bash
npm install
npm run build
```

The template carries the required ChillSharp package archives in `packages/`, so the build does not depend on `C:\source\npm-shared` being present on the target machine.

To refresh those local archives from a shared npm folder, run:

```powershell
.\upgrade.ps1
```

This package is a private application shell. It is not published to the shared npm folder, and its local [`publish-to-shared-folder.ps1`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/publish-to-shared-folder.ps1) script is intentionally a no-op.
