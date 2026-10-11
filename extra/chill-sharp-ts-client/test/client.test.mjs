import assert from "node:assert/strict";
import test from "node:test";
import { ChillSharpClient, PermissionAction } from "../dist/client.js";

test("Command permissions and command lookups are available", async () => {
  const requests = [];
  const client = new ChillSharpClient("https://example.test/api/chill", {
    fetchImpl: async input => {
      requests.push(String(input));
      return Response.json(["RenameItem"]);
    }
  });

  assert.equal(PermissionAction.Command, 7);
  assert.deepEqual(await client.getAuthCommandList("Commands"), ["RenameItem"]);
  assert.deepEqual(requests, ["https://example.test/api/chill-auth/get-command-list?module=Commands"]);
});

test("CQRS and CRUD methods POST their DTOs to the matching Chill endpoints", async () => {
  const requests = [];
  const client = new ChillSharpClient("https://example.test/api/chill", {
    fetchImpl: async (input, init) => {
      const url = new URL(String(input));
      requests.push({
        path: url.pathname,
        method: init.method,
        contentType: init.headers.get("Content-Type"),
        body: init.body ? JSON.parse(init.body) : undefined
      });

      if (url.pathname.endsWith("/delete")) {
        return new Response(null, { status: 204 });
      }

      return Response.json({ endpoint: url.pathname.split("/").at(-1) });
    }
  });

  const command = { chillType: "Commands.RenameItem", parameters: { itemGuid: "item-1", name: "updated" } };
  const query = { chillType: "Query.ItemQuery", properties: { guid: "item-1" } };
  const entity = { chillType: "Model.Item", guid: "item-1", properties: { name: "updated" } };

  assert.deepEqual(await client.command(command), { endpoint: "command" });
  assert.deepEqual(await client.query(query), { endpoint: "query" });
  assert.deepEqual(await client.find(entity), { endpoint: "find" });
  assert.deepEqual(await client.create(entity), { endpoint: "create" });
  assert.deepEqual(await client.update(entity), { endpoint: "update" });
  assert.equal(await client.delete(entity), undefined);

  assert.deepEqual(requests.map(({ path, method }) => ({ path, method })), [
    { path: "/api/chill/command", method: "POST" },
    { path: "/api/chill/query", method: "POST" },
    { path: "/api/chill/find", method: "POST" },
    { path: "/api/chill/create", method: "POST" },
    { path: "/api/chill/update", method: "POST" },
    { path: "/api/chill/delete", method: "POST" }
  ]);
  assert.ok(requests.every(request => request.contentType === "application/json"));
  assert.deepEqual(requests.map(request => request.body), [command, query, entity, entity, entity, entity]);
});