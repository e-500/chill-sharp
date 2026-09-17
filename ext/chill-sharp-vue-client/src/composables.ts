import { onScopeDispose, ref, watch, watchEffect, type Ref } from "vue";
import { CHILL_SHARP_VUE_CLIENT_VERSION } from "./version.js";
import { useChillSharpClient } from "./plugin.js";
import type {
  ChillDtoSchemaListItem,
  ChillEntityChangeCallback,
  ChillEntityChangeSubscription,
  GetTextRequest,
  JsonObject
} from "chill-sharp-ts-client";

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

export function useSchema(
  chillType: Ref<string> | string,
  chillViewCode: Ref<string> | string = "default",
  cultureName?: Ref<string | undefined> | string
): UseChillAsyncState<JsonObject> {
  const client = useChillSharpClient();
  const data = ref<JsonObject | null>(null);
  const error = ref<unknown>(null);
  const isLoading = ref<boolean>(true);

  const load = async () => {
    isLoading.value = true;
    error.value = null;

    try {
      const response = await client.getSchema(
        readRef(chillType),
        readRef(chillViewCode),
        cultureName === undefined ? undefined : readRef(cultureName)
      );
      data.value = response;
      return response;
    } catch (err) {
      error.value = err;
      throw err;
    } finally {
      isLoading.value = false;
    }
  };

  watchEffect(() => {
    void load();
  });

  return {
    data,
    error,
    isLoading,
    reload: load
  };
}

export function useSchemaList(
  cultureName?: Ref<string | undefined> | string
): UseChillAsyncState<JsonObject[]> {
  const client = useChillSharpClient();
  const data = ref<JsonObject[] | null>(null);
  const error = ref<unknown>(null);
  const isLoading = ref<boolean>(true);

  const load = async () => {
    isLoading.value = true;
    error.value = null;

    try {
      const response = await client.getSchemaList(cultureName === undefined ? undefined : readRef(cultureName));
      data.value = response;
      return response;
    } catch (err) {
      error.value = err;
      throw err;
    } finally {
      isLoading.value = false;
    }
  };

  watchEffect(() => {
    void load();
  });

  return {
    data,
    error,
    isLoading,
    reload: load
  };
}

export function useText(request: Ref<JsonObject> | JsonObject): UseChillAsyncState<JsonObject> {
  const client = useChillSharpClient();
  const data = ref<JsonObject | null>(null);
  const error = ref<unknown>(null);
  const isLoading = ref<boolean>(true);

  const load = async () => {
    isLoading.value = true;
    error.value = null;

    try {
      const response = await client.getText(readRef(request) as GetTextRequest);
      data.value = response;
      return response;
    } catch (err) {
      error.value = err;
      throw err;
    } finally {
      isLoading.value = false;
    }
  };

  watchEffect(() => {
    void load();
  });

  return {
    data,
    error,
    isLoading,
    reload: load
  };
}

export function useTexts(requests: Ref<JsonObject[]> | JsonObject[]): UseChillAsyncState<Array<JsonObject | null>> {
  const client = useChillSharpClient();
  const data = ref<Array<JsonObject | null> | null>(null);
  const error = ref<unknown>(null);
  const isLoading = ref<boolean>(true);

  const load = async () => {
    isLoading.value = true;
    error.value = null;

    try {
      const response = await client.getTexts(readRef(requests) as GetTextRequest[]) as Array<JsonObject | null>;
      data.value = response;
      return response;
    } catch (err) {
      error.value = err;
      throw err;
    } finally {
      isLoading.value = false;
    }
  };

  watchEffect(() => {
    void load();
  });

  return {
    data,
    error,
    isLoading,
    reload: load
  };
}

export function useVersion(): string {
  return CHILL_SHARP_VUE_CLIENT_VERSION;
}

export function useTest(): UseChillAsyncState<string> {
  const client = useChillSharpClient();
  const data = ref<string | null>(null);
  const error = ref<unknown>(null);
  const isLoading = ref<boolean>(true);

  const load = async () => {
    isLoading.value = true;
    error.value = null;

    try {
      const response = await client.test();
      data.value = response;
      return response;
    } catch (err) {
      error.value = err;
      throw err;
    } finally {
      isLoading.value = false;
    }
  };

  watchEffect(() => {
    void load();
  });

  return {
    data,
    error,
    isLoading,
    reload: load
  };
}

export function useQueryMutation(): UseChillMutationState<JsonObject> {
  const client = useChillSharpClient();
  const data = ref<JsonObject | null>(null);
  const error = ref<unknown>(null);
  const isLoading = ref<boolean>(false);

  const execute = async (...args: unknown[]) => {
    const payload = args[0] as JsonObject;
    isLoading.value = true;
    error.value = null;

    try {
      const response = await client.query(payload);
      data.value = response;
      return response;
    } catch (err) {
      error.value = err;
      throw err;
    } finally {
      isLoading.value = false;
    }
  };

  return {
    data,
    error,
    isLoading,
    execute,
    reset: () => {
      data.value = null;
      error.value = null;
      isLoading.value = false;
    }
  };
}

export function useEntityMutation(action: "find" | "create" | "update" | "delete"): UseChillMutationState<JsonObject | null> {
  const client = useChillSharpClient();
  const data = ref<JsonObject | null>(null);
  const error = ref<unknown>(null);
  const isLoading = ref<boolean>(false);

  const execute = async (...args: unknown[]) => {
    const payload = args[0] as JsonObject;
    isLoading.value = true;
    error.value = null;

    try {
      let response: JsonObject | null;
      switch (action) {
        case "find":
          response = await client.find(payload);
          break;
        case "create":
          response = await client.create(payload);
          break;
        case "update":
          response = await client.update(payload);
          break;
        default:
          await client.delete(payload);
          response = null;
          break;
      }

      data.value = response;
      return response;
    } catch (err) {
      error.value = err;
      throw err;
    } finally {
      isLoading.value = false;
    }
  };

  return {
    data,
    error,
    isLoading,
    execute,
    reset: () => {
      data.value = null;
      error.value = null;
      isLoading.value = false;
    }
  };
}

export function useEntityChanges(
  chillType: Ref<string> | string,
  onChanges: Ref<ChillEntityChangeCallback> | ChillEntityChangeCallback,
  guid?: Ref<string | null | undefined> | string | null
): UseChillSubscriptionState {
  const client = useChillSharpClient();
  const error = ref<unknown>(null);
  const isSubscribed = ref<boolean>(false);
  let subscription: ChillEntityChangeSubscription | null = null;

  const release = async () => {
    const currentSubscription = subscription;
    subscription = null;
    isSubscribed.value = false;
    if (currentSubscription) {
      await currentSubscription.unsubscribe();
    }
  };

  watch(
    [
      () => readRef(chillType),
      () => (guid === undefined ? null : (readRef(guid) ?? null)),
      () => readRef(onChanges)
    ],
    async ([nextChillType, nextGuid, nextOnChanges], _previousValue, onCleanup) => {
      error.value = null;
      await release();

      let isActive = true;
      onCleanup(() => {
        isActive = false;
        void release();
      });

      try {
        const nextSubscription = await client.subscribeToEntityChanges(
          nextChillType,
          async (changes) => {
            await nextOnChanges(changes);
          },
          nextGuid
        );

        if (!isActive) {
          await nextSubscription.unsubscribe();
          return;
        }

        subscription = nextSubscription;
        isSubscribed.value = true;
      } catch (err) {
        if (isActive) {
          error.value = err;
          isSubscribed.value = false;
        }
      }
    },
    { immediate: true }
  );

  onScopeDispose(() => {
    void release();
  });

  return {
    error,
    isSubscribed
  };
}

type ReadonlyRef<T> = Ref<T>;

function asReadonlyRef<T>(value: Ref<T>): ReadonlyRef<T> {
  return value;
}

function readRef<T>(value: Ref<T> | T): T {
  if (typeof value === "object" && value !== null && "value" in value) {
    return value.value;
  }

  return value;
}


