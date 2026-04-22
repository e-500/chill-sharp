import { type Ref } from "vue";
import type { ChillEntityChangeCallback, ChillValidationError, JsonObject } from "chill-sharp-ts-client";
export interface UseChillAsyncState<TData> {
    data: ReadonlyRef<TData | null>;
    error: ReadonlyRef<unknown>;
    isLoading: ReadonlyRef<boolean>;
    reload: () => Promise<TData | null>;
}
export interface UseChillMutationState<TData> {
    data: ReadonlyRef<TData | null>;
    error: ReadonlyRef<unknown>;
    isLoading: ReadonlyRef<boolean>;
    execute: (...args: unknown[]) => Promise<TData>;
    reset: () => void;
}
export interface UseChillSubscriptionState {
    error: ReadonlyRef<unknown>;
    isSubscribed: ReadonlyRef<boolean>;
}
export declare function useSchema(chillType: Ref<string> | string, chillViewCode?: Ref<string> | string, cultureName?: Ref<string | undefined> | string, update?: Ref<boolean | undefined> | boolean): UseChillAsyncState<JsonObject>;
export declare function useSchemaList(cultureName?: Ref<string | undefined> | string): UseChillAsyncState<JsonObject[]>;
export declare function useText(request: Ref<JsonObject> | JsonObject): UseChillAsyncState<JsonObject>;
export declare function useTexts(requests: Ref<JsonObject[]> | JsonObject[]): UseChillAsyncState<Array<JsonObject | null>>;
export declare function useVersion(): string;
export declare function useTest(): UseChillAsyncState<string>;
export declare function useQueryMutation(): UseChillMutationState<JsonObject>;
export declare function useLookupMutation(): UseChillMutationState<JsonObject>;
export declare function useAutocompleteMutation(): UseChillMutationState<JsonObject>;
export declare function useValidateMutation(): UseChillMutationState<ChillValidationError[]>;
export declare function useEntityMutation(action: "find" | "create" | "update" | "delete"): UseChillMutationState<JsonObject | null>;
export declare function useEntityChanges(chillType: Ref<string> | string, onChanges: Ref<ChillEntityChangeCallback> | ChillEntityChangeCallback, guid?: Ref<string | null | undefined> | string | null): UseChillSubscriptionState;
type ReadonlyRef<T> = Ref<T>;
export {};
