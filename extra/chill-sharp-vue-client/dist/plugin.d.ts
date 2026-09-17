import type { InjectionKey, Plugin } from "vue";
import { ChillSharpClient } from "@chill-sharp/ts-client";
import type { ChillSharpClientOptions } from "@chill-sharp/ts-client";
export interface ChillSharpVueOptions {
    baseUrl: string;
    client?: ChillSharpClient;
    options?: ChillSharpClientOptions;
}
export declare const chillSharpClientKey: InjectionKey<ChillSharpClient>;
export declare function createChillSharpClient(options: ChillSharpVueOptions): ChillSharpClient;
export declare function createChillSharpPlugin(options: ChillSharpVueOptions): Plugin;
export declare function useChillSharpClient(): ChillSharpClient;
