import { HubConnectionBuilder, HubConnectionState } from "@microsoft/signalr";
import { ChillSharpClientError } from "./errors.js";
import { CHILL_SHARP_TS_CLIENT_VERSION } from "./version.js";
export class ChillSharpClient {
    baseUrl;
    fetchImpl;
    cultureName;
    username;
    password;
    refreshPromise = null;
    tokenState;
    notificationConnection = null;
    entityChangeSubscriptions = new Map();
    entityChangeRegistrationCounts = new Map();
    entityChangeSubscriptionSequence = 0;
    constructor(baseUrl, options = {}) {
        this.baseUrl = this.normalizeRequiredValue(baseUrl, "baseUrl").replace(/\/$/, "");
        this.fetchImpl = options.fetchImpl ?? fetch;
        this.username = this.normalizeOptionalValue(options.username);
        this.password = this.normalizeOptionalValue(options.password);
        this.cultureName = this.normalizeOptionalValue(options.cultureName);
        this.tokenState = {
            accessToken: this.normalizeOptionalValue(options.accessToken),
            accessTokenIssuedUtc: null,
            accessTokenExpiresUtc: null,
            refreshToken: null,
            refreshTokenExpiresUtc: null
        };
    }
    query(dtoQuery) {
        return this.sendJson("POST", this.buildChillUrl("query"), dtoQuery);
    }
    find(dtoEntity) {
        return this.sendJson("POST", this.buildChillUrl("find"), dtoEntity);
    }
    create(dtoEntity) {
        return this.sendJson("POST", this.buildChillUrl("create"), dtoEntity);
    }
    update(dtoEntity) {
        return this.sendJson("POST", this.buildChillUrl("update"), dtoEntity);
    }
    async delete(dtoEntity) {
        await this.sendJson("POST", this.buildChillUrl("delete"), dtoEntity, false);
    }
    chunk(operations) {
        return this.sendJson("POST", this.buildChillUrl("chunk"), operations);
    }
    version() {
        return CHILL_SHARP_TS_CLIENT_VERSION;
    }
    test() {
        return this.sendText("GET", this.buildChillUrl("test"), true);
    }
    getSchema(chillType, chillViewCode, cultureName) {
        const encodedType = encodeURIComponent(this.normalizeRequiredValue(chillType, "chillType"));
        const encodedView = encodeURIComponent(this.normalizeRequiredValue(chillViewCode, "chillViewCode"));
        const effectiveCultureName = this.normalizeOptionalValue(cultureName) ?? this.cultureName;
        let relativeUrl = `get-schema?chillType=${encodedType}&chillViewCode=${encodedView}`;
        if (effectiveCultureName) {
            relativeUrl += `&cultureName=${encodeURIComponent(effectiveCultureName)}`;
        }
        return this.sendJson("GET", this.buildSchemaUrl(relativeUrl));
    }
    getSchemaList(cultureName) {
        const effectiveCultureName = this.normalizeOptionalValue(cultureName) ?? this.cultureName;
        let relativeUrl = "get-schema-list";
        if (effectiveCultureName) {
            relativeUrl += `?cultureName=${encodeURIComponent(effectiveCultureName)}`;
        }
        return this.sendJson("GET", this.buildSchemaUrl(relativeUrl));
    }
    setSchema(schema) {
        return this.sendJson("POST", this.buildSchemaUrl("set-schema"), schema);
    }
    getEntityOptions(chillType) {
        const encodedType = encodeURIComponent(this.normalizeRequiredValue(chillType, "chillType"));
        return this.sendJson("GET", this.buildSchemaUrl(`get-entity-options?chillType=${encodedType}`));
    }
    setEntityOptions(entityOptions) {
        return this.sendJson("POST", this.buildSchemaUrl("set-entity-options"), entityOptions);
    }
    getText(request) {
        return this.sendJson("GET", this.buildI18nUrl("get-text"), this.prepareGetTextRequest(request), true, true);
    }
    getTexts(requests) {
        if (!Array.isArray(requests)) {
            throw new Error("requests is required.");
        }
        return this.sendJson("GET", this.buildI18nUrl("get-multiple-text"), requests.map((request) => this.prepareGetTextRequest(request)));
    }
    setText(payload) {
        return this.sendJson("PUT", this.buildI18nUrl("set-text"), payload);
    }
    async subscribeToEntityChanges(chillType, callback, guid) {
        if (typeof callback !== "function") {
            throw new Error("callback is required.");
        }
        const normalizedChillType = this.normalizeRequiredValue(chillType, "chillType");
        const normalizedGuid = this.normalizeOptionalValue(guid);
        const connection = await this.ensureNotificationConnection();
        const registrationKey = this.buildEntityChangeRegistrationKey(normalizedChillType, normalizedGuid);
        const registrationCount = this.entityChangeRegistrationCounts.get(registrationKey) ?? 0;
        if (registrationCount === 0) {
            await connection.invoke("Register", normalizedChillType, normalizedGuid);
        }
        this.entityChangeRegistrationCounts.set(registrationKey, registrationCount + 1);
        const subscriptionId = `entity-change-${++this.entityChangeSubscriptionSequence}`;
        this.entityChangeSubscriptions.set(subscriptionId, {
            id: subscriptionId,
            chillType: normalizedChillType,
            guid: normalizedGuid,
            callback
        });
        return {
            chillType: normalizedChillType,
            guid: normalizedGuid,
            unsubscribe: async () => {
                await this.unsubscribeFromEntityChanges(subscriptionId);
            }
        };
    }
    async disconnectEntityChanges() {
        this.entityChangeSubscriptions.clear();
        this.entityChangeRegistrationCounts.clear();
        if (!this.notificationConnection) {
            return;
        }
        const connection = this.notificationConnection;
        this.notificationConnection = null;
        await connection.stop();
    }
    async registerAuthAccount(payload) {
        const response = await this.sendAuthJson("POST", "account/register", payload, true, true);
        this.applyAuthToken(response, true);
        return response;
    }
    async loginAuthAccount(payload) {
        const response = await this.sendAuthJson("POST", "account/login", payload, true, true);
        this.applyAuthToken(response, true);
        return response;
    }
    refreshAuthAccount() {
        return this.getAuthTokenIfNecessary(true);
    }
    changeAuthPassword(payload) {
        return this.sendAuthJson("POST", "account/change-password", payload);
    }
    requestAuthPasswordReset(payload) {
        return this.sendAuthJson("POST", "account/request-password-reset", payload, true, true);
    }
    resetAuthPassword(payload) {
        return this.sendAuthJson("POST", "account/reset-password", payload, true, true);
    }
    getAuthPermissions() {
        return this.sendAuthJson("GET", "get-permissions");
    }
    getAuthUserList() {
        return this.sendAuthJson("GET", "get-user-list");
    }
    getAuthUser(userGuid) {
        const normalizedUserGuid = this.normalizeRequiredValue(userGuid, "userGuid");
        return this.sendAuthJson("GET", `get-user?userGuid=${encodeURIComponent(normalizedUserGuid)}`);
    }
    setAuthUser(payload) {
        return this.sendAuthJson("POST", "set-user", payload);
    }
    getAuthRoleList() {
        return this.sendAuthJson("GET", "get-role-list");
    }
    getAuthRole(roleGuid) {
        const normalizedRoleGuid = this.normalizeRequiredValue(roleGuid, "roleGuid");
        return this.sendAuthJson("GET", `get-role?roleGuid=${encodeURIComponent(normalizedRoleGuid)}`);
    }
    setAuthRole(payload) {
        return this.sendAuthJson("POST", "set-role", payload);
    }
    prepareGetTextRequest(request) {
        if (!request || typeof request !== "object") {
            throw new Error("request is required.");
        }
        const effectiveCultureName = this.normalizeOptionalValue(this.readString(request, "cultureName")) ?? this.cultureName;
        if (!effectiveCultureName) {
            throw new Error("cultureName is required.");
        }
        return {
            labelGuid: this.normalizeRequiredValue(this.readString(request, "labelGuid"), "labelGuid"),
            cultureName: effectiveCultureName,
            primaryCultureName: this.readString(request, "primaryCultureName") ?? "",
            primaryDefaultText: this.readString(request, "primaryDefaultText") ?? "",
            secondaryCultureName: this.readString(request, "secondaryCultureName") ?? "",
            secondaryDefaultText: this.readString(request, "secondaryDefaultText") ?? ""
        };
    }
    sendAuthJson(method, relativeUrl, payload, expectResponseBody = true, allowAnonymous = false) {
        return this.sendJson(method, this.buildAuthUrl(relativeUrl), payload, expectResponseBody, allowAnonymous);
    }
    async sendJson(method, url, payload, expectResponseBody = true, allowAnonymous = false, allowRetry = true) {
        const response = await this.sendRequest(method, url, payload, allowAnonymous, allowRetry);
        if (!expectResponseBody) {
            return null;
        }
        const text = await response.text();
        if (!text.trim()) {
            return null;
        }
        return JSON.parse(text);
    }
    async sendText(method, url, allowAnonymous = false, allowRetry = true) {
        const response = await this.sendRequest(method, url, undefined, allowAnonymous, allowRetry);
        return await response.text();
    }
    async sendRequest(method, url, payload, allowAnonymous = false, allowRetry = true) {
        try {
            if (!allowAnonymous && this.canUseAuthentication()) {
                await this.getAuthTokenIfNecessary();
            }
            const headers = new Headers();
            if (!allowAnonymous && this.tokenState.accessToken) {
                headers.set("Authorization", `Bearer ${this.tokenState.accessToken}`);
            }
            if (payload !== undefined) {
                headers.set("Content-Type", "application/json");
            }
            const response = await this.fetchImpl(url, {
                method,
                headers,
                body: payload === undefined ? undefined : JSON.stringify(payload)
            });
            if ((response.status === 401 || response.status === 403) && !allowAnonymous && allowRetry && await this.tryRefreshAuthentication()) {
                return this.sendRequest(method, url, payload, allowAnonymous, false);
            }
            if (!response.ok) {
                throw new ChillSharpClientError(`HTTP ${response.status} calling ${method} ${url}`, response.status, await response.text());
            }
            return response;
        }
        catch (error) {
            if (error instanceof ChillSharpClientError) {
                throw error;
            }
            throw new ChillSharpClientError(`Unexpected error executing ${method} ${url}`, undefined, undefined, error);
        }
    }
    async getAuthTokenIfNecessary(forceRefresh = false) {
        if (this.refreshPromise) {
            return this.refreshPromise;
        }
        this.refreshPromise = this.getAuthTokenIfNecessaryCore(forceRefresh);
        try {
            return await this.refreshPromise;
        }
        finally {
            this.refreshPromise = null;
        }
    }
    async getAuthTokenIfNecessaryCore(forceRefresh) {
        if (!forceRefresh && this.hasUsableAccessToken() && !this.shouldRefreshAccessToken()) {
            return this.createCurrentTokenResponse();
        }
        if (this.tokenState.refreshToken && (!forceRefresh || !this.password)) {
            try {
                const refreshed = await this.sendAuthJson("POST", "account/refresh", { refreshToken: this.tokenState.refreshToken }, true, true);
                this.applyAuthToken(refreshed, true);
                return refreshed;
            }
            catch (error) {
                if (!(error instanceof ChillSharpClientError)) {
                    throw error;
                }
                this.tokenState.refreshToken = null;
                this.tokenState.refreshTokenExpiresUtc = null;
            }
        }
        if (this.username && this.password) {
            const token = await this.sendAuthJson("POST", "account/login", {
                userNameOrEmail: this.username,
                password: this.password
            }, true, true);
            this.applyAuthToken(token, true);
            return token;
        }
        if (this.hasUsableAccessToken()) {
            return this.createCurrentTokenResponse();
        }
        throw new ChillSharpClientError("No auth token is available and the client cannot obtain a new one.");
    }
    applyAuthToken(payload, forgetPassword) {
        this.tokenState.accessToken = this.readString(payload, "accessToken");
        this.tokenState.accessTokenIssuedUtc = this.readDate(payload, "accessTokenIssuedUtc");
        this.tokenState.accessTokenExpiresUtc = this.readDate(payload, "accessTokenExpiresUtc");
        this.tokenState.refreshToken = this.readString(payload, "refreshToken");
        this.tokenState.refreshTokenExpiresUtc = this.readDate(payload, "refreshTokenExpiresUtc");
        const userName = this.readString(payload, "userName");
        if (userName) {
            this.username = userName;
        }
        if (forgetPassword) {
            this.password = null;
        }
    }
    canUseAuthentication() {
        return !!(this.tokenState.accessToken || this.tokenState.refreshToken || (this.username && this.password));
    }
    hasUsableAccessToken() {
        if (!this.tokenState.accessToken) {
            return false;
        }
        if (!this.tokenState.accessTokenExpiresUtc) {
            return true;
        }
        return new Date() < this.tokenState.accessTokenExpiresUtc;
    }
    shouldRefreshAccessToken() {
        const issued = this.tokenState.accessTokenIssuedUtc;
        const expires = this.tokenState.accessTokenExpiresUtc;
        if (!issued || !expires) {
            return false;
        }
        if (expires <= issued) {
            return true;
        }
        const refreshThreshold = new Date(issued.getTime() + (expires.getTime() - issued.getTime()) * 0.75);
        return new Date() >= refreshThreshold;
    }
    async tryRefreshAuthentication() {
        if (!this.tokenState.refreshToken && !this.password) {
            return false;
        }
        try {
            await this.getAuthTokenIfNecessary(true);
            return true;
        }
        catch (error) {
            if (error instanceof ChillSharpClientError) {
                return false;
            }
            throw error;
        }
    }
    createCurrentTokenResponse() {
        return {
            accessToken: this.tokenState.accessToken ?? "",
            accessTokenIssuedUtc: this.formatDate(this.tokenState.accessTokenIssuedUtc),
            accessTokenExpiresUtc: this.formatDate(this.tokenState.accessTokenExpiresUtc),
            refreshToken: this.tokenState.refreshToken ?? "",
            refreshTokenExpiresUtc: this.formatDate(this.tokenState.refreshTokenExpiresUtc),
            userName: this.username ?? ""
        };
    }
    buildChillUrl(relativeUrl) {
        return `${this.baseUrl}/${relativeUrl.replace(/^\/+/, "")}`;
    }
    buildNotifyUrl() {
        return `${this.baseUrl.replace(/\/$/, "")}/notify`;
    }
    buildAuthUrl(relativeUrl) {
        return `${this.getAuthBaseUrl().replace(/\/$/, "")}/${relativeUrl.replace(/^\/+/, "")}`;
    }
    buildSchemaUrl(relativeUrl) {
        return `${this.getSchemaBaseUrl().replace(/\/$/, "")}/${relativeUrl.replace(/^\/+/, "")}`;
    }
    buildI18nUrl(relativeUrl) {
        return `${this.getI18nBaseUrl().replace(/\/$/, "")}/${relativeUrl.replace(/^\/+/, "")}`;
    }
    getAuthBaseUrl() {
        const suffix = "/chill";
        if (this.baseUrl.toLowerCase().endsWith(suffix)) {
            return `${this.baseUrl.slice(0, -suffix.length)}/chill-auth`;
        }
        return `${this.baseUrl.replace(/\/$/, "")}-auth`;
    }
    getSchemaBaseUrl() {
        const suffix = "/chill";
        if (this.baseUrl.toLowerCase().endsWith(suffix)) {
            return `${this.baseUrl.slice(0, -suffix.length)}/chill-schema`;
        }
        return `${this.baseUrl.replace(/\/$/, "")}-schema`;
    }
    getI18nBaseUrl() {
        const suffix = "/chill";
        if (this.baseUrl.toLowerCase().endsWith(suffix)) {
            return `${this.baseUrl.slice(0, -suffix.length)}/chill-i18n`;
        }
        return `${this.baseUrl.replace(/\/$/, "")}-i18n`;
    }
    normalizeRequiredValue(value, argumentName) {
        const normalized = this.normalizeOptionalValue(value);
        if (!normalized) {
            throw new Error(`${argumentName} is required.`);
        }
        return normalized;
    }
    normalizeOptionalValue(value) {
        const normalized = value?.trim();
        return normalized ? normalized : null;
    }
    readString(payload, key) {
        const value = this.readValue(payload, key);
        return typeof value === "string" && value.trim() ? value.trim() : null;
    }
    readDate(payload, key) {
        return this.parseDate(this.readValue(payload, key));
    }
    readValue(payload, key) {
        if (key in payload) {
            return payload[key];
        }
        const pascalKey = key.length > 1
            ? `${key[0].toUpperCase()}${key.slice(1)}`
            : key.toUpperCase();
        if (pascalKey in payload) {
            return payload[pascalKey];
        }
        const matchedKey = Object.keys(payload).find((candidate) => candidate.toLowerCase() === key.toLowerCase());
        return matchedKey ? payload[matchedKey] : undefined;
    }
    parseDate(value) {
        if (typeof value !== "string" || !value.trim()) {
            return null;
        }
        const parsed = new Date(value);
        return Number.isNaN(parsed.getTime()) ? null : parsed;
    }
    formatDate(value) {
        return value ? value.toISOString() : "";
    }
    async ensureNotificationConnection() {
        if (this.notificationConnection) {
            if (this.notificationConnection.state === HubConnectionState.Disconnected) {
                await this.notificationConnection.start();
            }
            return this.notificationConnection;
        }
        const connection = new HubConnectionBuilder()
            .withUrl(this.buildNotifyUrl(), {
            accessTokenFactory: async () => {
                if (this.canUseAuthentication()) {
                    await this.getAuthTokenIfNecessary();
                }
                return this.tokenState.accessToken ?? "";
            }
        })
            .withAutomaticReconnect()
            .build();
        connection.on("EntitiesChanged", (payload) => {
            void this.dispatchEntityChangeNotifications(payload);
        });
        connection.onreconnected(async () => {
            await this.reregisterEntityChangeSubscriptions();
        });
        await connection.start();
        this.notificationConnection = connection;
        return connection;
    }
    async unsubscribeFromEntityChanges(subscriptionId) {
        const subscription = this.entityChangeSubscriptions.get(subscriptionId);
        if (!subscription) {
            return;
        }
        this.entityChangeSubscriptions.delete(subscriptionId);
        const registrationKey = this.buildEntityChangeRegistrationKey(subscription.chillType, subscription.guid);
        const registrationCount = this.entityChangeRegistrationCounts.get(registrationKey) ?? 0;
        if (registrationCount <= 1) {
            this.entityChangeRegistrationCounts.delete(registrationKey);
            const connection = this.notificationConnection;
            if (connection && connection.state === HubConnectionState.Connected) {
                await connection.invoke("Unregister", subscription.chillType, subscription.guid);
            }
        }
        else {
            this.entityChangeRegistrationCounts.set(registrationKey, registrationCount - 1);
        }
    }
    async dispatchEntityChangeNotifications(payload) {
        const notifications = this.normalizeEntityChangeNotifications(payload);
        if (notifications.length === 0) {
            return;
        }
        for (const subscription of this.entityChangeSubscriptions.values()) {
            const matchingChanges = notifications.filter((change) => change.chillType === subscription.chillType &&
                (!subscription.guid || change.guid === subscription.guid));
            if (matchingChanges.length === 0) {
                continue;
            }
            await subscription.callback(matchingChanges);
        }
    }
    normalizeEntityChangeNotifications(payload) {
        if (!Array.isArray(payload)) {
            return [];
        }
        return payload
            .filter((entry) => !!entry && typeof entry === "object" && !Array.isArray(entry))
            .map((entry) => {
            const chillType = this.readString(entry, "chillType");
            const guid = this.readString(entry, "guid");
            const action = this.readString(entry, "action");
            if (!chillType || !guid || !this.isEntityChangeAction(action)) {
                return null;
            }
            return {
                chillType,
                guid,
                action
            };
        })
            .filter((entry) => entry !== null);
    }
    isEntityChangeAction(value) {
        return value === "CREATED" || value === "UPDATED" || value === "DELETED";
    }
    async reregisterEntityChangeSubscriptions() {
        const connection = this.notificationConnection;
        if (!connection || connection.state !== HubConnectionState.Connected) {
            return;
        }
        for (const registrationKey of this.entityChangeRegistrationCounts.keys()) {
            const separatorIndex = registrationKey.indexOf("|");
            const chillType = separatorIndex >= 0 ? registrationKey.slice(0, separatorIndex) : registrationKey;
            const guid = separatorIndex >= 0 ? registrationKey.slice(separatorIndex + 1) : "";
            await connection.invoke("Register", chillType, guid || null);
        }
    }
    buildEntityChangeRegistrationKey(chillType, guid) {
        return `${chillType}|${guid ?? ""}`;
    }
}
