export interface ClientTemplateRuntimeConfig {
  tenantCode: string;
  featureFlags: Record<string, boolean>;
}

export function readClientRuntimeConfig(): ClientTemplateRuntimeConfig {
  const runtimeConfig = globalThis.__clientUiTemplateRuntimeConfig__;

  return {
    tenantCode: runtimeConfig?.tenantCode ?? 'template-client',
    featureFlags: runtimeConfig?.featureFlags ?? {}
  };
}
