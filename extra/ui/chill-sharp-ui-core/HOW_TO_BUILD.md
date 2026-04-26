# How To Build `@chill-sharp/ui-core`

From [`extra/ui/chill-sharp-ui-core`](/c:/source/personal/chill-sharp/chill-sharp/extra/ui/chill-sharp-ui-core):

```bash
npm install
npm run build
```

## Build Output

This package is built with `ng-packagr` and writes its output to:

```text
extra/ui/chill-sharp-ui-core/dist/
```

The built package in `dist/` is the artifact used for publishing and local consumption.

## Shared Folder Publish

To publish the package together with the latest required shared client dependencies from the `ui` workspace:

```powershell
cd ..\
.\publish-to-shared-folder.ps1
```

That flow publishes the latest `chill-sharp-ts-client` and `chill-sharp-ng-client` archives first, then builds and packs `ui-core`.

If the required shared client archives already exist in `C:\source\npm-shared`, you can also publish `ui-core` directly from this folder:

```powershell
.\publish-to-shared-folder.ps1
```

## Typical Workflow

1. run `npm install` if this machine has not built `ui-core` before
2. update source under `src/`
3. update `src/public-api.ts` if the public API changed
4. bump the `version` in [`package.json`](/c:/source/personal/chill-sharp/chill-sharp/extra/ui/chill-sharp-ui-core/package.json) when consumers need a new package revision
5. run `npm run build`
6. verify `dist/`
7. publish the package
8. update/install the new version in client apps

## Notes

- Rebuilding and versioning are different: build whenever output must include your source changes, and bump the version whenever external consumers must install a new package revision.
- If a client app consumes `@chill-sharp/ui-core` from npm, source edits in this repository do not update that app until a new package is published and installed.
- The template application dependency is currently tracked in [`extra/ui/chill-sharp-ui-template/package.json`](/c:/source/personal/chill-sharp/chill-sharp/extra/ui/chill-sharp-ui-template/package.json).

## Related Docs

- [`HOWTO_NPM_SHARED_SOURCE_FOLDER.md`](/c:/source/personal/chill-sharp/chill-sharp/extra/ui/chill-sharp-ui-core/HOWTO_NPM_SHARED_SOURCE_FOLDER.md)
- [`HOWTO_CREATE_PRIVATE_REGISTRY.md`](/c:/source/personal/chill-sharp/chill-sharp/extra/ui/chill-sharp-ui-core/HOWTO_CREATE_PRIVATE_REGISTRY.md)
