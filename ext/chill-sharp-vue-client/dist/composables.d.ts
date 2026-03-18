import { type Ref } from "vue";
import type { JsonObject } from "chill-sharp-ts-client";
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
export declare function useSchema(chillType: Ref<string> | string, chillViewCode?: Ref<string> | string, cultureName?: Ref<string | undefined> | string): UseChillAsyncState<JsonObject>;
export declare function useText(labelGuid: Ref<string> | string, cultureName: Ref<string> | string): UseChillAsyncState<JsonObject>;
export declare function useVersion(): string;
export declare function useTest(): UseChillAsyncState<string>;
export declare function useQueryMutation(): UseChillMutationState<JsonObject>;
export declare function useEntityMutation(action: "find" | "create" | "update" | "delete"): UseChillMutationState<JsonObject | null>;
type ReadonlyRef<T> = Ref<T>;
export {};
