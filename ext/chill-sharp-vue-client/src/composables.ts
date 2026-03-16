import { readonly, ref, watchEffect, type Ref } from "vue";
import { useChillSharpClient } from "./plugin.js";
import type { JsonObject } from "chill-sharp-ts-client";

export interface UseChillAsyncState<TData> {
  data: Readonly<Ref<TData | null>>;
  error: Readonly<Ref<unknown>>;
  isLoading: Readonly<Ref<boolean>>;
  reload: () => Promise<TData | null>;
}

export interface UseChillMutationState<TData> {
  data: Readonly<Ref<TData | null>>;
  error: Readonly<Ref<unknown>>;
  isLoading: Readonly<Ref<boolean>>;
  execute: (...args: unknown[]) => Promise<TData>;
  reset: () => void;
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
    data: readonly(data),
    error: readonly(error),
    isLoading: readonly(isLoading),
    reload: load
  };
}

export function useText(
  labelGuid: Ref<string> | string,
  cultureName: Ref<string> | string
): UseChillAsyncState<JsonObject> {
  const client = useChillSharpClient();
  const data = ref<JsonObject | null>(null);
  const error = ref<unknown>(null);
  const isLoading = ref<boolean>(true);

  const load = async () => {
    isLoading.value = true;
    error.value = null;

    try {
      const response = await client.getText(readRef(labelGuid), readRef(cultureName));
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
    data: readonly(data),
    error: readonly(error),
    isLoading: readonly(isLoading),
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
    data: readonly(data),
    error: readonly(error),
    isLoading: readonly(isLoading),
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
    data: readonly(data),
    error: readonly(error),
    isLoading: readonly(isLoading),
    execute,
    reset: () => {
      data.value = null;
      error.value = null;
      isLoading.value = false;
    }
  };
}

function readRef<T>(value: Ref<T> | T): T {
  if (typeof value === "object" && value !== null && "value" in value) {
    return value.value;
  }

  return value;
}
