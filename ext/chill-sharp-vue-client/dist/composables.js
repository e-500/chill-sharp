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
import { onScopeDispose, ref, watch, watchEffect } from "vue";
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
        data,
        error,
        isLoading,
        reload: load
    };
}
export function useSchemaList(cultureName) {
    const client = useChillSharpClient();
    const data = ref(null);
    const error = ref(null);
    const isLoading = ref(true);
    const load = async () => {
        isLoading.value = true;
        error.value = null;
        try {
            const response = await client.getSchemaList(cultureName === undefined ? undefined : readRef(cultureName));
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
        data,
        error,
        isLoading,
        reload: load
    };
}
export function useText(request) {
    const client = useChillSharpClient();
    const data = ref(null);
    const error = ref(null);
    const isLoading = ref(true);
    const load = async () => {
        isLoading.value = true;
        error.value = null;
        try {
            const response = await client.getText(readRef(request));
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
        data,
        error,
        isLoading,
        reload: load
    };
}
export function useTexts(requests) {
    const client = useChillSharpClient();
    const data = ref(null);
    const error = ref(null);
    const isLoading = ref(true);
    const load = async () => {
        isLoading.value = true;
        error.value = null;
        try {
            const response = await client.getTexts(readRef(requests));
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
        data,
        error,
        isLoading,
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
        data,
        error,
        isLoading,
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
        data,
        error,
        isLoading,
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
        data,
        error,
        isLoading,
        execute,
        reset: () => {
            data.value = null;
            error.value = null;
            isLoading.value = false;
        }
    };
}
export function useEntityChanges(chillType, onChanges, guid) {
    const client = useChillSharpClient();
    const error = ref(null);
    const isSubscribed = ref(false);
    let subscription = null;
    const release = async () => {
        const currentSubscription = subscription;
        subscription = null;
        isSubscribed.value = false;
        if (currentSubscription) {
            await currentSubscription.unsubscribe();
        }
    };
    watch([
        () => readRef(chillType),
        () => (guid === undefined ? null : (readRef(guid) ?? null)),
        () => readRef(onChanges)
    ], async ([nextChillType, nextGuid, nextOnChanges], _previousValue, onCleanup) => {
        error.value = null;
        await release();
        let isActive = true;
        onCleanup(() => {
            isActive = false;
            void release();
        });
        try {
            const nextSubscription = await client.subscribeToEntityChanges(nextChillType, async (changes) => {
                await nextOnChanges(changes);
            }, nextGuid);
            if (!isActive) {
                await nextSubscription.unsubscribe();
                return;
            }
            subscription = nextSubscription;
            isSubscribed.value = true;
        }
        catch (err) {
            if (isActive) {
                error.value = err;
                isSubscribed.value = false;
            }
        }
    }, { immediate: true });
    onScopeDispose(() => {
        void release();
    });
    return {
        error,
        isSubscribed
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
