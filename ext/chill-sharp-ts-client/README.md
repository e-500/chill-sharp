# chill-sharp-ts-client

TypeScript client for a generic ChillSharp service.

This package targets the standard ChillSharp HTTP surface:

- core Chill API at `/api/chill`
- auth API at `/api/chill-auth`
- i18n API at `/api/chill-i18n`

It is intentionally lightweight. Payloads are plain JavaScript objects so the client can work against arbitrary ChillSharp models without code generation.

## Install

From the repository root:

```bash
cd ext/chill-sharp-ts-client
npm install
npm run build
```

Or from another project:

```bash
npm install ../ext/chill-sharp-ts-client
```

The client uses the runtime `fetch` API available in modern browsers and Node.js 18+.

## Local Linking

This package now builds automatically on `npm install`, `npm pack`, and `npm link` through the `prepare` and `prepack` scripts.

Example local workflow:

```bash
cd ext/chill-sharp-ts-client
npm install
npm link

cd path/to/your-app
npm link chill-sharp-ts-client
```

## Quick Start

```ts
import { ChillSharpClient } from "chill-sharp-ts-client";

const client = new ChillSharpClient("http://localhost:5000/api/chill", {
  cultureName: "it-IT"
});

const created = await client.create({
  ChillType: "Model.Post",
  Guid: "00000000-0000-0000-0000-000000000001",
  Properties: {
    Title: "Hello",
    Author: "Ada Lovelace"
  }
});

const found = await client.find({
  ChillType: "Model.Post",
  Guid: created.Guid as string
});
```

## Construction Modes

### Anonymous or externally authenticated

```ts
const client = new ChillSharpClient("http://localhost:5000/api/chill", {
  cultureName: "it-IT"
});
```

### With an existing access token

```ts
const client = new ChillSharpClient("http://localhost:5000/api/chill", {
  accessToken: "your-jwt-token",
  cultureName: "it-IT"
});
```

### With username and password

```ts
const client = new ChillSharpClient("http://localhost:5000/api/chill", {
  username: "root",
  password: "Pass123$",
  cultureName: "it-IT"
});
```

If the service supports ChillSharp auth endpoints, the client can log in and refresh tokens automatically.

## Core ChillSharp Operations

### Query

```ts
const result = await client.query({
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

### Find

```ts
const entity = await client.find({
  ChillType: "Model.Post",
  Guid: "f2d5d5e3-0a1f-4d15-9396-2ab5f6c4ff11"
});
```

### Create

```ts
const entity = await client.create({
  ChillType: "Model.Post",
  Guid: "f2d5d5e3-0a1f-4d15-9396-2ab5f6c4ff11",
  Properties: {
    Title: "New title",
    Author: "Grace Hopper"
  }
});
```

### Update

```ts
const updated = await client.update({
  ChillType: "Model.Post",
  Guid: "f2d5d5e3-0a1f-4d15-9396-2ab5f6c4ff11",
  Properties: {
    Title: "Updated title"
  }
});
```

### Delete

```ts
await client.delete({
  ChillType: "Model.Post",
  Guid: "f2d5d5e3-0a1f-4d15-9396-2ab5f6c4ff11"
});
```

### Chunk

```ts
const operations = await client.chunk([
  {
    Verb: "CREATE",
    Entity: {
      ChillType: "Model.Post",
      Guid: "11111111-1111-1111-1111-111111111111",
      Properties: { Title: "First", Author: "A" }
    }
  },
  {
    Verb: "CREATE",
    Entity: {
      ChillType: "Model.Post",
      Guid: "22222222-2222-2222-2222-222222222222",
      Properties: { Title: "Second", Author: "B" }
    }
  }
]);
```

### Test

```ts
const status = await client.test();
// "ChillSharp is up and running!"
```

Use this to verify the Chill endpoint is reachable before sending API payloads.

## Schema Operations

### Get schema

```ts
const schema = await client.getSchema("Model.Post", "default");

// Override the constructor default for one call
const englishSchema = await client.getSchema("Model.Post", "default", "en-GB");
```

### Get schema list

```ts
const schemaList = await client.getSchemaList();
const englishSchemaList = await client.getSchemaList("en-GB");
```

### Set schema

```ts
await client.setSchema({
  ChillType: "Model.Post",
  ChillViewCode: "default",
  DisplayName: "Post",
  Properties: [
    {
      Name: "Title",
      DisplayName: "Post title"
    }
  ]
});
```

## I18n Operations

### Get text

```ts
const text = await client.getText({
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
const texts = await client.getTexts([
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

### Set text

```ts
const saved = await client.setText({
  LabelGuid: "4e16f6c0-6b95-4d67-98bc-9f4d0d63eaf1",
  CultureName: "it-IT",
  Value: "Titolo del blog"
});
```

## Auth Operations

The client assumes the auth base path is derived from `/api/chill` to `/api/chill-auth`, matching the .NET client.

### Register account

```ts
const token = await client.registerAuthAccount({
  UserName: "root",
  Email: "root@example.com",
  Password: "Pass123$",
  DisplayName: "Root",
  CreateChillAuthUser: true
});
```

### Login

```ts
const token = await client.loginAuthAccount({
  UserNameOrEmail: "root",
  Password: "Pass123$"
});
```

### Refresh current token

```ts
const token = await client.refreshAuthAccount();
```

### Change password

```ts
const result = await client.changeAuthPassword({
  CurrentPassword: "Pass123$",
  NewPassword: "Pass456$"
});
```

### Request password reset

```ts
const resetToken = await client.requestAuthPasswordReset({
  UserNameOrEmail: "root"
});
```

### Reset password

```ts
const result = await client.resetAuthPassword({
  UserId: resetToken.UserId as string,
  ResetToken: resetToken.ResetToken as string,
  NewPassword: "Pass789$"
});
```

## Error Handling

All request failures raise `ChillSharpClientError`.

```ts
import { ChillSharpClient, ChillSharpClientError } from "chill-sharp-ts-client";

const client = new ChillSharpClient("http://localhost:5000/api/chill", {
  cultureName: "it-IT"
});

try {
  await client.getSchema("Model.Post", "default");
} catch (error) {
  if (error instanceof ChillSharpClientError) {
    console.log(error.statusCode);
    console.log(error.responseText);
  }
}
```

## Custom Fetch

If you need custom transport behavior, pass your own `fetch` implementation:

```ts
const client = new ChillSharpClient("http://localhost:5000/api/chill", {
  fetchImpl: fetch
});
```

## Generic Payload Strategy

This package does not generate TypeScript model classes for your Chill entities.

That is intentional:

- ChillSharp models are application-specific
- the standard Chill API already works well with generic objects
- a generic client is easier to reuse across many different ChillSharp services

If you need strongly typed TypeScript clients, generate them from your host OpenAPI document as described in [doc/ClientGeneration/README.md](../../doc/ClientGeneration/README.md).

