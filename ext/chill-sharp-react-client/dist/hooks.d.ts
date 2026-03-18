import type { GetTextRequest, GetTextResponse, JsonObject } from "chill-sharp-ts-client";
export interface UseChillAsyncState<TData> {
    data: TData | null;
    error: unknown;
    isLoading: boolean;
    reload: () => Promise<TData | null>;
}
export interface UseChillMutationState<TData> {
    data: TData | null;
    error: unknown;
    isLoading: boolean;
    execute: (...args: unknown[]) => Promise<TData>;
    reset: () => void;
}
export declare function useSchema(chillType: string, chillViewCode?: string, cultureName?: string): UseChillAsyncState<JsonObject>;
export declare function useText(request: GetTextRequest): UseChillAsyncState<GetTextResponse>;
export declare function useTexts(requests: GetTextRequest[]): UseChillAsyncState<Array<GetTextResponse | null>>;
export declare function useVersion(): string;
export declare function useTest(): UseChillAsyncState<string>;
export declare function useQueryMutation(): UseChillMutationState<JsonObject>;
export declare function useEntityMutation(action: "find" | "create" | "update" | "delete"): UseChillMutationState<JsonObject | null>;
