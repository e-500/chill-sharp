// Shared cache behavior is upgraded with @chill-sharp/ui-core.
self.CHILL_SHARP_SW_OPTIONS = {
  cachePrefix: 'chill-sharp-ui',
  cacheVersion: 'v2',
  cacheTimeoutMs: 10 * 60 * 1000,
  appShell: ['/', '/index.html']
};
importScripts('./chill-sharp-service-worker.js');
