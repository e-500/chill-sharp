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
export declare const ChillDtoPropertyType: {
    readonly Unknown: 0;
    readonly Guid: 1;
    readonly Integer: 10;
    readonly Decimal: 20;
    readonly Date: 30;
    readonly Time: 40;
    readonly DateTime: 50;
    readonly Duration: 60;
    readonly Boolean: 70;
    readonly String: 80;
    readonly Text: 81;
    readonly Json: 99;
    readonly ChillEntity: 1000;
    readonly ChillEntityCollection: 1010;
    readonly ChillQuery: 1100;
};
export type ChillDtoPropertyType = (typeof ChillDtoPropertyType)[keyof typeof ChillDtoPropertyType];
export interface ChillDtoPropertySchema extends JsonObject {
    name: string;
    displayName: string;
    propertyType: ChillDtoPropertyType;
    referenceChillType: string | null;
    referenceChillTypeQuery: string | null;
    metadata: Record<string, string>;
}
export interface ChillDtoSchema extends JsonObject {
    chillType: string;
    chillViewCode: string;
    displayName: string;
    metadata: Record<string, string>;
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
export interface ChillDtoMenuItem extends JsonObject {
    guid: string;
    title: string;
    description: string | null;
    parent: ChillDtoMenuItem | null;
    componentName: string;
    componentConfigurationJson: string | null;
    menuHierarchy: string;
}
export interface ChillValidationError extends JsonObject {
    fieldName: string | null;
    message: string | null;
}
export interface AuthUserListItem extends JsonObject {
    guid: string;
    externalId: string;
    userName: string;
    displayName: string;
    displayCultureName: string;
    displayTimeZone: string;
    displayDateFormat: string;
    displayNumberFormat: string;
    isActive: boolean;
    canManagePermissions: boolean;
    canManageSchema: boolean;
    menuHierarchy: string;
}
export interface AuthRoleListItem extends JsonObject {
    guid: string;
    name: string;
    description: string;
    isActive: boolean;
    menuHierarchy: string;
}
export interface AuthTokenResponse extends JsonObject {
    accessToken: string;
    accessTokenIssuedUtc: string;
    accessTokenExpiresUtc: string;
    refreshToken: string;
    refreshTokenExpiresUtc: string;
    userId: string;
    userName: string;
}
export interface RegisterAuthIdentityRequest extends JsonObject {
    userName: string;
    email: string | null;
    password: string;
    displayName: string;
    displayCultureName: string;
    createChillAuthUser: boolean;
}
export interface LoginAuthIdentityRequest extends JsonObject {
    userNameOrEmail: string;
    password: string;
}
export interface RefreshAuthTokenRequest extends JsonObject {
    refreshToken: string;
}
export interface ChangePasswordRequest extends JsonObject {
    currentPassword: string;
    newPassword: string;
}
export interface ChangePasswordResponse extends JsonObject {
    succeeded: boolean;
}
export interface RequestPasswordResetRequest extends JsonObject {
    userNameOrEmail: string;
}
export interface PasswordResetTokenResponse extends JsonObject {
    isAccepted: boolean;
    userId: string | null;
    resetToken: string | null;
}
export interface ResetPasswordRequest extends JsonObject {
    userId: string;
    resetToken: string;
    newPassword: string;
}
export interface ResetPasswordResponse extends JsonObject {
    succeeded: boolean;
}
export declare const PermissionEffect: {
    readonly Allow: 1;
    readonly Deny: 2;
};
export type PermissionEffect = (typeof PermissionEffect)[keyof typeof PermissionEffect];
export declare const PermissionAction: {
    readonly FullControl: 0;
    readonly Query: 1;
    readonly Create: 2;
    readonly Update: 3;
    readonly Delete: 4;
    readonly See: 5;
    readonly Modify: 6;
};
export type PermissionAction = (typeof PermissionAction)[keyof typeof PermissionAction];
export declare const PermissionScope: {
    readonly Module: 1;
    readonly Entity: 2;
    readonly Property: 3;
};
export type PermissionScope = (typeof PermissionScope)[keyof typeof PermissionScope];
export interface AuthPermissionRule extends JsonObject {
    guid: string;
    userGuid: string | null;
    roleGuid: string | null;
    effect: PermissionEffect;
    action: PermissionAction;
    scope: PermissionScope;
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
    effect: PermissionEffect;
    action: PermissionAction;
    scope: PermissionScope;
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
    displayCultureName: string;
    displayTimeZone: string;
    displayDateFormat: string;
    displayNumberFormat: string;
    isActive: boolean;
    canManagePermissions: boolean;
    canManageSchema: boolean;
    menuHierarchy: string;
    roleGuids: string[];
    permissions: AuthPermissionRuleItem[];
}
export interface CreateAuthUserRequest extends JsonObject {
    externalId: string;
    email: string;
    userName: string;
    displayName: string;
    displayCultureName: string;
    displayTimeZone: string;
    displayDateFormat: string;
    displayNumberFormat: string;
    isActive: boolean;
    canManagePermissions: boolean;
    canManageSchema: boolean;
    menuHierarchy: string;
}
export interface UpdateAuthUserRequest extends JsonObject {
    externalId: string;
    userName: string;
    displayName: string;
    displayCultureName: string;
    displayTimeZone: string;
    displayDateFormat: string;
    displayNumberFormat: string;
    isActive: boolean;
    canManagePermissions: boolean;
    canManageSchema: boolean;
    menuHierarchy: string;
}
export interface SetAuthRoleRequest extends JsonObject {
    guid: string | null;
    name: string;
    description: string;
    isActive: boolean;
    menuHierarchy: string;
    userGuids: string[];
    permissions: AuthPermissionRuleItem[];
}
export interface CreateAuthRoleRequest extends JsonObject {
    name: string;
    description: string;
    isActive: boolean;
    menuHierarchy: string;
}
export interface UpdateAuthRoleRequest extends CreateAuthRoleRequest {
}
export interface CreateAuthPermissionRuleRequest extends JsonObject {
    userGuid: string | null;
    roleGuid: string | null;
    effect: PermissionEffect;
    action: PermissionAction;
    scope: PermissionScope;
    module: string;
    entityName: string | null;
    propertyName: string | null;
    appliesToAllProperties: boolean;
    description: string;
}
export interface UpdateAuthPermissionRuleRequest extends CreateAuthPermissionRuleRequest {
}
export interface ChillSharpClientOptions {
    accessToken?: string;
    username?: string;
    password?: string;
    cultureName?: string;
    fetchImpl?: typeof fetch;
    signalRWithCredentials?: boolean;
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
    private readonly signalRWithCredentials;
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
    lookup(dtoQuery: JsonObject): Promise<JsonObject>;
    find(dtoEntity: JsonObject): Promise<JsonObject | null>;
    create(dtoEntity: JsonObject): Promise<JsonObject>;
    update(dtoEntity: JsonObject): Promise<JsonObject>;
    delete(dtoEntity: JsonObject): Promise<void>;
    autocomplete(dto: JsonObject): Promise<JsonObject>;
    validate(dto: JsonObject): Promise<ChillValidationError[]>;
    chunk(operations: JsonObject[]): Promise<JsonObject[]>;
    version(): string;
    test(): Promise<string>;
    getSchema(chillType: string, chillViewCode: string, cultureName?: string): Promise<ChillDtoSchema | null>;
    getSchemaList(cultureName?: string): Promise<ChillDtoSchemaListItem[]>;
    setSchema(schema: ChillDtoSchema): Promise<ChillDtoSchema | null>;
    getEntityOptions(chillType: string): Promise<ChillDtoEntityOptions>;
    setEntityOptions(entityOptions: ChillDtoEntityOptions): Promise<ChillDtoEntityOptions>;
    getMenu(parentGuid?: string | null): Promise<ChillDtoMenuItem[]>;
    setMenu(menuItem: ChillDtoMenuItem): Promise<ChillDtoMenuItem>;
    deleteMenu(menuItemGuid: string): Promise<void>;
    getText(request: GetTextRequest): Promise<GetTextResponse | null>;
    getTexts(requests: GetTextRequest[]): Promise<Array<GetTextResponse | null>>;
    setText(payload: JsonObject): Promise<GetTextResponse>;
    subscribeToEntityChanges(chillType: string, callback: ChillEntityChangeCallback, guid?: string | null): Promise<ChillEntityChangeSubscription>;
    disconnectEntityChanges(): Promise<void>;
    registerAuthAccount(payload: RegisterAuthIdentityRequest): Promise<AuthTokenResponse>;
    loginAuthAccount(payload: LoginAuthIdentityRequest): Promise<AuthTokenResponse>;
    refreshAuthAccount(): Promise<AuthTokenResponse>;
    logoutAuthAccount(): Promise<void>;
    changeAuthPassword(payload: ChangePasswordRequest): Promise<ChangePasswordResponse>;
    requestAuthPasswordReset(payload: RequestPasswordResetRequest): Promise<PasswordResetTokenResponse>;
    resetAuthPassword(payload: ResetPasswordRequest): Promise<ResetPasswordResponse>;
    getAuthPermissions(): Promise<GetAuthPermissionsResponse>;
    getAuthUserList(): Promise<AuthUserListItem[]>;
    getAuthUser(userGuid: string): Promise<AuthUserDetailsResponse>;
    setAuthUser(payload: SetAuthUserRequest): Promise<AuthUserDetailsResponse>;
    getAuthRoleList(): Promise<AuthRoleListItem[]>;
    getAuthModuleList(): Promise<string[]>;
    getAuthEntityList(module?: string | null): Promise<string[]>;
    getAuthQueryList(module?: string | null): Promise<string[]>;
    getAuthModuleEntityList(module?: string | null): Promise<string[]>;
    getAuthPropertyList(chillType: string): Promise<string[]>;
    getAuthRole(roleGuid: string): Promise<AuthRoleDetailsResponse>;
    setAuthRole(payload: SetAuthRoleRequest): Promise<AuthRoleDetailsResponse>;
    getAuthUsers(): Promise<AuthUserListItem[]>;
    createAuthUser(payload: CreateAuthUserRequest): Promise<AuthUserListItem>;
    updateAuthUser(userGuid: string, payload: UpdateAuthUserRequest): Promise<AuthUserListItem | null>;
    deleteAuthUser(userGuid: string): Promise<void>;
    getAuthUserRoles(userGuid: string): Promise<AuthRoleListItem[]>;
    assignAuthRole(userGuid: string, roleGuid: string): Promise<void>;
    removeAuthRole(userGuid: string, roleGuid: string): Promise<void>;
    getAuthRoles(): Promise<AuthRoleListItem[]>;
    createAuthRole(payload: CreateAuthRoleRequest): Promise<AuthRoleListItem>;
    updateAuthRole(roleGuid: string, payload: UpdateAuthRoleRequest): Promise<AuthRoleListItem | null>;
    deleteAuthRole(roleGuid: string): Promise<void>;
    getAuthPermissionRules(userGuid?: string | null, roleGuid?: string | null): Promise<AuthPermissionRule[]>;
    getAuthPermissionRule(ruleGuid: string): Promise<AuthPermissionRule | null>;
    createAuthPermissionRule(payload: CreateAuthPermissionRuleRequest): Promise<AuthPermissionRule>;
    updateAuthPermissionRule(ruleGuid: string, payload: UpdateAuthPermissionRuleRequest): Promise<AuthPermissionRule | null>;
    deleteAuthPermissionRule(ruleGuid: string): Promise<void>;
    private prepareGetTextRequest;
    private sendAuthJson;
    private sendJson;
    private sendText;
    private sendRequest;
    private getAuthTokenIfNecessary;
    private getAuthTokenIfNecessaryCore;
    private applyAuthToken;
    private clearAuthToken;
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
    private normalizeQueryValue;
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
    private getUsersAssignedToRole;
    private syncUserRoles;
    private syncUserPermissions;
    private syncRoleUsers;
    private syncRolePermissions;
    private syncPermissionRules;
}
