import React, { createContext, useContext, useRef } from "react";
import { ChillSharpClient } from "chill-sharp-ts-client";
import type { ChillSharpClientOptions } from "chill-sharp-ts-client";

export interface ChillSharpProviderProps {
  baseUrl: string;
  client?: ChillSharpClient;
  options?: ChillSharpClientOptions;
  children: React.ReactNode;
}

const ChillSharpContext = createContext<ChillSharpClient | null>(null);

export function ChillSharpProvider(props: ChillSharpProviderProps) {
  const clientRef = useRef<ChillSharpClient | null>(props.client ?? null);

  if (props.client && clientRef.current !== props.client) {
    clientRef.current = props.client;
  }

  if (!clientRef.current) {
    clientRef.current = new ChillSharpClient(props.baseUrl, props.options);
  }

  return (
    <ChillSharpContext.Provider value={clientRef.current}>
      {props.children}
    </ChillSharpContext.Provider>
  );
}

export function useChillSharpClient(): ChillSharpClient {
  const client = useContext(ChillSharpContext);
  if (!client) {
    throw new Error("useChillSharpClient must be used inside ChillSharpProvider.");
  }

  return client;
}
