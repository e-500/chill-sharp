export const environment = {
  production: false,
  apiBaseUrl: globalThis.CHILLSHARP_API_URL?.trim() || 'http://localhost:6002/api'
};
