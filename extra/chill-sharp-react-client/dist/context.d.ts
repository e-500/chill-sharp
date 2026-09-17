import React from "react";
import { ChillSharpClient } from "@chill-sharp/ts-client";
import type { ChillSharpClientOptions } from "@chill-sharp/ts-client";
export interface ChillSharpProviderProps {
    baseUrl: string;
    client?: ChillSharpClient;
    options?: ChillSharpClientOptions;
    children: React.ReactNode;
}
export declare function ChillSharpProvider(props: ChillSharpProviderProps): import("react/jsx-runtime").JSX.Element;
export declare function useChillSharpClient(): ChillSharpClient;
