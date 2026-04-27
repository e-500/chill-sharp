# Extra Packages

This folder contains the reusable client packages and UI packages that sit alongside the main .NET solution.

## Contents

- [`chill-sharp-ts-client`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ts-client): base TypeScript client for the generic ChillSharp HTTP API
- [`chill-sharp-ng-client`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ng-client): Angular wrapper around `chill-sharp-ts-client`
- [`chill-sharp-react-client`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-react-client): React helpers built on `chill-sharp-ts-client`
- [`chill-sharp-vue-client`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-vue-client): Vue helpers built on `chill-sharp-ts-client`
- [`chill-sharp-py-client`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-py-client): Python client for the generic ChillSharp HTTP API
- [`chill-sharp-ui-core`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core): shared Angular UI package
- [`chill-sharp-ui-template`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template): starter client shell
- [`chill-sharp-ui-architecture`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-architecture): UI architecture notes and compatibility publish wrapper

## Shared Folder Publishing

Publish the JavaScript packages to the shared npm folder with:

```powershell
.\publish-to-shared-folder.ps1
```

Default shared folder:

```text
C:\source\npm-shared
```

## Quick Usage

### `chill-sharp-ts-client`

Use this when you want the plain TypeScript client without framework-specific helpers.

```bash
cd extra/chill-sharp-ts-client
npm install
npm run build
```

Shared-folder publish:

```powershell
.\publish-to-shared-folder.ps1
```

More information: [`extra/chill-sharp-ts-client/README.md`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ts-client/README.md)

### `chill-sharp-ng-client`

Use this in Angular applications that need DI-friendly access to the ChillSharp client.

```bash
cd extra/chill-sharp-ng-client
npm install
npm run build
```

Shared-folder publish:

```powershell
.\publish-to-shared-folder.ps1
```

More information: [`extra/chill-sharp-ng-client/README.md`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ng-client/README.md)

### `chill-sharp-react-client`

Use this in React applications that want provider and hook-based helpers on top of the generic client.

```bash
cd extra/chill-sharp-react-client
npm install
npm run build
```

Shared-folder publish:

```powershell
.\publish-to-shared-folder.ps1
```

More information: [`extra/chill-sharp-react-client/README.md`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-react-client/README.md)

### `chill-sharp-vue-client`

Use this in Vue applications that want plugin and composable helpers on top of the generic client.

```bash
cd extra/chill-sharp-vue-client
npm install
npm run build
```

Shared-folder publish:

```powershell
.\publish-to-shared-folder.ps1
```

More information: [`extra/chill-sharp-vue-client/README.md`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-vue-client/README.md)

### `chill-sharp-py-client`

Use this when you want the generic ChillSharp client from Python.

```bash
cd extra/chill-sharp-py-client
pip install -e .
python -m compileall chillsharp_py_client
```

This package is not part of the npm shared-folder publish flow.

More information: [`extra/chill-sharp-py-client/README.md`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-py-client/README.md)

### UI Packages

The UI packages live beside the other extra libraries:

- [`chill-sharp-ui-core`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core): shared Angular UI package
- [`chill-sharp-ui-template`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template): starter client shell

To publish the latest required shared client packages and then publish `ui-core`:

```powershell
cd extra
.\publish-to-shared-folder.ps1
```

For local builds:

```bash
cd extra/chill-sharp-ui-core
npm install
npm run build
```

```bash
cd extra/chill-sharp-ui-template
npm install
npm run build
```

More information:

- [`extra/chill-sharp-ui-core/README.md`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/README.md)
- [`extra/chill-sharp-ui-template/README.md`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-template/README.md)
