# chill-sharp-react-client

React helpers for a generic ChillSharp service.

This package wraps [`chill-sharp-ts-client`](../chill-sharp-ts-client) and adds:

- a `ChillSharpProvider`
- `useChillSharpClient()` to access the raw client
- `useSchema()` for localized schema loading
- `useSchemaList()` for registered type discovery
- `useText()` and `useTexts()` for i18n label lookups
- `useTest()` for endpoint health checks
- `useQueryMutation()`, `useLookupMutation()`, `useEntityMutation()`, `useAutocompleteMutation()`, and `useValidateMutation()` for generic API actions

It stays generic on purpose. Payloads are plain objects so the same package can work against arbitrary ChillSharp models.

## Install

From the repository root:

```bash
cd extra/chill-sharp-react-client
npm install
```

This package expects:

- `react` 18 or 19
- `chill-sharp-ts-client`
- a runtime `fetch` implementation, which modern browsers and Node.js 18+ already provide

## Local Linking

The package builds automatically on `npm install`, `npm pack`, and `npm link`.
Link `chill-sharp-ts-client` first, then this package:

```bash
cd extra/chill-sharp-ts-client
npm install
npm link

cd ../chill-sharp-react-client
npm install
npm link

cd path/to/your-react-app
npm link chill-sharp-ts-client
npm link chill-sharp-react-client
```

## Quick Start

```tsx
import { ChillSharpProvider, useSchema, useSchemaList, useText, useTexts } from "chill-sharp-react-client";

function BlogSchemaName() {
  const { data, isLoading, error } = useSchema("Model.Blog", "default");

  if (isLoading) return <p>Loading...</p>;
  if (error) return <p>Failed to load schema.</p>;

  return <h1>{String(data?.DisplayName ?? "")}</h1>;
}

export function App() {
  return (
    <ChillSharpProvider
      baseUrl="http://localhost:5000/api/chill"
      options={{ cultureName: "it-IT" }}
    >
      <BlogSchemaName />
    </ChillSharpProvider>
  );
}
```

## Provider Setup

### Provider creates the client

```tsx
<ChillSharpProvider
  baseUrl="http://localhost:5000/api/chill"
  options={{
    cultureName: "it-IT",
    accessToken: "your-jwt-token"
  }}
>
  <App />
</ChillSharpProvider>
```

### Provider receives a prebuilt client

```tsx
import { ChillSharpClient } from "chill-sharp-react-client";

const client = new ChillSharpClient("http://localhost:5000/api/chill", {
  cultureName: "it-IT"
});

<ChillSharpProvider baseUrl="http://localhost:5000/api/chill" client={client}>
  <App />
</ChillSharpProvider>;
```

## Hooks

### `useChillSharpClient()`

Use the raw client when you need the full API surface.

```tsx
import { useEffect, useState } from "react";
import { useChillSharpClient } from "chill-sharp-react-client";

function PostCount() {
  const client = useChillSharpClient();
  const [count, setCount] = useState<number>(0);

  useEffect(() => {
    void client.query({
      ChillType: "Query.PostQuery",
      ResultProperties: [{ Name: "Guid" }]
    }).then(result => {
      const rows = Array.isArray(result.Results) ? result.Results : [];
      setCount(rows.length);
    });
  }, [client]);

  return <span>{count}</span>;
}
```

### `useSchema()`

`useSchema()` loads metadata and tracks `isLoading`, `error`, and `reload`.

```tsx
const { data, isLoading, error, reload } = useSchema("Model.Post", "default");
const handleAttachments = data?.handleAttachments;
const relations = data?.relations ?? [];
```

You can override the provider culture for one call:

```tsx
const englishSchema = useSchema("Model.Post", "default", "en-GB");
```

Pass `update: true` as the fourth argument when you want the server to refresh a persisted schema from the current runtime model:

```tsx
const refreshedSchema = useSchema("Model.Post", "default", undefined, true);
```

Existing properties keep their saved metadata, new model properties are added, and removed model properties are dropped from the persisted schema.

The schema and entity-option payloads re-exported by this package include `handleAttachments` and schema-level `relations`. Query payloads also include `ordering`, and entity payloads include `position` with backend default `0`.

### `useSchemaList()`

```tsx
const { data, isLoading, error, reload } = useSchemaList();
const englishSchemaList = useSchemaList("en-GB");
```

### `useText()`

```tsx
const { data, isLoading } = useText({
  LabelGuid: "4e16f6c0-6b95-4d67-98bc-9f4d0d63eaf1",
  CultureName: "it-IT",
  PrimaryCultureName: "en-GB",
  PrimaryDefaultText: "Blog title",
  SecondaryCultureName: "it-IT",
  SecondaryDefaultText: "Titolo del blog"
});
```

### `useTexts()`

```tsx
const { data: texts, isLoading: isTextsLoading } = useTexts([
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

### `useTest()`

```tsx
const { data, isLoading, reload } = useTest();
```

`useTest()` calls `GET /api/chill/test` and returns the plain-text service status.

### `useQueryMutation()`

Use `useQueryMutation()` when `ChillType` points to a concrete query type such as `Query.PostQuery`.

`	sx
const { execute, data, isLoading } = useQueryMutation();

async function runQuery() {
  await execute({
    chillType: "Query.PostQuery",
    properties: { title: "Hello" },
    ordering: {
      propertyName: "Position",
      direction: "ASC"
    },
    resultProperties: [{ name: "Guid" }, { name: "Title" }]
  });
}
```

If `ordering.propertyName` points to a Chill entity reference, the backend orders by that referenced entity `Label`.

### `useLookupMutation()`

```tsx
const lookupPosts = useLookupMutation();

await lookupPosts.execute({
  chillType: "Model.Post",
  properties: {
    fullTextSearch: "Ada Lovelace"
  },
  ordering: {
    propertyName: "Blog",
    direction: "ASC"
  },
  resultProperties: [{ name: "Guid" }, { name: "Title" }]
});
```

Use `useLookupMutation()` when `ChillType` points to an entity type and you only need generic full-text search.

### `useAutocompleteMutation()`

```tsx
const autocompletePost = useAutocompleteMutation();

await autocompletePost.execute({
  ChillType: "Model.Post",
  Properties: {
    Title: "  Draft title  "
  }
});
```

### `useValidateMutation()`

```tsx
const validatePost = useValidateMutation();

const errors = await validatePost.execute({
  ChillType: "Model.Post",
  Properties: {
    Title: ""
  }
});
```

### `useEntityMutation()`

Use one hook instance per entity action:

```tsx
const createPost = useEntityMutation("create");
const updatePost = useEntityMutation("update");
const deletePost = useEntityMutation("delete");
```

Example:

```tsx
await createPost.execute({
  chillType: "Model.Post",
  guid: crypto.randomUUID(),
  position: 10,
  properties: {
    title: "New title",
    author: "Grace Hopper"
  }
});
```

## Attachments

Use the raw client from `useChillSharpClient()` for attachment helpers:

```tsx
function AttachmentActions({ postGuid }: { postGuid: string }) {
  const client = useChillSharpClient();

  async function upload() {
    await client.uploadAttachment(
      { ChillType: "Model.Post", Guid: postGuid },
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
  }

  return <button onClick={() => void upload()}>Upload</button>;
}
```

## Chunk batches

Call `chunk()` through `useChillSharpClient()` when several operations should be sent in one request.

```tsx
function SaveBatch() {
  const client = useChillSharpClient();

  async function executeBatch(existingGuid: string) {
    await client.chunk([
      { Index: 0, Verb: "transaction" },
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
      { Index: 3, Verb: "commit" }
    ]);
  }

  return null;
}
```

Use `transaction` and `commit` only when the enclosed write operations must be committed together.

## Authentication

Because the React package reuses the TypeScript client, it inherits the same auth behavior:

- pass `accessToken` when you already have a token
- pass `username` and `password` when the client should log in and refresh automatically
- pass `DisplayCultureName` during registration when the server should preset auth-user display preferences
- call `useChillSharpClient()` when you need direct access to auth account methods, auth management methods, or schema-management methods like `getEntityOptions()`, `setEntityOptions()`, `getMenu()`, `setMenu()`, and `deleteMenu()`
- schema and entity option payloads re-exported by this package include the `handleAttachments` flag from `chill-sharp-ts-client`
- query payloads re-exported by this package include `ordering`, and entity payloads include `position`

Auth user list/detail payloads exposed through the raw client include:

- `displayCultureName`
- `displayTimeZone`
- `displayDateFormat`
- `displayNumberFormat`

## Error Handling

The hooks expose the last thrown error. The underlying client throws `ChillSharpClientError`.

```tsx
import { ChillSharpClientError, useSchema } from "chill-sharp-react-client";

function SchemaStatus() {
  const { error } = useSchema("Model.Post", "default");

  if (error instanceof ChillSharpClientError) {
    return <pre>{error.responseText}</pre>;
  }

  return null;
}
```

## When To Use The React Package

Use this package when you want React-friendly state handling on top of the generic client.

Use the plain TypeScript package instead when:

- you are not using React
- you already have your own data-fetching layer
- you want complete control over caching, retries, and optimistic updates

## Generic Payload Strategy

This package does not generate React components or model-specific hooks for your Chill entities.

That is intentional:

- ChillSharp models are application-specific
- generic object payloads are enough to talk to the standard ChillSharp API
- model-specific React hooks are better generated from OpenAPI for each host application

If you need typed model clients, generate them from your host OpenAPI document as described in [doc/ClientGeneration/README.md](../../doc/ClientGeneration/README.md).






## Menu endpoints

When you use the raw client from `useChillSharpClient()`, `getMenu()` loads root menu nodes or the direct children of one menu item, `setMenu()` creates or updates one menu item, and `deleteMenu()` removes one menu item together with its child subtree. Menu items include `positionNo`, which the backend persists and uses to order siblings.

For the complete tree model, delete behavior, and `MenuHierarchy` filtering behavior, see [../../doc/MenuModel.md](../../doc/MenuModel.md).
