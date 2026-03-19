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
        return this.sendJson("GET", this.buildChillUrl(relativeUrl));
    }
    getSchemaList(cultureName) {
        const effectiveCultureName = this.normalizeOptionalValue(cultureName) ?? this.cultureName;
        let relativeUrl = "get-schema-list";
        if (effectiveCultureName) {
            relativeUrl += `?cultureName=${encodeURIComponent(effectiveCultureName)}`;
        }
        return this.sendJson("GET", this.buildChillUrl(relativeUrl));
    }
    setSchema(schema) {
        return this.sendJson("POST", this.buildChillUrl("set-schema"), schema);
    }
    getText(request) {
        return this.sendJson("POST", this.buildI18nUrl("text/get"), this.prepareGetTextRequest(request), true, true);
    }
    getTexts(requests) {
        if (!Array.isArray(requests)) {
            throw new Error("requests is required.");
        }
        return this.sendJson("POST", this.buildI18nUrl("text/get-multiple"), requests.map((request) => this.prepareGetTextRequest(request)));
    }
    setText(payload) {
        return this.sendJson("PUT", this.buildI18nUrl("text"), payload);
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
    buildAuthUrl(relativeUrl) {
        return `${this.getAuthBaseUrl().replace(/\/$/, "")}/${relativeUrl.replace(/^\/+/, "")}`;
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
}
