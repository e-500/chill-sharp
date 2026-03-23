import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState
} from "@microsoft/signalr";
import { ChillSharpClientError } from "./errors.js";
import { CHILL_SHARP_TS_CLIENT_VERSION } from "./version.js";

export type JsonPrimitive = string | number | boolean | null;
export type JsonValue = JsonPrimitive | JsonObject | JsonValue[];
export interface JsonObject {
  [key: string]: JsonValue;
}

export interface GetTextRequest extends JsonObject {
  labelGuid: string;
  cultureName: string;
  primaryCultureName: string;
  primaryDefaultText: string;
  secondaryCultureName: string;
  secondaryDefaultText: string;
}

export interface GetTextResponse extends JsonObject {
  labelGuid: string;
  cultureName: string;
  value: string;
}

export interface ChillDtoPropertySchema extends JsonObject {
  name: string;
  displayName: string;
  propertyType: number;
  chillType: string | null;
}

export interface ChillDtoSchema extends JsonObject {
  chillType: string;
  chillViewCode: string;
  displayName: string;
  queryRelatedChillType: string | null;
  properties: ChillDtoPropertySchema[];
}

export interface ChillDtoSchemaListItem extends JsonObject {
  name: string;
  chillType: string;
  type: string;
  relatedChillType: string | null;
}

export interface ChillSharpClientOptions {
  accessToken?: string;
  username?: string;
  password?: string;
  cultureName?: string;
  fetchImpl?: typeof fetch;
}

export type ChillEntityChangeAction = "CREATED" | "UPDATED" | "DELETED";

export interface ChillEntityChangeNotification extends JsonObject {
  chillType: string;
  guid: string;
  action: ChillEntityChangeAction;
}

export type ChillEntityChangeCallback = (
  changes: ChillEntityChangeNotification[]
) => void | Promise<void>;

export interface ChillEntityChangeSubscription {
  chillType: string;
  guid: string | null;
  unsubscribe(): Promise<void>;
}

interface TokenState {
  accessToken: string | null;
  accessTokenIssuedUtc: Date | null;
  accessTokenExpiresUtc: Date | null;
  refreshToken: string | null;
  refreshTokenExpiresUtc: Date | null;
}

interface LocalEntityChangeSubscription {
  id: string;
  chillType: string;
  guid: string | null;
  callback: ChillEntityChangeCallback;
}

export class ChillSharpClient {
  private readonly baseUrl: string;
  private readonly fetchImpl: typeof fetch;
  private readonly cultureName: string | null;

  private username: string | null;
  private password: string | null;
  private refreshPromise: Promise<JsonObject> | null = null;
  private tokenState: TokenState;
  private notificationConnection: HubConnection | null = null;
  private readonly entityChangeSubscriptions = new Map<string, LocalEntityChangeSubscription>();
  private readonly entityChangeRegistrationCounts = new Map<string, number>();
  private entityChangeSubscriptionSequence = 0;

  constructor(baseUrl: string, options: ChillSharpClientOptions = {}) {
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

  query(dtoQuery: JsonObject): Promise<JsonObject> {
    return this.sendJson<JsonObject>("POST", this.buildChillUrl("query"), dtoQuery);
  }

  find(dtoEntity: JsonObject): Promise<JsonObject | null> {
    return this.sendJson<JsonObject | null>("POST", this.buildChillUrl("find"), dtoEntity);
  }

  create(dtoEntity: JsonObject): Promise<JsonObject> {
    return this.sendJson<JsonObject>("POST", this.buildChillUrl("create"), dtoEntity);
  }

  update(dtoEntity: JsonObject): Promise<JsonObject> {
    return this.sendJson<JsonObject>("POST", this.buildChillUrl("update"), dtoEntity);
  }

  async delete(dtoEntity: JsonObject): Promise<void> {
    await this.sendJson("POST", this.buildChillUrl("delete"), dtoEntity, false);
  }

  chunk(operations: JsonObject[]): Promise<JsonObject[]> {
    return this.sendJson<JsonObject[]>("POST", this.buildChillUrl("chunk"), operations);
  }

  version(): string {
    return CHILL_SHARP_TS_CLIENT_VERSION;
  }

  test(): Promise<string> {
    return this.sendText("GET", this.buildChillUrl("test"), true);
  }

  getSchema(chillType: string, chillViewCode: string, cultureName?: string): Promise<ChillDtoSchema | null> {
    const encodedType = encodeURIComponent(this.normalizeRequiredValue(chillType, "chillType"));
    const encodedView = encodeURIComponent(this.normalizeRequiredValue(chillViewCode, "chillViewCode"));
    const effectiveCultureName = this.normalizeOptionalValue(cultureName) ?? this.cultureName;

    let relativeUrl = `get-schema?chillType=${encodedType}&chillViewCode=${encodedView}`;
    if (effectiveCultureName) {
      relativeUrl += `&cultureName=${encodeURIComponent(effectiveCultureName)}`;
    }

    return this.sendJson<ChillDtoSchema | null>("GET", this.buildChillUrl(relativeUrl));
  }

  getSchemaList(cultureName?: string): Promise<ChillDtoSchemaListItem[]> {
    const effectiveCultureName = this.normalizeOptionalValue(cultureName) ?? this.cultureName;
    let relativeUrl = "get-schema-list";
    if (effectiveCultureName) {
      relativeUrl += `?cultureName=${encodeURIComponent(effectiveCultureName)}`;
    }

    return this.sendJson<ChillDtoSchemaListItem[]>("GET", this.buildChillUrl(relativeUrl));
  }

  setSchema(schema: ChillDtoSchema): Promise<ChillDtoSchema | null> {
    return this.sendJson<ChillDtoSchema | null>("POST", this.buildChillUrl("set-schema"), schema);
  }

  getText(request: GetTextRequest): Promise<GetTextResponse | null> {
    return this.sendJson<GetTextResponse | null>("GET", this.buildI18nUrl("get-text"), this.prepareGetTextRequest(request), true, true);
  }

  getTexts(requests: GetTextRequest[]): Promise<Array<GetTextResponse | null>> {
    if (!Array.isArray(requests)) {
      throw new Error("requests is required.");
    }

    return this.sendJson<Array<GetTextResponse | null>>(
      "GET",
      this.buildI18nUrl("get-multiple-text"),
      requests.map((request) => this.prepareGetTextRequest(request))
    );
  }

  setText(payload: JsonObject): Promise<GetTextResponse> {
    return this.sendJson<GetTextResponse>("PUT", this.buildI18nUrl("set-text"), payload);
  }

  async subscribeToEntityChanges(
    chillType: string,
    callback: ChillEntityChangeCallback,
    guid?: string | null
  ): Promise<ChillEntityChangeSubscription> {
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

  async disconnectEntityChanges(): Promise<void> {
    this.entityChangeSubscriptions.clear();
    this.entityChangeRegistrationCounts.clear();

    if (!this.notificationConnection) {
      return;
    }

    const connection = this.notificationConnection;
    this.notificationConnection = null;
    await connection.stop();
  }

  async registerAuthAccount(payload: JsonObject): Promise<JsonObject> {
    const response = await this.sendAuthJson<JsonObject>("POST", "account/register", payload, true, true);
    this.applyAuthToken(response, true);
    return response;
  }

  async loginAuthAccount(payload: JsonObject): Promise<JsonObject> {
    const response = await this.sendAuthJson<JsonObject>("POST", "account/login", payload, true, true);
    this.applyAuthToken(response, true);
    return response;
  }

  refreshAuthAccount(): Promise<JsonObject> {
    return this.getAuthTokenIfNecessary(true);
  }

  changeAuthPassword(payload: JsonObject): Promise<JsonObject> {
    return this.sendAuthJson<JsonObject>("POST", "account/change-password", payload);
  }

  requestAuthPasswordReset(payload: JsonObject): Promise<JsonObject> {
    return this.sendAuthJson<JsonObject>("POST", "account/request-password-reset", payload, true, true);
  }

  resetAuthPassword(payload: JsonObject): Promise<JsonObject> {
    return this.sendAuthJson<JsonObject>("POST", "account/reset-password", payload, true, true);
  }

  private prepareGetTextRequest(request: GetTextRequest): GetTextRequest {
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

  private sendAuthJson<T extends JsonValue | null>(
    method: string,
    relativeUrl: string,
    payload?: JsonValue,
    expectResponseBody = true,
    allowAnonymous = false
  ): Promise<T> {
    return this.sendJson<T>(method, this.buildAuthUrl(relativeUrl), payload, expectResponseBody, allowAnonymous);
  }

  private async sendJson<T extends JsonValue | null>(
    method: string,
    url: string,
    payload?: JsonValue,
    expectResponseBody = true,
    allowAnonymous = false,
    allowRetry = true
  ): Promise<T> {
    const response = await this.sendRequest(method, url, payload, allowAnonymous, allowRetry);
    if (!expectResponseBody) {
      return null as T;
    }

    const text = await response.text();
    if (!text.trim()) {
      return null as T;
    }

    return JSON.parse(text) as T;
  }

  private async sendText(
    method: string,
    url: string,
    allowAnonymous = false,
    allowRetry = true
  ): Promise<string> {
    const response = await this.sendRequest(method, url, undefined, allowAnonymous, allowRetry);
    return await response.text();
  }

  private async sendRequest(
    method: string,
    url: string,
    payload?: JsonValue,
    allowAnonymous = false,
    allowRetry = true
  ): Promise<Response> {
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
        throw new ChillSharpClientError(
          `HTTP ${response.status} calling ${method} ${url}`,
          response.status,
          await response.text()
        );
      }

      return response;
    } catch (error) {
      if (error instanceof ChillSharpClientError) {
        throw error;
      }

      throw new ChillSharpClientError(`Unexpected error executing ${method} ${url}`, undefined, undefined, error);
    }
  }

  private async getAuthTokenIfNecessary(forceRefresh = false): Promise<JsonObject> {
    if (this.refreshPromise) {
      return this.refreshPromise;
    }

    this.refreshPromise = this.getAuthTokenIfNecessaryCore(forceRefresh);
    try {
      return await this.refreshPromise;
    } finally {
      this.refreshPromise = null;
    }
  }

  private async getAuthTokenIfNecessaryCore(forceRefresh: boolean): Promise<JsonObject> {
    if (!forceRefresh && this.hasUsableAccessToken() && !this.shouldRefreshAccessToken()) {
      return this.createCurrentTokenResponse();
    }

    if (this.tokenState.refreshToken && (!forceRefresh || !this.password)) {
      try {
        const refreshed = await this.sendAuthJson<JsonObject>(
          "POST",
          "account/refresh",
          { refreshToken: this.tokenState.refreshToken },
          true,
          true
        );

        this.applyAuthToken(refreshed, true);
        return refreshed;
      } catch (error) {
        if (!(error instanceof ChillSharpClientError)) {
          throw error;
        }

        this.tokenState.refreshToken = null;
        this.tokenState.refreshTokenExpiresUtc = null;
      }
    }

    if (this.username && this.password) {
      const token = await this.sendAuthJson<JsonObject>(
        "POST",
        "account/login",
        {
          userNameOrEmail: this.username,
          password: this.password
        },
        true,
        true
      );

      this.applyAuthToken(token, true);
      return token;
    }

    if (this.hasUsableAccessToken()) {
      return this.createCurrentTokenResponse();
    }

    throw new ChillSharpClientError("No auth token is available and the client cannot obtain a new one.");
  }

  private applyAuthToken(payload: JsonObject, forgetPassword: boolean): void {
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

  private canUseAuthentication(): boolean {
    return !!(this.tokenState.accessToken || this.tokenState.refreshToken || (this.username && this.password));
  }

  private hasUsableAccessToken(): boolean {
    if (!this.tokenState.accessToken) {
      return false;
    }

    if (!this.tokenState.accessTokenExpiresUtc) {
      return true;
    }

    return new Date() < this.tokenState.accessTokenExpiresUtc;
  }

  private shouldRefreshAccessToken(): boolean {
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

  private async tryRefreshAuthentication(): Promise<boolean> {
    if (!this.tokenState.refreshToken && !this.password) {
      return false;
    }

    try {
      await this.getAuthTokenIfNecessary(true);
      return true;
    } catch (error) {
      if (error instanceof ChillSharpClientError) {
        return false;
      }

      throw error;
    }
  }

  private createCurrentTokenResponse(): JsonObject {
    return {
      accessToken: this.tokenState.accessToken ?? "",
      accessTokenIssuedUtc: this.formatDate(this.tokenState.accessTokenIssuedUtc),
      accessTokenExpiresUtc: this.formatDate(this.tokenState.accessTokenExpiresUtc),
      refreshToken: this.tokenState.refreshToken ?? "",
      refreshTokenExpiresUtc: this.formatDate(this.tokenState.refreshTokenExpiresUtc),
      userName: this.username ?? ""
    };
  }

  private buildChillUrl(relativeUrl: string): string {
    return `${this.baseUrl}/${relativeUrl.replace(/^\/+/, "")}`;
  }

  private buildNotifyUrl(): string {
    return `${this.baseUrl.replace(/\/$/, "")}/notify`;
  }

  private buildAuthUrl(relativeUrl: string): string {
    return `${this.getAuthBaseUrl().replace(/\/$/, "")}/${relativeUrl.replace(/^\/+/, "")}`;
  }

  private buildI18nUrl(relativeUrl: string): string {
    return `${this.getI18nBaseUrl().replace(/\/$/, "")}/${relativeUrl.replace(/^\/+/, "")}`;
  }

  private getAuthBaseUrl(): string {
    const suffix = "/chill";
    if (this.baseUrl.toLowerCase().endsWith(suffix)) {
      return `${this.baseUrl.slice(0, -suffix.length)}/chill-auth`;
    }

    return `${this.baseUrl.replace(/\/$/, "")}-auth`;
  }

  private getI18nBaseUrl(): string {
    const suffix = "/chill";
    if (this.baseUrl.toLowerCase().endsWith(suffix)) {
      return `${this.baseUrl.slice(0, -suffix.length)}/chill-i18n`;
    }

    return `${this.baseUrl.replace(/\/$/, "")}-i18n`;
  }

  private normalizeRequiredValue(value: string | null | undefined, argumentName: string): string {
    const normalized = this.normalizeOptionalValue(value);
    if (!normalized) {
      throw new Error(`${argumentName} is required.`);
    }

    return normalized;
  }

  private normalizeOptionalValue(value?: string | null): string | null {
    const normalized = value?.trim();
    return normalized ? normalized : null;
  }

  private readString(payload: JsonObject, key: string): string | null {
    const value = this.readValue(payload, key);
    return typeof value === "string" && value.trim() ? value.trim() : null;
  }

  private readDate(payload: JsonObject, key: string): Date | null {
    return this.parseDate(this.readValue(payload, key));
  }

  private readValue(payload: JsonObject, key: string): JsonValue | undefined {
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

  private parseDate(value: JsonValue | undefined): Date | null {
    if (typeof value !== "string" || !value.trim()) {
      return null;
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
  }

  private formatDate(value: Date | null): string {
    return value ? value.toISOString() : "";
  }

  private async ensureNotificationConnection(): Promise<HubConnection> {
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

    connection.on("EntitiesChanged", (payload: unknown) => {
      void this.dispatchEntityChangeNotifications(payload);
    });

    connection.onreconnected(async () => {
      await this.reregisterEntityChangeSubscriptions();
    });

    await connection.start();
    this.notificationConnection = connection;
    return connection;
  }

  private async unsubscribeFromEntityChanges(subscriptionId: string): Promise<void> {
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
    } else {
      this.entityChangeRegistrationCounts.set(registrationKey, registrationCount - 1);
    }
  }

  private async dispatchEntityChangeNotifications(payload: unknown): Promise<void> {
    const notifications = this.normalizeEntityChangeNotifications(payload);
    if (notifications.length === 0) {
      return;
    }

    for (const subscription of this.entityChangeSubscriptions.values()) {
      const matchingChanges = notifications.filter((change) =>
        change.chillType === subscription.chillType &&
        (!subscription.guid || change.guid === subscription.guid)
      );

      if (matchingChanges.length === 0) {
        continue;
      }

      await subscription.callback(matchingChanges);
    }
  }

  private normalizeEntityChangeNotifications(payload: unknown): ChillEntityChangeNotification[] {
    if (!Array.isArray(payload)) {
      return [];
    }

    return payload
      .filter((entry): entry is JsonObject => !!entry && typeof entry === "object" && !Array.isArray(entry))
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
        } satisfies ChillEntityChangeNotification;
      })
      .filter((entry): entry is ChillEntityChangeNotification => entry !== null);
  }

  private isEntityChangeAction(value: string | null): value is ChillEntityChangeAction {
    return value === "CREATED" || value === "UPDATED" || value === "DELETED";
  }

  private async reregisterEntityChangeSubscriptions(): Promise<void> {
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

  private buildEntityChangeRegistrationKey(chillType: string, guid: string | null): string {
    return `${chillType}|${guid ?? ""}`;
  }
}
