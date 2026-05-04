# How To Share `@chill-sharp/ui-core` Through A Shared Folder

This guide explains a simpler alternative to a private npm registry.

Instead of running a registry server, you can build `@chill-sharp/ui-core`, package it, place the package file in a shared folder, and let client applications install that package from the shared location.

## When This Approach Makes Sense

This approach is useful when:

- you want something simpler than a private registry
- you have a small number of client applications
- you do not need full npm registry behavior
- you are fine with manually updating client apps when a new package is published

## Important Limitation

A shared folder is not an npm registry.

That means this will not work:

```powershell
npm install @chill-sharp/ui-core@1.0.119
```

unless the package is coming from a real registry.

With a shared folder, the practical options are:

1. install from a packaged `.tgz` file
2. reference a local folder with `file:`

For most teams, the best shared-folder workflow is the `.tgz` approach.

## Recommended Shared-Folder Workflow

The recommended flow is:

1. build `ui-core`
2. create an npm package archive with `npm pack`
3. copy the generated `.tgz` file into a shared folder
4. install that `.tgz` file from each client app

## Suggested Shared Folder

Example:

```text
C:\source\shared-npm-packages
```

You can also use a network share if multiple machines must consume the package.

Examples:

```text
\\my-server\shared-npm-packages
```

or

```text
Z:\shared-npm-packages
```

## 1. Build The Library

Before the first local build on a machine, install the package dependencies from `extra\chill-sharp-ui-core`:

```powershell
npm install
```

From:

```text
extra\chill-sharp-ui-core
```

run:

```powershell
npm run build
```

This produces the compiled package output in:

```text
extra\chill-sharp-ui-core\dist
```

The helper script [`publish-to-shared-folder.ps1`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/publish-to-shared-folder.ps1) will run `npm install` automatically if `ng-packagr` has not been installed yet.

## 2. Bump The Version First

Before creating a distributable package, update the version in:

[`package.json`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/package.json)

Example:

```json
{
  "name": "@chill-sharp/ui-core",
  "version": "1.0.119"
}
```

You should use a new version each time you want client applications to consume a new package revision.

## 3. Create The Package Archive

After the build completes, create an npm package archive from the built `dist/` output.

From the `dist/` folder:

```powershell
cd C:\source\personal\chill-sharp\chill-sharp\extra\chill-sharp-ui-core
cd dist
npm pack
```

This creates a file similar to:

```text
chill-sharp-ui-core-1.0.119.tgz
```

Important:

- the `.tgz` file is a normal npm package artifact
- client apps can install it directly
- this is usually easier and safer than pointing client apps at raw source files

## 4. Copy The Package To The Shared Folder

Copy the generated `.tgz` file to your shared package folder.

Example destination:

```text
C:\source\shared-npm-packages
```

After copying, you might have:

```text
C:\source\shared-npm-packages\chill-sharp-ui-core-1.0.119.tgz
```

## 5. Install The Package In A Client App

From a client application, install the package directly from the shared folder:

```powershell
npm install C:\source\shared-npm-packages\chill-sharp-ui-core-1.0.119.tgz
```

If the package is on a network share:

```powershell
npm install \\my-server\shared-npm-packages\chill-sharp-ui-core-1.0.119.tgz
```

This works because npm understands package tarballs even when they come from a file path instead of a registry.

## 6. Update A Client App After A New `ui-core` Change

When `ui-core` changes:

1. update the source code
2. bump the package version
3. run `npm run build`
4. run `npm pack` from `dist`
5. copy the new `.tgz` file into the shared folder
6. install the new `.tgz` file in each client app

## Windows Helper Script

If you want one command for the whole workflow, use:

[`publish-to-shared-folder.ps1`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/publish-to-shared-folder.ps1)

Default usage:

```powershell
.\publish-to-shared-folder.ps1
```

This script:

1. runs `npm run build`
2. creates a `.tgz` from `dist`
3. writes the package into `C:\source\npm-shared`

Custom destination:

```powershell
.\publish-to-shared-folder.ps1 -SharedFolder C:\source\shared-npm-packages
```

## Alternative: Use `file:` Dependencies

You can also point a client app at a folder directly.

Example in client `package.json`:

```json
{
  "dependencies": {
    "@chill-sharp/ui-core": "file:../shared/ui-core"
  }
}
```

This can work for local development, but it has tradeoffs:

- it is less explicit than versioned package archives
- it is easier to accidentally consume unbuilt or inconsistent content
- it is not as clean for multi-app distribution
- it is not a substitute for versioned publishing

For repeatable distribution, `.tgz` packages are usually the better option.

## Shared Folder Vs Private Registry

Shared folder advantages:

- much simpler setup
- no server process to maintain
- works well for one machine or a small team

Shared folder disadvantages:

- no real registry semantics
- no `npm install @chill-sharp/ui-core@version`
- manual installation workflow for each client app
- version upgrades are more manual

Private registry advantages:

- normal npm install flow
- better versioned package distribution
- easier to scale to many client apps

Private registry disadvantages:

- more setup
- requires a running server

## Recommended Choice

If you want the simplest possible workflow, use:

- `npm run build`
- `npm pack` from `dist`
- shared folder storage for the `.tgz`
- `npm install <path-to-tgz>` in client apps

If you want a cleaner long-term multi-client workflow, use a private registry instead.

## Example End-To-End Workflow

### In `ui-core`

```powershell
cd C:\source\personal\chill-sharp\chill-sharp\extra\chill-sharp-ui-core
npm run build
cd dist
npm pack
Copy-Item .\chill-sharp-ui-core-1.0.119.tgz C:\source\shared-npm-packages\
```

### In a client app

```powershell
npm install C:\source\shared-npm-packages\chill-sharp-ui-core-1.0.119.tgz
```

## Summary

Yes, a shared folder is simpler than a private npm server.

But npm only handles it cleanly when you install:

- a `.tgz` package file
- or a `file:` folder dependency

It does not behave like a registry-backed package source for normal scoped version installs.
