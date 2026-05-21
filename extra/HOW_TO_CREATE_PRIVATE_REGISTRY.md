# How To Create A Private Registry For `@chill-sharp`

This guide explains how to create a private npm registry on your local machine under `C:\source\` so all client applications can consume packages such as `@chill-sharp/ui-core`.

## Goal

The goal is to:

1. host a private npm registry on your machine
2. publish `@chill-sharp/*` packages to it
3. configure all client apps to install `@chill-sharp/*` from that registry

## Recommended Option

Use `Verdaccio`.

`Verdaccio` is a lightweight private npm registry that works well for local development and internal package distribution.

## Suggested Folder

Create the registry in:

```text
C:\source\private-npm-registry
```

This folder will contain:

- the registry config
- package storage
- user authentication data
- the local npm server installation

## 1. Create The Registry Folder

Open PowerShell and run:

```powershell
mkdir C:\source\private-npm-registry
cd C:\source\private-npm-registry
npm init -y
npm install verdaccio
```

## 2. Create The Registry Config

Create a file named:

```text
C:\source\private-npm-registry\config.yaml
```

Use this content:

```yaml
storage: C:/source/private-npm-registry/storage
plugins: C:/source/private-npm-registry/plugins

auth:
  htpasswd:
    file: C:/source/private-npm-registry/htpasswd

uplinks:
  npmjs:
    url: https://registry.npmjs.org/

packages:
  '@chill-sharp/*':
    access: $all
    publish: $authenticated
    unpublish: $authenticated

  '**':
    access: $all
    proxy: npmjs

server:
  keepAliveTimeout: 60

listen:
  - 0.0.0.0:4873
```

## 3. Start The Registry

Run:

```powershell
npx verdaccio -c C:\source\private-npm-registry\config.yaml
```

The registry will be available at:

```text
http://localhost:4873
```

Important:

- this is a running server process
- if the process is stopped, package install and publish commands will fail

## 4. Configure npm For All Client Apps

To make every client app on your machine use the local registry for `@chill-sharp/*`, add this to your user-level `.npmrc` file:

```text
C:\Users\<your-user>\.npmrc
```

Add:

```ini
@chill-sharp:registry=http://localhost:4873/
```

This is the most convenient option because it applies to all client applications automatically.

## 5. Create A Registry User

Before publishing packages, create a user account in the local registry:

```powershell
npm adduser --registry http://localhost:4873
```

Use the username, password, and email you want for local publishing.

## 6. Publish `@chill-sharp/ui-core`

From the `ui-core` library:

```powershell
cd C:\source\personal\chill-sharp\chill-sharp\extra\chill-sharp-ui-core
npm run build
cd dist
npm publish --registry http://localhost:4873
```

Notes:

- bump the package version before publishing a new revision
- the package must be rebuilt after source changes
- client apps will only see published versions, not raw source changes from this repository

## 7. Consume The Package In Client Apps

In any client application:

```powershell
npm install @chill-sharp/ui-core@1.0.127
```

Because of the scoped `.npmrc` setting, npm will resolve `@chill-sharp/*` from your private registry.

## 8. Update Client Apps After A New Publish

When `ui-core` changes:

1. update the source
2. bump the package version
3. rebuild the library
4. publish the new version to the private registry
5. run `npm install` in each client app

## Optional: Project-Level `.npmrc`

If you do not want to configure the registry for your whole user account, you can add a `.npmrc` file in each client repository instead:

```ini
@chill-sharp:registry=http://localhost:4873/
```

This is more explicit, but it means every client repo must be configured separately.

## Optional: Start Script

You may want a simple start command for the registry project.

In `C:\source\private-npm-registry\package.json`, add:

```json
{
  "scripts": {
    "start": "verdaccio -c config.yaml"
  }
}
```

Then start it with:

```powershell
npm start
```

## Troubleshooting

### Package Install Cannot Find `@chill-sharp/ui-core`

Check:

- the Verdaccio server is running
- the `.npmrc` file contains `@chill-sharp:registry=http://localhost:4873/`
- the package version has actually been published

### Publish Fails Because Version Already Exists

You must publish a new version. Update the `version` field in:

[`package.json`](/c:/source/personal/chill-sharp/chill-sharp/extra/chill-sharp-ui-core/package.json)

Then rebuild and publish again.

### Client App Still Uses An Older Package

Check:

- the client app installed the expected version
- `package.json` in the client app points to the correct version
- `npm install` was run after publishing

## Summary

The normal local workflow is:

1. run a private registry from `C:\source\private-npm-registry`
2. publish `@chill-sharp/ui-core` to `http://localhost:4873`
3. configure all client apps to install `@chill-sharp/*` from that registry

That gives you one local package source that every client application can share.
