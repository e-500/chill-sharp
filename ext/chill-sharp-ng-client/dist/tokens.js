import { InjectionToken } from "@angular/core";
import { ChillSharpClient } from "chill-sharp-ts-client";
export const CHILL_SHARP_CLIENT = new InjectionToken("CHILL_SHARP_CLIENT");
export function createChillSharpClient(config) {
    if (config.client) {
        return config.client;
    }
    return new ChillSharpClient(config.baseUrl, config.options);
}
export function provideChillSharpClient(config) {
    const client = createChillSharpClient(config);
    return [
        {
            provide: CHILL_SHARP_CLIENT,
            useValue: client
        }
    ];
}
