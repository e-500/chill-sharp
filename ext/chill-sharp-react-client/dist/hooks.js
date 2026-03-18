import { useEffect, useState } from "react";
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
export function useText(labelGuid, cultureName) {
    const client = useChillSharpClient();
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const load = async () => {
        setIsLoading(true);
        setError(null);
        try {
            const response = await client.getText(labelGuid, cultureName);
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
    }, [client, labelGuid, cultureName]);
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
