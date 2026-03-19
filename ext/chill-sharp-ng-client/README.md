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
- `find()`
- `create()`
- `update()`
- `delete()`
- `chunk()`
- `test()`
- `getSchema()`
- `getSchemaList()`
- `setSchema()`
- `getText()`
- `getTexts()`
- `setText()`
- auth helpers like `loginAuthAccount()` and `refreshAuthAccount()`

## Examples

### Query

```ts
readonly posts$ = this.chill.query({
  ChillType: "Query.PostQuery",
  Properties: {
    Title: "Hello"
  },
  ResultProperties: [
    { Name: "Guid" },
    { Name: "Title" },
    { Name: "Author" }
  ]
});
```

### Create

```ts
this.chill.create({
  ChillType: "Model.Post",
  Guid: crypto.randomUUID(),
  Properties: {
    Title: "New title",
    Author: "Grace Hopper"
  }
}).subscribe();
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
- use `loginAuthAccount()`, `refreshAuthAccount()`, and password-reset methods from `ChillSharpNgClient`

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

