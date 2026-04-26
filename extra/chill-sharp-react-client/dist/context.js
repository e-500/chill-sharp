import { jsx as _jsx } from "react/jsx-runtime";
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
import { createContext, useContext, useRef } from "react";
import { ChillSharpClient } from "chill-sharp-ts-client";
const ChillSharpContext = createContext(null);
export function ChillSharpProvider(props) {
    const clientRef = useRef(props.client ?? null);
    if (props.client && clientRef.current !== props.client) {
        clientRef.current = props.client;
    }
    if (!clientRef.current) {
        clientRef.current = new ChillSharpClient(props.baseUrl, props.options);
    }
    return (_jsx(ChillSharpContext.Provider, { value: clientRef.current, children: props.children }));
}
export function useChillSharpClient() {
    const client = useContext(ChillSharpContext);
    if (!client) {
        throw new Error("useChillSharpClient must be used inside ChillSharpProvider.");
    }
    return client;
}
