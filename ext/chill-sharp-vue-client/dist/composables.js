import { ref, watchEffect } from "vue";
import { CHILL_SHARP_VUE_CLIENT_VERSION } from "./version.js";
import { useChillSharpClient } from "./plugin.js";
export function useSchema(chillType, chillViewCode = "default", cultureName) {
    const client = useChillSharpClient();
    const data = ref(null);
    const error = ref(null);
    const isLoading = ref(true);
    const load = async () => {
        isLoading.value = true;
        error.value = null;
        try {
            const response = await client.getSchema(readRef(chillType), readRef(chillViewCode), cultureName === undefined ? undefined : readRef(cultureName));
            data.value = response;
            return response;
        }
        catch (err) {
            error.value = err;
            throw err;
        }
        finally {
            isLoading.value = false;
        }
    };
    watchEffect(() => {
        void load();
    });
    return {
        data: asReadonlyRef(data),
        error: asReadonlyRef(error),
        isLoading: asReadonlyRef(isLoading),
        reload: load
    };
}
export function useText(labelGuid, cultureName) {
    const client = useChillSharpClient();
    const data = ref(null);
    const error = ref(null);
    const isLoading = ref(true);
    const load = async () => {
        isLoading.value = true;
        error.value = null;
        try {
            const response = await client.getText(readRef(labelGuid), readRef(cultureName));
            data.value = response;
            return response;
        }
        catch (err) {
            error.value = err;
            throw err;
        }
        finally {
            isLoading.value = false;
        }
    };
    watchEffect(() => {
        void load();
    });
    return {
        data: asReadonlyRef(data),
        error: asReadonlyRef(error),
        isLoading: asReadonlyRef(isLoading),
        reload: load
    };
}
export function useVersion() {
    return CHILL_SHARP_VUE_CLIENT_VERSION;
}
export function useTest() {
    const client = useChillSharpClient();
    const data = ref(null);
    const error = ref(null);
    const isLoading = ref(true);
    const load = async () => {
        isLoading.value = true;
        error.value = null;
        try {
            const response = await client.test();
            data.value = response;
            return response;
        }
        catch (err) {
            error.value = err;
            throw err;
        }
        finally {
            isLoading.value = false;
        }
    };
    watchEffect(() => {
        void load();
    });
    return {
        data: asReadonlyRef(data),
        error: asReadonlyRef(error),
        isLoading: asReadonlyRef(isLoading),
        reload: load
    };
}
export function useQueryMutation() {
    const client = useChillSharpClient();
    const data = ref(null);
    const error = ref(null);
    const isLoading = ref(false);
    const execute = async (...args) => {
        const payload = args[0];
        isLoading.value = true;
        error.value = null;
        try {
            const response = await client.query(payload);
            data.value = response;
            return response;
        }
        catch (err) {
            error.value = err;
            throw err;
        }
        finally {
            isLoading.value = false;
        }
    };
    return {
        data: asReadonlyRef(data),
        error: asReadonlyRef(error),
        isLoading: asReadonlyRef(isLoading),
        execute,
        reset: () => {
            data.value = null;
            error.value = null;
            isLoading.value = false;
        }
    };
}
export function useEntityMutation(action) {
    const client = useChillSharpClient();
    const data = ref(null);
    const error = ref(null);
    const isLoading = ref(false);
    const execute = async (...args) => {
        const payload = args[0];
        isLoading.value = true;
        error.value = null;
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
            data.value = response;
            return response;
        }
        catch (err) {
            error.value = err;
            throw err;
        }
        finally {
            isLoading.value = false;
        }
    };
    return {
        data: asReadonlyRef(data),
        error: asReadonlyRef(error),
        isLoading: asReadonlyRef(isLoading),
        execute,
        reset: () => {
            data.value = null;
            error.value = null;
            isLoading.value = false;
        }
    };
}
function asReadonlyRef(value) {
    return value;
}
function readRef(value) {
    if (typeof value === "object" && value !== null && "value" in value) {
        return value.value;
    }
    return value;
}
