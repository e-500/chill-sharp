import type { ChillDtoSchema, ChillDtoSchemaListItem, ChillEntityChangeCallback, GetTextRequest, GetTextResponse, JsonObject } from "chill-sharp-ts-client";
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
export interface UseChillSubscriptionState {
    error: unknown;
    isSubscribed: boolean;
}
export declare function useSchema(chillType: string, chillViewCode?: string, cultureName?: string): UseChillAsyncState<ChillDtoSchema>;
export declare function useSchemaList(cultureName?: string): UseChillAsyncState<ChillDtoSchemaListItem[]>;
export declare function useText(request: GetTextRequest): UseChillAsyncState<GetTextResponse>;
export declare function useTexts(requests: GetTextRequest[]): UseChillAsyncState<Array<GetTextResponse | null>>;
export declare function useVersion(): string;
export declare function useTest(): UseChillAsyncState<string>;
export declare function useQueryMutation(): UseChillMutationState<JsonObject>;
export declare function useEntityMutation(action: "find" | "create" | "update" | "delete"): UseChillMutationState<JsonObject | null>;
export declare function useEntityChanges(chillType: string, onChanges: ChillEntityChangeCallback, guid?: string | null): UseChillSubscriptionState;
