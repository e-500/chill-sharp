import { jsx as _jsx } from "react/jsx-runtime";
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
