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
import { useEffect, useRef, useState } from "react";
import { CHILL_SHARP_REACT_CLIENT_VERSION } from "./version.js";
import { useChillSharpClient } from "./context.js";
export function useSchema(chillType, chillViewCode = "default", cultureName) {
    const client = useChillSharpClient();
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const load = async () => {
        setIsLoading(true);
        setError(null);
        try {
            const response = await client.getSchema(chillType, chillViewCode, cultureName);
            setData(response);
            return response;
        }
        catch (err) {
            setError(err);
            throw err;
        }
        finally {
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
export function useSchemaList(cultureName) {
    const client = useChillSharpClient();
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const load = async () => {
        setIsLoading(true);
        setError(null);
        try {
            const response = await client.getSchemaList(cultureName);
            setData(response);
            return response;
        }
        catch (err) {
            setError(err);
            throw err;
        }
        finally {
            setIsLoading(false);
        }
    };
    useEffect(() => {
        void load();
    }, [client, cultureName]);
    return {
        data,
        error,
        isLoading,
        reload: load
    };
}
export function useText(request) {
    const client = useChillSharpClient();
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const load = async () => {
        setIsLoading(true);
        setError(null);
        try {
            const response = await client.getText(request);
            setData(response);
            return response;
        }
        catch (err) {
            setError(err);
            throw err;
        }
        finally {
            setIsLoading(false);
        }
    };
    useEffect(() => {
        void load();
    }, [client, request]);
    return {
        data,
        error,
        isLoading,
        reload: load
    };
}
export function useTexts(requests) {
    const client = useChillSharpClient();
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const load = async () => {
        setIsLoading(true);
        setError(null);
        try {
            const response = await client.getTexts(requests);
            setData(response);
            return response;
        }
        catch (err) {
            setError(err);
            throw err;
        }
        finally {
            setIsLoading(false);
        }
    };
    useEffect(() => {
        void load();
    }, [client, requests]);
    return {
        data,
        error,
        isLoading,
        reload: load
    };
}
export function useVersion() {
    return CHILL_SHARP_REACT_CLIENT_VERSION;
}
export function useTest() {
    const client = useChillSharpClient();
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const load = async () => {
        setIsLoading(true);
        setError(null);
        try {
            const response = await client.test();
            setData(response);
            return response;
        }
        catch (err) {
            setError(err);
            throw err;
        }
        finally {
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
export function useQueryMutation() {
    const client = useChillSharpClient();
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const [isLoading, setIsLoading] = useState(false);
    const execute = async (...args) => {
        const payload = args[0];
        setIsLoading(true);
        setError(null);
        try {
            const response = await client.query(payload);
            setData(response);
            return response;
        }
        catch (err) {
            setError(err);
            throw err;
        }
        finally {
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
export function useLookupMutation() {
    const client = useChillSharpClient();
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const [isLoading, setIsLoading] = useState(false);
    const execute = async (...args) => {
        const payload = args[0];
        setIsLoading(true);
        setError(null);
        try {
            const response = await client.lookup(payload);
            setData(response);
            return response;
        }
        catch (err) {
            setError(err);
            throw err;
        }
        finally {
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
export function useAutocompleteMutation() {
    const client = useChillSharpClient();
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const [isLoading, setIsLoading] = useState(false);
    const execute = async (...args) => {
        const payload = args[0];
        setIsLoading(true);
        setError(null);
        try {
            const response = await client.autocomplete(payload);
            setData(response);
            return response;
        }
        catch (err) {
            setError(err);
            throw err;
        }
        finally {
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
export function useValidateMutation() {
    const client = useChillSharpClient();
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const [isLoading, setIsLoading] = useState(false);
    const execute = async (...args) => {
        const payload = args[0];
        setIsLoading(true);
        setError(null);
        try {
            const response = await client.validate(payload);
            setData(response);
            return response;
        }
        catch (err) {
            setError(err);
            throw err;
        }
        finally {
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
export function useEntityMutation(action) {
    const client = useChillSharpClient();
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const [isLoading, setIsLoading] = useState(false);
    const execute = async (...args) => {
        const payload = args[0];
        setIsLoading(true);
        setError(null);
        try {
            let response;
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
        }
        catch (err) {
            setError(err);
            throw err;
        }
        finally {
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
export function useEntityChanges(chillType, onChanges, guid) {
    const client = useChillSharpClient();
    const onChangesRef = useRef(onChanges);
    const unsubscribeRef = useRef(null);
    const [error, setError] = useState(null);
    const [isSubscribed, setIsSubscribed] = useState(false);
    useEffect(() => {
        onChangesRef.current = onChanges;
    }, [onChanges]);
    useEffect(() => {
        let isDisposed = false;
        setError(null);
        setIsSubscribed(false);
        unsubscribeRef.current = null;
        void client
            .subscribeToEntityChanges(chillType, async (changes) => {
            await onChangesRef.current(changes);
        }, guid)
            .then((subscription) => {
            if (isDisposed) {
                void subscription.unsubscribe();
                return;
            }
            unsubscribeRef.current = () => subscription.unsubscribe();
            setIsSubscribed(true);
        })
            .catch((err) => {
            if (!isDisposed) {
                setError(err);
                setIsSubscribed(false);
            }
        });
        return () => {
            isDisposed = true;
            setIsSubscribed(false);
            if (unsubscribeRef.current) {
                void unsubscribeRef.current();
                unsubscribeRef.current = null;
            }
        };
    }, [client, chillType, guid]);
    return {
        error,
        isSubscribed
    };
}
