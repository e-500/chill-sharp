import type { App, InjectionKey, Plugin } from "vue";
import { inject } from "vue";
import { ChillSharpClient } from "chill-sharp-ts-client";
import type { ChillSharpClientOptions } from "chill-sharp-ts-client";

export interface ChillSharpVueOptions {
  baseUrl: string;
  client?: ChillSharpClient;
  options?: ChillSharpClientOptions;
}

export const chillSharpClientKey: InjectionKey<ChillSharpClient> = Symbol("ChillSharpClient");

export function createChillSharpClient(options: ChillSharpVueOptions): ChillSharpClient {
  if (options.client) {
    return options.client;
  }

  return new ChillSharpClient(options.baseUrl, options.options);
}

export function createChillSharpPlugin(options: ChillSharpVueOptions): Plugin {
  const client = createChillSharpClient(options);

  return {
    install(app: App) {
      app.provide(chillSharpClientKey, client);
    }
  };
}

export function useChillSharpClient(): ChillSharpClient {
  const client = inject(chillSharpClientKey, null);
  if (!client) {
    throw new Error("useChillSharpClient requires createChillSharpPlugin() to be installed.");
  }

  return client;
}
