import { useEffect, useState } from "react";
import { CHILL_SHARP_REACT_CLIENT_VERSION } from "./version.js";
import { useChillSharpClient } from "./context.js";
import type { JsonObject } from "chill-sharp-ts-client";

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

export function useSchema(
  chillType: string,
  chillViewCode = "default",
  cultureName?: string
): UseChillAsyncState<JsonObject> {
  const client = useChillSharpClient();
  const [data, setData] = useState<JsonObject | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const load = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await client.getSchema(chillType, chillViewCode, cultureName);
      setData(response);
      return response;
    } catch (err) {
      setError(err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, [client, chillType, chillViewCode, cultureName]);

  return {
    data,
    error,
    isLoading,
    reload: load
  };
}

export function useText(labelGuid: string, cultureName: string): UseChillAsyncState<JsonObject> {
  const client = useChillSharpClient();
  const [data, setData] = useState<JsonObject | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const load = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await client.getText(labelGuid, cultureName);
      setData(response);
      return response;
    } catch (err) {
      setError(err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, [client, labelGuid, cultureName]);

  return {
    data,
    error,
    isLoading,
    reload: load
  };
}

export function useVersion(): string {
  return CHILL_SHARP_REACT_CLIENT_VERSION;
}

export function useTest(): UseChillAsyncState<string> {
  const client = useChillSharpClient();
  const [data, setData] = useState<string | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const load = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await client.test();
      setData(response);
      return response;
    } catch (err) {
      setError(err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, [client]);

  return {
    data,
    error,
    isLoading,
    reload: load
  };
}

export function useQueryMutation(): UseChillMutationState<JsonObject> {
  const client = useChillSharpClient();
  const [data, setData] = useState<JsonObject | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);

  const execute = async (...args: unknown[]) => {
    const payload = args[0] as JsonObject;
    setIsLoading(true);
    setError(null);

    try {
      const response = await client.query(payload);
      setData(response);
      return response;
    } catch (err) {
      setError(err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  return {
    data,
    error,
    isLoading,
    execute,
    reset: () => {
      setData(null);
      setError(null);
      setIsLoading(false);
    }
  };
}

export function useEntityMutation(action: "find" | "create" | "update" | "delete"): UseChillMutationState<JsonObject | null> {
  const client = useChillSharpClient();
  const [data, setData] = useState<JsonObject | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);

  const execute = async (...args: unknown[]) => {
    const payload = args[0] as JsonObject;
    setIsLoading(true);
    setError(null);

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

      setData(response);
      return response;
    } catch (err) {
      setError(err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  return {
    data,
    error,
    isLoading,
    execute,
    reset: () => {
      setData(null);
      setError(null);
      setIsLoading(false);
    }
  };
}
