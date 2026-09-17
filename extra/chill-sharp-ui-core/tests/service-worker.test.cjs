const { test } = require('node:test');
const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const workerPath = process.env.CHILL_WORKER_PATH || path.join(__dirname, '../service-worker/chill-sharp-service-worker.js');
const source = readFileSync(workerPath, 'utf8');
const origin = 'https://ui.example.test';
const schema = 'https://api.example.test/api/chill-schema/get-schema?chillType=Model.Cart&chillViewCode=default&cultureName=it-IT';
const options = 'https://api.example.test/api/chill-schema/get-entity-options?chillType=Model.Cart';
const list = 'https://api.example.test/api/chill-schema/get-schema-list?cultureName=it-IT';
const deferred = () => { let resolve; const promise = new Promise((r) => { resolve = r; }); return { promise, resolve }; };

function harness() {
  const listeners = new Map();
  const storage = new Map();
  const requests = [];
  let now = 1_000_000;
  let version = 1;
  let network = async (request) => {
    const url = new URL(request.url);
    if (request.method === 'POST' || /update=true/i.test(url.search)) version++;
    return Response.json({ version, token: request.headers.get('Authorization') });
  };
  const toRequest = (input) => input instanceof Request ? input : new Request(new URL(input, origin));
  const caches = {
    keys: async () => [...storage.keys()],
    delete: async (name) => storage.delete(name),
    open: async (name) => {
      if (!storage.has(name)) storage.set(name, new Map());
      const entries = storage.get(name);
      return {
        match: async (input) => {
          const request = toRequest(input);
          const entry = entries.get(request.url);
          if (!entry) return undefined;
          for (const header of (entry.response.headers.get('Vary') || '').split(',').filter(Boolean)) {
            if (request.headers.get(header.trim()) !== entry.request.headers.get(header.trim())) return undefined;
          }
          return entry.response.clone();
        },
        put: async (input, response) => {
          const request = toRequest(input);
          entries.set(request.url, { request: request.clone(), response: response.clone() });
        },
        delete: async (input) => entries.delete(toRequest(input).url),
        addAll: async (inputs) => {
          for (const input of inputs) {
            const request = toRequest(input);
            entries.set(request.url, { request, response: new Response('shell') });
          }
        }
      };
    }
  };
  const context = vm.createContext({
    Request, Response, Headers, URL, Set, Promise,
    Date: class extends Date { static now() { return now; } },
    caches,
    fetch: async (request) => { requests.push(request); return network(request); },
    self: {
      location: new URL('/sw.js', origin),
      addEventListener: (name, handler) => listeners.set(name, handler),
      skipWaiting: async () => {}, clients: { claim: async () => {} }
    }
  });
  vm.runInContext(source, context);
  async function dispatch(url, init = {}) {
    const request = new Request(url, { headers: { Authorization: 'Bearer user-a' }, ...init });
    let response;
    const work = [];
    listeners.get('fetch')({ request, respondWith: (value) => { response = value; }, waitUntil: (value) => work.push(value) });
    const result = response ? await response : await context.fetch(request);
    await Promise.all(work);
    return result;
  }
  return {
    caches, storage, requests, dispatch,
    json: async (url, init) => (await dispatch(url, init)).json(),
    tick: (ms) => { now += ms; },
    setVersion: (value) => { version = value; },
    setNetwork: (value) => { network = value; },
    lifecycle: async (name) => {
      const work = [];
      listeners.get(name)({ waitUntil: (value) => work.push(value) });
      await Promise.all(work);
    }
  };
}

test('schema and options expire at exactly ten minutes, including cross-origin APIs', async () => {
  const h = harness();
  for (const url of [schema, options, list]) assert.equal((await h.json(url)).version, 1);
  h.setVersion(2);
  h.tick(600_000 - 1);
  for (const url of [schema, options, list]) assert.equal((await h.json(url)).version, 1);
  h.tick(1);
  for (const url of [schema, options, list]) assert.equal((await h.json(url)).version, 2);
  assert.equal(h.requests.length, 6);
  assert.ok(h.requests.every((r) => r.cache === 'no-store'));
});

test('every Update schema click reaches the server and the first normal refresh stays fresh', async () => {
  const h = harness();
  await h.json(schema);
  await h.json(options);
  await h.json(list);
  assert.equal((await h.json(schema + '&update=true')).version, 2);
  for (const url of [schema, options, list]) assert.equal((await h.json(url)).version, 2);
  assert.equal((await h.json(schema + '&update=true')).version, 3);
  assert.equal((await h.json(schema)).version, 3);
  assert.equal((await h.json(schema)).version, 3);
  assert.equal(h.requests.filter((r) => r.url.includes('update=true')).length, 2);
});

for (const action of ['set-schema', 'set-entity-options']) {
  test(`${action} clears schemas, options, lists and all view/culture variants`, async () => {
    const h = harness();
    const urls = [schema, schema.replace('it-IT', 'en-US'), schema.replace('default', 'details'), options, list];
    for (const url of urls) await h.json(url);
    await h.json(`https://api.example.test/api/chill-schema/${action}`, { method: 'POST', body: '{}' });
    for (const url of urls) assert.equal((await h.json(url)).version, 2);
  });
}

test('Update and True parameter casing is handled like the backend', async () => {
  const h = harness();
  await h.json(schema);
  await h.json(schema + '&Update=True');
  assert.equal((await h.json(schema)).version, 2);
});

test('in-flight old GET cannot put stale data back after an update', async () => {
  const h = harness();
  const started = deferred();
  const release = deferred();
  let updated = false;
  h.setNetwork(async (request) => {
    if (request.url.includes('update=true')) { updated = true; return Response.json({ version: 2 }); }
    if (!updated) { started.resolve(); await release.promise; return Response.json({ version: 1 }); }
    return Response.json({ version: 2 });
  });
  const oldRead = h.json(schema);
  await started.promise;
  await h.json(schema + '&update=true');
  release.resolve();
  await oldRead;
  assert.equal((await h.json(schema)).version, 2);
  assert.equal((await h.json(schema)).version, 2);
});

test('a read during an update waits until the update and invalidation finish', async () => {
  const h = harness();
  await h.json(schema);
  const started = deferred();
  const release = deferred();
  h.setNetwork(async (request) => {
    if (request.url.includes('update=true')) { started.resolve(); await release.promise; }
    return Response.json({ version: 2 });
  });
  const update = h.json(schema + '&update=true');
  await started.promise;
  const read = h.json(schema);
  release.resolve();
  await update;
  assert.equal((await read).version, 2);
});

test('cached bearer responses are never reused for a different user or an anonymous request', async () => {
  const h = harness();
  await h.json(schema);
  assert.equal((await h.json(schema, { headers: { Authorization: 'Bearer user-b' } })).token, 'Bearer user-b');
  assert.equal((await h.json(schema, { headers: {} })).token, null);
  assert.equal(h.requests.length, 3);
});

test('failed update clears old data and does not block later reads', async () => {
  const h = harness();
  await h.json(schema);
  h.setNetwork(async (request) => {
    if (request.url.includes('update=true')) throw new Error('offline');
    return Response.json({ version: 2 });
  });
  await assert.rejects(h.json(schema + '&update=true'), /offline/);
  assert.equal((await h.json(schema)).version, 2);
});

test('expired metadata is not served on network errors; HTTP errors are not cached', async () => {
  const h = harness();
  await h.json(schema);
  h.tick(600_000);
  h.setNetwork(async () => { throw new Error('offline'); });
  await assert.rejects(h.json(schema), /offline/);
  h.setNetwork(async () => new Response('unauthorized', { status: 401 }));
  assert.equal((await h.dispatch(schema)).status, 401);
  h.setNetwork(async () => Response.json({ version: 2 }));
  assert.equal((await h.json(schema)).version, 2);
});

test('no-store responses and other APIs/runtime configuration never enter the cache', async () => {
  const h = harness();
  h.setNetwork(async () => Response.json({}, { headers: { 'Cache-Control': 'no-store' } }));
  const urls = [schema, origin + '/api/chill/get-entity', origin + '/env.js', origin + '/runtime-config.js'];
  for (const url of urls) { await h.json(url); await h.json(url); }
  assert.equal(h.requests.length, urls.length * 2);
  assert.ok(h.requests.every((r) => r.cache === 'no-store'));
});

test('activation deletes only this application legacy caches and retains unrelated caches', async () => {
  const h = harness();
  const config = source.match(/cachePrefix: '([^']+)'/)[1];
  await h.caches.open(`${config}-shell-v1`);
  await h.caches.open('unrelated-app-cache');
  await h.lifecycle('install');
  await h.lifecycle('activate');
  assert.ok(!(await h.caches.keys()).includes(`${config}-shell-v1`));
  assert.ok((await h.caches.keys()).includes('unrelated-app-cache'));
  assert.ok((await h.caches.keys()).includes(`${config}-shell-v2`));
});
