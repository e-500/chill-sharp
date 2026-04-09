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

import type { App, InjectionKey, Plugin } from "vue";
import { inject } from "vue";
import { ChillSharpClient } from "chill-sharp-ts-client";
import type { ChillSharpClientOptions } from "chill-sharp-ts-client";

export interface ChillSharpVueOptions {
  baseUrl: string;
  client?: ChillSharpClient;
  options?: ChillSharpClientOptions;
}

export const chillSharpClientKey: InjectionKey<ChillSharpClient> = Symbol("ChillSharpClient");

export function createChillSharpClient(options: ChillSharpVueOptions): ChillSharpClient {
  if (options.client) {
    return options.client;
  }

  return new ChillSharpClient(options.baseUrl, options.options);
}

export function createChillSharpPlugin(options: ChillSharpVueOptions): Plugin {
  const client = createChillSharpClient(options);

  return {
    install(app: App) {
      app.provide(chillSharpClientKey, client);
    }
  };
}

export function useChillSharpClient(): ChillSharpClient {
  const client = inject(chillSharpClientKey, null);
  if (!client) {
    throw new Error("useChillSharpClient requires createChillSharpPlugin() to be installed.");
  }

  return client;
}
