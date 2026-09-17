/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core 
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

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
