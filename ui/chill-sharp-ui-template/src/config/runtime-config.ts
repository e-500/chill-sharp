export interface ClientTemplateRuntimeConfig {
  tenantCode: string;
  featureFlags: Record<string, boolean>;
}

export function readClientRuntimeConfig(): ClientTemplateRuntimeConfig {
  return globalThis.__clientUiTemplateRuntimeConfig__ ?? {
    tenantCode: 'template-client',
    featureFlags: {}
  };
}
