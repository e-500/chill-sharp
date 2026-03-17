# chill-sharp-react-client

React helpers for a generic ChillSharp service.

This package wraps [`chill-sharp-ts-client`](../chill-sharp-ts-client) and adds:

- a `ChillSharpProvider`
- `useChillSharpClient()` to access the raw client
- `useSchema()` for localized schema loading
- `useText()` for i18n label lookups
- `useTest()` for endpoint health checks
- `useQueryMutation()` and `useEntityMutation()` for generic API actions

It stays generic on purpose. Payloads are plain objects so the same package can work against arbitrary ChillSharp models.

## Install

From the repository root:

```bash
cd ext/chill-sharp-react-client
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
cd ext/chill-sharp-ts-client
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
import { ChillSharpProvider, useSchema } from "chill-sharp-react-client";

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
```

You can override the provider culture for one call:

```tsx
const englishSchema = useSchema("Model.Post", "default", "en-GB");
```

### `useText()`

```tsx
const { data, isLoading } = useText(
  "4e16f6c0-6b95-4d67-98bc-9f4d0d63eaf1",
  "it-IT"
);
```

### `useTest()`

```tsx
const { data, isLoading, reload } = useTest();
```

`useTest()` calls `GET /api/chill/test` and returns the plain-text service status.

### `useQueryMutation()`

```tsx
const { execute, data, isLoading } = useQueryMutation();

async function runQuery() {
  await execute({
    ChillType: "Query.PostQuery",
    Properties: { Title: "Hello" },
    ResultProperties: [{ Name: "Guid" }, { Name: "Title" }]
  });
}
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
  ChillType: "Model.Post",
  Guid: crypto.randomUUID(),
  Properties: {
    Title: "New title",
    Author: "Grace Hopper"
  }
});
```

## Authentication

Because the React package reuses the TypeScript client, it inherits the same auth behavior:

- pass `accessToken` when you already have a token
- pass `username` and `password` when the client should log in and refresh automatically
- call `useChillSharpClient()` when you need direct access to `loginAuthAccount()`, `refreshAuthAccount()`, or password-reset flows

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

