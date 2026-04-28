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
import { inject } from "vue";
import { ChillSharpClient } from "@chill-sharp/ts-client";
export const chillSharpClientKey = Symbol("ChillSharpClient");
export function createChillSharpClient(options) {
    if (options.client) {
        return options.client;
    }
    return new ChillSharpClient(options.baseUrl, options.options);
}
export function createChillSharpPlugin(options) {
    const client = createChillSharpClient(options);
    return {
        install(app) {
            app.provide(chillSharpClientKey, client);
        }
    };
}
export function useChillSharpClient() {
    const client = inject(chillSharpClientKey, null);
    if (!client) {
        throw new Error("useChillSharpClient requires createChillSharpPlugin() to be installed.");
    }
    return client;
}
