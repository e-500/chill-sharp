import { inject } from "vue";
import { ChillSharpClient } from "chill-sharp-ts-client";
export const chillSharpClientKey = Symbol("ChillSharpClient");
export function createChillSharpClient(options) {
    if (options.client) {
        return options.client;
    }
    return new ChillSharpClient(options.baseUrl, options.options);
}
export function createChillSharpPlugin(options) {
    const client = createChillSharpClient(options);
    return {
        install(app) {
            app.provide(chillSharpClientKey, client);
        }
    };
}
export function useChillSharpClient() {
    const client = inject(chillSharpClientKey, null);
    if (!client) {
        throw new Error("useChillSharpClient requires createChillSharpPlugin() to be installed.");
    }
    return client;
}
