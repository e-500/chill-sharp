# chill-sharp-vue-client

Vue helpers for a generic ChillSharp service.

This package wraps [`chill-sharp-ts-client`](../chill-sharp-ts-client) and adds:

- `createChillSharpPlugin()` for app-wide client injection
- `useChillSharpClient()` to access the raw client
- `useSchema()` for localized schema loading
- `useText()` for i18n label lookups
- `useTest()` for endpoint health checks
- `useQueryMutation()` and `useEntityMutation()` for generic API actions

It stays generic on purpose. Payloads are plain objects so the same package can work against arbitrary ChillSharp models.

## Install

From the repository root:

```bash
cd ext/chill-sharp-vue-client
npm install
```

This package expects:

- `vue` 3
- `chill-sharp-ts-client`
- a runtime `fetch` implementation, which modern browsers and Node.js 18+ already provide

## Local Linking

The package builds automatically on `npm install`, `npm pack`, and `npm link`.
Link `chill-sharp-ts-client` first, then this package:

```bash
cd ext/chill-sharp-ts-client
npm install
npm link

cd ../chill-sharp-vue-client
npm install
npm link

cd path/to/your-vue-app
npm link chill-sharp-ts-client
npm link chill-sharp-vue-client
```

## Quick Start

```ts
import { createApp, defineComponent } from "vue";
import { createChillSharpPlugin, useSchema } from "chill-sharp-vue-client";

const BlogSchemaName = defineComponent({
  setup() {
    const { data, isLoading, error } = useSchema("Model.Blog", "default");
    return { data, isLoading, error };
  },
  template: `
    <p v-if="isLoading">Loading...</p>
    <p v-else-if="error">Failed to load schema.</p>
    <h1 v-else>{{ String(data?.DisplayName ?? "") }}</h1>
  `
});

const app = createApp(BlogSchemaName);

app.use(createChillSharpPlugin({
  baseUrl: "http://localhost:5000/api/chill",
  options: { cultureName: "it-IT" }
}));

app.mount("#app");
```

## Plugin Setup

### Plugin creates the client

```ts
app.use(createChillSharpPlugin({
  baseUrl: "http://localhost:5000/api/chill",
  options: {
    cultureName: "it-IT",
    accessToken: "your-jwt-token"
  }
}));
```

### Plugin receives a prebuilt client

```ts
import { ChillSharpClient, createChillSharpPlugin } from "chill-sharp-vue-client";

const client = new ChillSharpClient("http://localhost:5000/api/chill", {
  cultureName: "it-IT"
});

app.use(createChillSharpPlugin({
  baseUrl: "http://localhost:5000/api/chill",
  client
}));
```

## Composables

### `useChillSharpClient()`

Use the raw client when you need the full API surface.

```ts
import { defineComponent, onMounted, ref } from "vue";
import { useChillSharpClient } from "chill-sharp-vue-client";

export default defineComponent({
  setup() {
    const client = useChillSharpClient();
    const count = ref(0);

    onMounted(async () => {
      const result = await client.query({
        ChillType: "Query.PostQuery",
        ResultProperties: [{ Name: "Guid" }]
      });

      const rows = Array.isArray(result.Results) ? result.Results : [];
      count.value = rows.length;
    });

    return { count };
  }
});
```

### `useSchema()`

`useSchema()` loads metadata and tracks `isLoading`, `error`, and `reload`.

```ts
const { data, isLoading, error, reload } = useSchema("Model.Post", "default");
```

You can override the plugin culture for one call:

```ts
const englishSchema = useSchema("Model.Post", "default", "en-GB");
```

`useSchema()` also accepts refs:

```ts
const chillType = ref("Model.Post");
const schema = useSchema(chillType, "default");
```

### `useText()`

```ts
const textState = useText(
  "4e16f6c0-6b95-4d67-98bc-9f4d0d63eaf1",
  "it-IT"
);
```

### `useTest()`

```ts
const statusState = useTest();
```

`useTest()` calls `GET /api/chill/test` and exposes the plain-text service status.

### `useQueryMutation()`

```ts
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

Use one composable instance per entity action:

```ts
const createPost = useEntityMutation("create");
const updatePost = useEntityMutation("update");
const deletePost = useEntityMutation("delete");
```

Example:

```ts
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

Because the Vue package reuses the TypeScript client, it inherits the same auth behavior:

- pass `accessToken` when you already have a token
- pass `username` and `password` when the client should log in and refresh automatically
- call `useChillSharpClient()` when you need direct access to `loginAuthAccount()`, `refreshAuthAccount()`, or password-reset flows

## Error Handling

The composables expose the last thrown error. The underlying client throws `ChillSharpClientError`.

```ts
import { computed } from "vue";
import { ChillSharpClientError, useSchema } from "chill-sharp-vue-client";

const { error } = useSchema("Model.Post", "default");

const errorText = computed(() => {
  return error.value instanceof ChillSharpClientError
    ? error.value.responseText ?? ""
    : "";
});
```

## When To Use The Vue Package

Use this package when you want Vue-friendly state handling on top of the generic client.

Use the plain TypeScript package instead when:

- you are not using Vue
- you already have your own data-fetching layer
- you want complete control over caching, retries, and optimistic updates

## Generic Payload Strategy

This package does not generate components or model-specific composables for your Chill entities.

That is intentional:

- ChillSharp models are application-specific
- generic object payloads are enough to talk to the standard ChillSharp API
- model-specific Vue composables are better generated from OpenAPI for each host application

If you need typed model clients, generate them from your host OpenAPI document as described in [doc/ClientGeneration/README.md](../../doc/ClientGeneration/README.md).

