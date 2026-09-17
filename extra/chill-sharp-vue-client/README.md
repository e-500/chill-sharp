# chill-sharp-vue-client

Vue helpers for a generic ChillSharp service.

This package wraps [`chill-sharp-ts-client`](../chill-sharp-ts-client) and adds:

- `createChillSharpPlugin()` for app-wide client injection
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
cd extra/chill-sharp-vue-client
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
cd extra/chill-sharp-ts-client
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
import { createChillSharpPlugin, useSchema, useSchemaList, useText, useTexts } from "chill-sharp-vue-client";

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
const handleAttachments = computed(() => data.value?.handleAttachments ?? false);
const relations = computed(() => data.value?.relations ?? []);
```

You can override the plugin culture for one call:

```ts
const englishSchema = useSchema("Model.Post", "default", "en-GB");
```

Pass `update: true` as the fourth argument when you want the server to refresh a persisted schema from the current runtime model:

```ts
const refreshedSchema = useSchema("Model.Post", "default", undefined, true);
```

Existing properties keep their saved metadata, new model properties are added, and removed model properties are dropped from the persisted schema.

The schema and entity-option payloads re-exported by this package include `handleAttachments` and schema-level `relations`. Query payloads also include `ordering`, and entity payloads include `position` with backend default `0`.

`useSchema()` also accepts refs:

```ts
const chillType = ref("Model.Post");
const schema = useSchema(chillType, "default");
```

### `useSchemaList()`

```ts
const schemaListState = useSchemaList();
const englishSchemaListState = useSchemaList("en-GB");
```

### `useText()`

```ts
const textState = useText({
  LabelGuid: "4e16f6c0-6b95-4d67-98bc-9f4d0d63eaf1",
  CultureName: "it-IT",
  PrimaryCultureName: "en-GB",
  PrimaryDefaultText: "Blog title",
  SecondaryCultureName: "it-IT",
  SecondaryDefaultText: "Titolo del blog"
});
```

### `useTexts()`

```ts
const textsState = useTexts([
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

```ts
const statusState = useTest();
```

`useTest()` calls `GET /api/chill/test` and exposes the plain-text service status.

### `useQueryMutation()`

Use `useQueryMutation()` when `ChillType` points to a concrete query type such as `Query.PostQuery`.

`	s
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

```ts
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

```ts
const autocompletePost = useAutocompleteMutation();

await autocompletePost.execute({
  ChillType: "Model.Post",
  Properties: {
    Title: "  Draft title  "
  }
});
```

### `useValidateMutation()`

```ts
const validatePost = useValidateMutation();

const errors = await validatePost.execute({
  ChillType: "Model.Post",
  Properties: {
    Title: ""
  }
});
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

```ts
const client = useChillSharpClient();

await client.uploadAttachment(
  {
    ChillType: "Model.Post",
    Guid: postGuid
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

const attachments = await client.getAttachments({
  ChillType: "Model.Post",
  Guid: postGuid
});
```

## Chunk batches

Call `chunk()` through `useChillSharpClient()` when several operations should be sent in one request.

```ts
const client = useChillSharpClient();

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
```

Use `transaction` and `commit` only when the enclosed write operations must be committed together.

## Authentication

Because the Vue package reuses the TypeScript client, it inherits the same auth behavior:

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







## Menu endpoints

When you use the raw client from `useChillSharpClient()`, `getMenu()` loads root menu nodes or the direct children of one menu item, `setMenu()` creates or updates one menu item, and `deleteMenu()` removes one menu item together with its child subtree. Menu items include `positionNo`, which the backend persists and uses to order siblings.

For the complete tree model, delete behavior, and `MenuHierarchy` filtering behavior, see [../../doc/MenuModel.md](../../doc/MenuModel.md).
