// Shared worker, loaded by the client-owned public/sw.js through importScripts().
// Set self.CHILL_SHARP_SW_OPTIONS before importing to customize these defaults.
const OPTIONS = {
  cachePrefix: 'chill-sharp-ui',
  cacheVersion: 'v2',
  cacheTimeoutMs: 10 * 60 * 1000,
  appShell: ['/', '/index.html'],
  legacyCacheNames: [],
  ...self.CHILL_SHARP_SW_OPTIONS
};

const SHELL_CACHE = `${OPTIONS.cachePrefix}-shell-${OPTIONS.cacheVersion}`;
const METADATA_CACHE = `${OPTIONS.cachePrefix}-metadata-${OPTIONS.cacheVersion}`;
const CACHED_AT = 'X-ChillSharp-Cached-At';
const METADATA_READS = new Set(['get-schema', 'get-schema-list', 'get-entity-options']);
const METADATA_WRITES = new Set(['set-schema', 'set-entity-options']);
let metadataGeneration = 0;
let pendingMutation = Promise.resolve();
let cacheOperations = Promise.resolve();

// Order cache writes/deletions so a pre-update fetch cannot refill an invalidated cache.
function changeMetadataCache(action) {
  const operation = cacheOperations.then(action);
  cacheOperations = operation.catch(() => {});
  return operation;
}

function schemaAction(url) {
  const match = url.pathname.match(/\/chill-schema\/([^/]+)\/?$/i);
  return match ? match[1].toLowerCase() : '';
}

function isSchemaUpdate(url, action) {
  return action === 'get-schema' && [...url.searchParams].some(
    ([name, value]) => name.toLowerCase() === 'update' && value.toLowerCase() === 'true'
  );
}

async function freshCachedResponse(cacheName, request) {
  const cache = await caches.open(cacheName);
  const response = await cache.match(request);
  if (!response) return undefined;
  const cachedAt = Number(response.headers.get(CACHED_AT));
  const age = Date.now() - cachedAt;
  if (cachedAt > 0 && age >= 0 && age < OPTIONS.cacheTimeoutMs) return response;
  await cache.delete(request);
  return undefined;
}

async function storeResponse(cacheName, request, response) {
  if (!response.ok || [204, 205, 206].includes(response.status) || response.type === 'opaque' ||
      /\bno-store\b/i.test(response.headers.get('Cache-Control') || '') ||
      (response.headers.get('Vary') || '').split(',').some((name) => name.trim() === '*')) return;

  const headers = new Headers(response.headers);
  headers.set(CACHED_AT, String(Date.now()));
  // Cache API does not distinguish bearer tokens unless the response varies by them.
  const vary = new Set((headers.get('Vary') || '').split(',').map((v) => v.trim()).filter(Boolean));
  vary.add('Authorization');
  headers.set('Vary', [...vary].join(', '));
  const copy = new Response(await response.clone().blob(), {
    status: response.status, statusText: response.statusText, headers
  });
  await (await caches.open(cacheName)).put(request, copy);
}

function fetchFresh(request) {
  return fetch(new Request(request, { cache: 'no-store' }));
}

async function readMetadata(request) {
  await pendingMutation;
  const generation = metadataGeneration;
  const cached = await changeMetadataCache(() => freshCachedResponse(METADATA_CACHE, request)).catch(() => undefined);
  if (cached && generation === metadataGeneration) return cached;
  const response = await fetchFresh(request);
  await changeMetadataCache(async () => {
    if (generation === metadataGeneration) await storeResponse(METADATA_CACHE, request, response);
  }).catch(() => {}); // Storage quota errors must not discard a successful network response.
  return response;
}

function mutateMetadata(request) {
  metadataGeneration++;
  const operation = pendingMutation.then(async () => {
    await changeMetadataCache(() => caches.delete(METADATA_CACHE));
    try {
      // update=true must reach the server on every click, including repeated updates.
      return await fetchFresh(request);
    } finally {
      // Clear all types/views/cultures and entity options, including schema-list summaries.
      // Normal GETs after the update (also after a reload) must fetch the saved schema.
      await changeMetadataCache(() => caches.delete(METADATA_CACHE));
    }
  });
  pendingMutation = operation.then(() => {}, () => {});
  return operation;
}

self.addEventListener('install', (event) => {
  event.waitUntil((async () => {
    const cache = await caches.open(SHELL_CACHE);
    await cache.addAll(OPTIONS.appShell.map((url) => new Request(new URL(url, self.location.href), { cache: 'reload' })));
    await self.skipWaiting();
  })());
});

self.addEventListener('activate', (event) => {
  event.waitUntil((async () => {
    const current = new Set([SHELL_CACHE, METADATA_CACHE]);
    await Promise.all((await caches.keys())
      .filter((key) => OPTIONS.legacyCacheNames.includes(key) ||
        (key.startsWith(`${OPTIONS.cachePrefix}-`) && !current.has(key)))
      .map((key) => caches.delete(key)));
    await self.clients.claim();
  })());
});

self.addEventListener('fetch', (event) => {
  const request = event.request;
  const url = new URL(request.url);
  const action = schemaAction(url);

  if ((request.method === 'GET' && isSchemaUpdate(url, action)) ||
      (request.method === 'POST' && METADATA_WRITES.has(action))) {
    const response = mutateMetadata(request);
    event.respondWith(response);
    event.waitUntil(response.then(() => {}, () => {}));
    return;
  }

  if (request.method !== 'GET') return;
  if (METADATA_READS.has(action)) {
    const response = readMetadata(request);
    event.respondWith(response);
    event.waitUntil(response.then(() => {}, () => {}));
    return;
  }

  // Only metadata APIs above are cached. Other APIs and runtime settings stay fresh.
  if (action || /\/api(?:\/|$)/i.test(url.pathname) ||
      /\/(?:env|runtime-config|sw)\.js$/i.test(url.pathname)) {
    event.respondWith(fetchFresh(request));
    return;
  }

  if (url.origin !== self.location.origin || request.headers.has('Authorization')) return;
  if (request.mode === 'navigate') {
    event.respondWith(fetchFresh(request).catch(async () => {
      const shell = await (await caches.open(SHELL_CACHE)).match('/index.html');
      return shell || Response.error();
    }));
    return;
  }

  if (!['script', 'style', 'image', 'font', 'manifest'].includes(request.destination)) return;
  const response = (async () => {
    const cached = await freshCachedResponse(SHELL_CACHE, request);
    if (cached) return cached;
    const result = await fetchFresh(request);
    await storeResponse(SHELL_CACHE, request, result).catch(() => {});
    return result;
  })();
  event.respondWith(response);
  event.waitUntil(response.then(() => {}, () => {}));
});
