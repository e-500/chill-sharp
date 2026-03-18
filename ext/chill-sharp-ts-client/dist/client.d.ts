export type JsonPrimitive = string | number | boolean | null;
export type JsonValue = JsonPrimitive | JsonObject | JsonValue[];
export interface JsonObject {
    [key: string]: JsonValue;
}
export interface GetTextRequest extends JsonObject {
    LabelGuid: string;
    CultureName: string;
    PrimaryCultureName: string;
    PrimaryDefaultText: string;
    SecondaryCultureName: string;
    SecondaryDefaultText: string;
}
export interface GetTextResponse extends JsonObject {
    LabelGuid: string;
    CultureName: string;
    Value: string;
}
export interface ChillSharpClientOptions {
    accessToken?: string;
    username?: string;
    password?: string;
    cultureName?: string;
    fetchImpl?: typeof fetch;
}
export declare class ChillSharpClient {
    private readonly baseUrl;
    private readonly fetchImpl;
    private readonly cultureName;
    private username;
    private password;
    private refreshPromise;
    private tokenState;
    constructor(baseUrl: string, options?: ChillSharpClientOptions);
    query(dtoQuery: JsonObject): Promise<JsonObject>;
    find(dtoEntity: JsonObject): Promise<JsonObject | null>;
    create(dtoEntity: JsonObject): Promise<JsonObject>;
    update(dtoEntity: JsonObject): Promise<JsonObject>;
    delete(dtoEntity: JsonObject): Promise<void>;
    chunk(operations: JsonObject[]): Promise<JsonObject[]>;
    version(): string;
    test(): Promise<string>;
    getSchema(chillType: string, chillViewCode: string, cultureName?: string): Promise<JsonObject | null>;
    setSchema(schema: JsonObject): Promise<JsonObject | null>;
    getText(request: GetTextRequest): Promise<GetTextResponse | null>;
    getTexts(requests: GetTextRequest[]): Promise<Array<GetTextResponse | null>>;
    setText(payload: JsonObject): Promise<GetTextResponse>;
    registerAuthAccount(payload: JsonObject): Promise<JsonObject>;
    loginAuthAccount(payload: JsonObject): Promise<JsonObject>;
    refreshAuthAccount(): Promise<JsonObject>;
    changeAuthPassword(payload: JsonObject): Promise<JsonObject>;
    requestAuthPasswordReset(payload: JsonObject): Promise<JsonObject>;
    resetAuthPassword(payload: JsonObject): Promise<JsonObject>;
    private prepareGetTextRequest;
    private sendAuthJson;
    private sendJson;
    private sendText;
    private sendRequest;
    private getAuthTokenIfNecessary;
    private getAuthTokenIfNecessaryCore;
    private applyAuthToken;
    private canUseAuthentication;
    private hasUsableAccessToken;
    private shouldRefreshAccessToken;
    private tryRefreshAuthentication;
    private createCurrentTokenResponse;
    private buildChillUrl;
    private buildAuthUrl;
    private buildI18nUrl;
    private getAuthBaseUrl;
    private getI18nBaseUrl;
    private normalizeRequiredValue;
    private normalizeOptionalValue;
    private readString;
    private parseDate;
    private formatDate;
}
