declare global {
  var CHILLSHARP_API_URL: string | undefined;
  var CHILLSHARP_UI_URL: string | undefined;
  var __clientUiTemplateRuntimeConfig__: {
    tenantCode?: string;
    featureFlags?: Record<string, boolean>;
  } | undefined;
}

export {};
