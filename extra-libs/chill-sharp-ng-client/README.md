# chill-sharp-ng-client

Angular helpers for a generic ChillSharp service.

This package wraps [`chill-sharp-ts-client`](../chill-sharp-ts-client) and adds:

- `provideChillSharpClient()` for Angular DI setup
- `CHILL_SHARP_CLIENT` injection token for direct access to the raw client
- `ChillSharpNgClient` service with RxJS `Observable` methods

It stays generic on purpose. Payloads are plain objects so the same package can work against arbitrary ChillSharp models.

## Install

From the repository root:

```bash
cd ext/chill-sharp-ng-client
npm install
```

This package expects:

- Angular 17+
- `rxjs`
- `chill-sharp-ts-client`
- a runtime `fetch` implementation, which modern browsers and Node.js 18+ already provide

## Local Linking

The package builds automatically on `npm install`, `npm pack`, and `npm link`.
Link `chill-sharp-ts-client` first, then this package:

```bash
cd ext/chill-sharp-ts-client
npm install
npm link

cd ../chill-sharp-ng-client
npm install
npm link

cd path/to/your-angular-app
npm link chill-sharp-ts-client
npm link chill-sharp-ng-client
```

## Quick Start

### Standalone bootstrap

```ts
import { bootstrapApplication } from "@angular/platform-browser";
import { AppComponent } from "./app/app.component";
import { provideChillSharpClient } from "chill-sharp-ng-client";

bootstrapApplication(AppComponent, {
  providers: [
    ...provideChillSharpClient({
      baseUrl: "http://localhost:5000/api/chill",
      options: { cultureName: "it-IT" }
    })
  ]
});
```

### Service usage

```ts
import { Component, inject } from "@angular/core";
import { AsyncPipe, JsonPipe } from "@angular/common";
import { ChillSharpNgClient } from "chill-sharp-ng-client";

@Component({
  selector: "app-blog-schema",
  standalone: true,
  imports: [AsyncPipe, JsonPipe],
  template: `
    <pre>{{ schema$ | async | json }}</pre>
  `
})
export class BlogSchemaComponent {
  private readonly chill = inject(ChillSharpNgClient);
  readonly schema$ = this.chill.getSchema("Model.Blog", "default");
}
```

## DI Setup

### Let Angular create the client

```ts
providers: [
  ...provideChillSharpClient({
    baseUrl: "http://localhost:5000/api/chill",
    options: {
      cultureName: "it-IT",
      accessToken: "your-jwt-token"
    }
  })
]
```

### Provide a prebuilt client

```ts
import { ChillSharpClient, provideChillSharpClient } from "chill-sharp-ng-client";

const client = new ChillSharpClient("http://localhost:5000/api/chill", {
  cultureName: "it-IT"
});

providers: [
  ...provideChillSharpClient({
    baseUrl: "http://localhost:5000/api/chill",
    client
  })
]
```

## `ChillSharpNgClient`

`ChillSharpNgClient` mirrors the generic TypeScript client but returns `Observable`s:

- `query()`
- `lookup()`
- `find()`
- `create()`
- `update()`
- `delete()`
- `uploadAttachment()`
- `uploadAttachments()`
- `getAttachments()`
- `downloadAttachment()`
- `chunk()`
- `test()`
- `getSchema()`
- `getSchemaList()`
- `setSchema()`
- `getEntityOptions()`
- `setEntityOptions()`
- `getMenu()`
- `setMenu()`
- `deleteMenu()`
- `getText()`
- `getTexts()`
- `setText()`
- auth helpers like `loginAuthAccount()` and `refreshAuthAccount()`
- auth management helpers like `getAuthPermissions()`, `getAuthUserList()`, `setAuthUser()`, `getAuthRoleList()`, and `setAuthRole()`

Schema and entity option payloads re-exported by this package include the `handleAttachments` flag exposed by the schema APIs.
Query payloads also include `ordering`, and entity payloads include `position` with backend default `0`.

The auth user payloads include:

- `displayCultureName`
- `displayTimeZone`
- `displayDateFormat`
- `displayNumberFormat`

## Examples

### Query

Use `query()` when `ChillType` points to a concrete query type such as `Query.PostQuery`.

```ts
readonly posts$ = this.chill.query({
  chillType: "Query.PostQuery",
  properties: {
    title: "Hello"
  },
  ordering: {
    propertyName: "Position",
    direction: "ASC"
  },
  resultProperties: [
    { name: "Guid" },
    { name: "Title" },
    { name: "Author" }
  ]
});
```

If `ordering.propertyName` points to a Chill entity reference, the backend orders by that referenced entity `Label`.

### Lookup

Use `lookup()` when `ChillType` points to an entity type and you only need generic full-text search.

```ts
readonly postsLookup$ = this.chill.lookup({
  chillType: "Model.Post",
  properties: {
    fullTextSearch: "Ada Lovelace"
  },
  ordering: {
    propertyName: "Blog",
    direction: "ASC"
  },
  resultProperties: [
    { name: "Guid" },
    { name: "Title" },
    { name: "Author" }
  ]
});
```

### Create

```ts
this.chill.create({
  chillType: "Model.Post",
  guid: crypto.randomUUID(),
  position: 10,
  properties: {
    title: "New title",
    author: "Grace Hopper"
  }
}).subscribe();
```

### Attachments

```ts
readonly uploaded$ = this.chill.uploadAttachment(
  {
    ChillType: "Model.Post",
    Guid: this.postGuid
  },
  {
    fileName: "contract.txt",
    content: new Blob(["hello attachment"], { type: "text/plain" }),
    contentType: "text/plain"
  },
  {
    title: "Contract",
    description: "Signed draft",
    isPublic: false
  }
);

readonly attachments$ = this.chill.getAttachments({
  ChillType: "Model.Post",
  Guid: this.postGuid
});
```

### Test endpoint

```ts
readonly status$ = this.chill.test();
```

### Get localized schema

```ts
readonly schema$ = this.chill.getSchema("Model.Post", "default");
readonly englishSchema$ = this.chill.getSchema("Model.Post", "default", "en-GB");
```

### Get schema list

```ts
readonly schemaList$ = this.chill.getSchemaList();
readonly englishSchemaList$ = this.chill.getSchemaList("en-GB");
```

### Get text

```ts
readonly text$ = this.chill.getText({
  LabelGuid: "4e16f6c0-6b95-4d67-98bc-9f4d0d63eaf1",
  CultureName: "it-IT",
  PrimaryCultureName: "en-GB",
  PrimaryDefaultText: "Blog title",
  SecondaryCultureName: "it-IT",
  SecondaryDefaultText: "Titolo del blog"
});
```

### Get texts

```ts
readonly texts$ = this.chill.getTexts([
  {
    LabelGuid: "4e16f6c0-6b95-4d67-98bc-9f4d0d63eaf1",
    CultureName: "it-IT",
    PrimaryCultureName: "en-GB",
    PrimaryDefaultText: "Blog title",
    SecondaryCultureName: "it-IT",
    SecondaryDefaultText: "Titolo del blog"
  },
  {
    LabelGuid: "2f6ef6f7-b0a9-44f8-bfd2-a3b3ed5b9a81",
    CultureName: "it-IT",
    PrimaryCultureName: "en-GB",
    PrimaryDefaultText: "Blog url",
    SecondaryCultureName: "it-IT",
    SecondaryDefaultText: "Url del blog"
  }
]);
```

## Chunk batches

Use `chunk()` when several operations should be sent in one request.

```ts
readonly batch$ = this.chill.chunk([
  {
    Index: 0,
    Verb: "transaction"
  },
  {
    Index: 1,
    Verb: "create",
    Entity: {
      ChillType: "Model.Post",
      Guid: crypto.randomUUID(),
      Properties: {
        Title: "Batched post",
        Author: "Grace Hopper"
      }
    }
  },
  {
    Index: 2,
    Verb: "update",
    Entity: {
      ChillType: "Model.Post",
      Guid: existingGuid,
      Properties: {
        Title: "Updated in the same batch"
      }
    }
  },
  {
    Index: 3,
    Verb: "commit"
  }
]);
```

Use `transaction` and `commit` only when the enclosed write operations must be committed together.
## Raw client access

If you need the promise-based client directly, inject `CHILL_SHARP_CLIENT`.

```ts
import { Component, Inject } from "@angular/core";
import { CHILL_SHARP_CLIENT, ChillSharpClient } from "chill-sharp-ng-client";

@Component({
  selector: "app-raw-client",
  standalone: true,
  template: `Ready`
})
export class RawClientComponent {
  constructor(@Inject(CHILL_SHARP_CLIENT) private readonly client: ChillSharpClient) {}
}
```

## Authentication

Because the Angular package reuses the TypeScript client, it inherits the same auth behavior:

- pass `accessToken` when you already have a token
- pass `username` and `password` when the client should log in and refresh automatically
- pass `DisplayCultureName` during registration when the server should preset auth-user display preferences
- use `loginAuthAccount()`, `refreshAuthAccount()`, password-reset methods, auth-management methods, and schema-management methods from `ChillSharpNgClient`

## Error Handling

Transport errors originate as `ChillSharpClientError` inside the wrapped promise. Handle them in RxJS the usual way.

```ts
import { catchError, throwError } from "rxjs";
import { ChillSharpClientError } from "chill-sharp-ng-client";

this.schema$ = this.chill.getSchema("Model.Post", "default").pipe(
  catchError((error: unknown) => {
    if (error instanceof ChillSharpClientError) {
      console.error(error.statusCode, error.responseText);
    }

    return throwError(() => error);
  })
);
```

## When To Use The Angular Package

Use this package when you want Angular DI and Observable-based integration on top of the generic client.

Use the plain TypeScript package instead when:

- you are not using Angular
- you prefer promises end to end
- you already have your own Angular data-access wrapper

## Generic Payload Strategy

This package does not generate Angular services or model-specific DTO classes for your Chill entities.

That is intentional:

- ChillSharp models are application-specific
- generic object payloads are enough to talk to the standard ChillSharp API
- model-specific Angular APIs are better generated from OpenAPI for each host application

If you need typed model clients, generate them from your host OpenAPI document as described in [doc/ClientGeneration/README.md](../../doc/ClientGeneration/README.md).






## Menu endpoints

`getMenu()` loads root menu nodes or the direct children of one menu item. `setMenu()` creates or updates one menu item. `deleteMenu()` removes one menu item together with its child subtree.

Menu items include `positionNo`, which the backend persists and uses to order siblings.

For the complete tree model, delete behavior, and `MenuHierarchy` filtering behavior, see [../../doc/MenuModel.md](../../doc/MenuModel.md).
