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
export interface ChillDtoEntityOptions extends JsonObject {
    chillType: string;
    checksumEnabled: boolean;
    labelFormatString: string | null;
    shortLabelFormatString: string | null;
    fullTextContentFormatString: string | null;
    changeLogEnabled: boolean;
}
export interface AuthUserListItem extends JsonObject {
    guid: string;
    externalId: string;
    userName: string;
    displayName: string;
    isActive: boolean;
    canManagePermissions: boolean;
    canManageSchema: boolean;
}
export interface AuthRoleListItem extends JsonObject {
    guid: string;
    name: string;
    description: string;
    isActive: boolean;
}
export interface AuthPermissionRule extends JsonObject {
    guid: string;
    effect: number;
    action: number;
    scope: number;
    module: string;
    entityName: string | null;
    propertyName: string | null;
    appliesToAllProperties: boolean;
    description: string;
    createdUtc: string;
}
export interface AuthRolePermissions extends AuthRoleListItem {
    permissions: AuthPermissionRule[];
}
export interface GetAuthPermissionsResponse extends JsonObject {
    user: AuthUserListItem | null;
    permissions: AuthPermissionRule[];
    roles: AuthRolePermissions[];
}
export interface AuthUserDetailsResponse extends AuthUserListItem {
    roles: AuthRoleListItem[];
    permissions: AuthPermissionRule[];
}
export interface AuthRoleDetailsResponse extends AuthRoleListItem {
    users: AuthUserListItem[];
    permissions: AuthPermissionRule[];
}
export interface AuthPermissionRuleItem extends JsonObject {
    guid: string | null;
    effect: number;
    action: number;
    scope: number;
    module: string;
    entityName: string | null;
    propertyName: string | null;
    appliesToAllProperties: boolean;
    description: string;
}
export interface SetAuthUserRequest extends JsonObject {
    guid: string | null;
    externalId: string;
    userName: string;
    displayName: string;
    isActive: boolean;
    canManagePermissions: boolean;
    canManageSchema: boolean;
    roleGuids: string[];
    permissions: AuthPermissionRuleItem[];
}
export interface SetAuthRoleRequest extends JsonObject {
    guid: string | null;
    name: string;
    description: string;
    isActive: boolean;
    userGuids: string[];
    permissions: AuthPermissionRuleItem[];
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
export type ChillEntityChangeCallback = (changes: ChillEntityChangeNotification[]) => void | Promise<void>;
export interface ChillEntityChangeSubscription {
    chillType: string;
    guid: string | null;
    unsubscribe(): Promise<void>;
}
export declare class ChillSharpClient {
    private readonly baseUrl;
    private readonly fetchImpl;
    private readonly cultureName;
    private username;
    private password;
    private refreshPromise;
    private tokenState;
    private notificationConnection;
    private readonly entityChangeSubscriptions;
    private readonly entityChangeRegistrationCounts;
    private entityChangeSubscriptionSequence;
    constructor(baseUrl: string, options?: ChillSharpClientOptions);
    query(dtoQuery: JsonObject): Promise<JsonObject>;
    find(dtoEntity: JsonObject): Promise<JsonObject | null>;
    create(dtoEntity: JsonObject): Promise<JsonObject>;
    update(dtoEntity: JsonObject): Promise<JsonObject>;
    delete(dtoEntity: JsonObject): Promise<void>;
    chunk(operations: JsonObject[]): Promise<JsonObject[]>;
    version(): string;
    test(): Promise<string>;
    getSchema(chillType: string, chillViewCode: string, cultureName?: string): Promise<ChillDtoSchema | null>;
    getSchemaList(cultureName?: string): Promise<ChillDtoSchemaListItem[]>;
    setSchema(schema: ChillDtoSchema): Promise<ChillDtoSchema | null>;
    getEntityOptions(chillType: string): Promise<ChillDtoEntityOptions>;
    setEntityOptions(entityOptions: ChillDtoEntityOptions): Promise<ChillDtoEntityOptions>;
    getText(request: GetTextRequest): Promise<GetTextResponse | null>;
    getTexts(requests: GetTextRequest[]): Promise<Array<GetTextResponse | null>>;
    setText(payload: JsonObject): Promise<GetTextResponse>;
    subscribeToEntityChanges(chillType: string, callback: ChillEntityChangeCallback, guid?: string | null): Promise<ChillEntityChangeSubscription>;
    disconnectEntityChanges(): Promise<void>;
    registerAuthAccount(payload: JsonObject): Promise<JsonObject>;
    loginAuthAccount(payload: JsonObject): Promise<JsonObject>;
    refreshAuthAccount(): Promise<JsonObject>;
    changeAuthPassword(payload: JsonObject): Promise<JsonObject>;
    requestAuthPasswordReset(payload: JsonObject): Promise<JsonObject>;
    resetAuthPassword(payload: JsonObject): Promise<JsonObject>;
    getAuthPermissions(): Promise<GetAuthPermissionsResponse>;
    getAuthUserList(): Promise<AuthUserListItem[]>;
    getAuthUser(userGuid: string): Promise<AuthUserDetailsResponse>;
    setAuthUser(payload: SetAuthUserRequest): Promise<AuthUserDetailsResponse>;
    getAuthRoleList(): Promise<AuthRoleListItem[]>;
    getAuthRole(roleGuid: string): Promise<AuthRoleDetailsResponse>;
    setAuthRole(payload: SetAuthRoleRequest): Promise<AuthRoleDetailsResponse>;
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
    private buildNotifyUrl;
    private buildAuthUrl;
    private buildSchemaUrl;
    private buildI18nUrl;
    private getAuthBaseUrl;
    private getSchemaBaseUrl;
    private getI18nBaseUrl;
    private normalizeRequiredValue;
    private normalizeOptionalValue;
    private readString;
    private readDate;
    private readValue;
    private parseDate;
    private formatDate;
    private ensureNotificationConnection;
    private unsubscribeFromEntityChanges;
    private dispatchEntityChangeNotifications;
    private normalizeEntityChangeNotifications;
    private isEntityChangeAction;
    private reregisterEntityChangeSubscriptions;
    private buildEntityChangeRegistrationKey;
}
