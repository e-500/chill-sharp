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


export const ChillDtoPropertyType = {
  Unknown: 0,
  Guid: 1,
  Integer: 10,
  Decimal: 20,
  Date: 30,
  Time: 40,
  DateTime: 50,
  Duration: 60,
  Boolean: 70,
  String: 80,
  Text: 81,
  Json: 99,
  ChillEntity: 1000,
  ChillEntityCollection: 1010,
  ChillQuery: 1100
} as const;

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
  positionNo: number;
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

export const PermissionEffect = {
  Allow: 1,
  Deny: 2
} as const;

export type PermissionEffect = (typeof PermissionEffect)[keyof typeof PermissionEffect];

export const PermissionAction = {
  FullControl: 0,
  Query: 1,
  Create: 2,
  Update: 3,
  Delete: 4,
  See: 5,
  Modify: 6
} as const;

export type PermissionAction = (typeof PermissionAction)[keyof typeof PermissionAction];

export const PermissionScope = {
  Module: 1,
  Entity: 2,
  Property: 3
} as const;

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

export interface UpdateAuthRoleRequest extends CreateAuthRoleRequest {}

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

export interface UpdateAuthPermissionRuleRequest extends CreateAuthPermissionRuleRequest {}

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
  private readonly signalRWithCredentials: boolean;

  private username: string | null;
  private password: string | null;
  private refreshPromise: Promise<AuthTokenResponse> | null = null;
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
    this.signalRWithCredentials = options.signalRWithCredentials ?? true;
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

  lookup(dtoQuery: JsonObject): Promise<JsonObject> {
    return this.sendJson<JsonObject>("POST", this.buildChillUrl("lookup"), dtoQuery);
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

  autocomplete(dto: JsonObject): Promise<JsonObject> {
    return this.sendJson<JsonObject>("POST", this.buildChillUrl("autocomplete"), dto);
  }

  validate(dto: JsonObject): Promise<ChillValidationError[]> {
    return this.sendJson<ChillValidationError[]>("POST", this.buildChillUrl("validate"), dto);
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

    return this.sendJson<ChillDtoSchema | null>("GET", this.buildSchemaUrl(relativeUrl));
  }

  getSchemaList(cultureName?: string): Promise<ChillDtoSchemaListItem[]> {
    const effectiveCultureName = this.normalizeOptionalValue(cultureName) ?? this.cultureName;
    let relativeUrl = "get-schema-list";
    if (effectiveCultureName) {
      relativeUrl += `?cultureName=${encodeURIComponent(effectiveCultureName)}`;
    }

    return this.sendJson<ChillDtoSchemaListItem[]>("GET", this.buildSchemaUrl(relativeUrl));
  }

  setSchema(schema: ChillDtoSchema): Promise<ChillDtoSchema | null> {
    return this.sendJson<ChillDtoSchema | null>("POST", this.buildSchemaUrl("set-schema"), schema);
  }

  getEntityOptions(chillType: string): Promise<ChillDtoEntityOptions> {
    const encodedType = encodeURIComponent(this.normalizeRequiredValue(chillType, "chillType"));
    return this.sendJson<ChillDtoEntityOptions>("GET", this.buildSchemaUrl(`get-entity-options?chillType=${encodedType}`));
  }

  setEntityOptions(entityOptions: ChillDtoEntityOptions): Promise<ChillDtoEntityOptions> {
    return this.sendJson<ChillDtoEntityOptions>("POST", this.buildSchemaUrl("set-entity-options"), entityOptions);
  }

  getMenu(parentGuid?: string | null): Promise<ChillDtoMenuItem[]> {
    const normalizedParentGuid = this.normalizeQueryValue(parentGuid);
    const suffix = normalizedParentGuid === null ? "" : `?parentGuid=${encodeURIComponent(normalizedParentGuid)}`;
    return this.sendJson<ChillDtoMenuItem[]>("GET", this.buildSchemaUrl(`get-menu${suffix}`));
  }

  setMenu(menuItem: ChillDtoMenuItem): Promise<ChillDtoMenuItem> {
    return this.sendJson<ChillDtoMenuItem>("POST", this.buildSchemaUrl("set-menu"), menuItem);
  }


  async deleteMenu(menuItemGuid: string): Promise<void> {
    const normalizedMenuItemGuid = this.normalizeRequiredValue(menuItemGuid, "menuItemGuid");
    await this.sendJson("DELETE", this.buildSchemaUrl(`delete-menu?menuItemGuid=${encodeURIComponent(normalizedMenuItemGuid)}`), undefined, false);
  }
  getText(request: GetTextRequest): Promise<GetTextResponse | null> {
    return this.sendJson<GetTextResponse | null>("POST", this.buildI18nUrl("get-text"), this.prepareGetTextRequest(request), true, true);
  }

  getTexts(requests: GetTextRequest[]): Promise<Array<GetTextResponse | null>> {
    if (!Array.isArray(requests)) {
      throw new Error("requests is required.");
    }

    return this.sendJson<Array<GetTextResponse | null>>(
      "POST",
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

  async registerAuthAccount(payload: RegisterAuthIdentityRequest): Promise<AuthTokenResponse> {
    const response = await this.sendAuthJson<AuthTokenResponse>("POST", "register", payload, true, true);
    this.applyAuthToken(response, true);
    return response;
  }

  async loginAuthAccount(payload: LoginAuthIdentityRequest): Promise<AuthTokenResponse> {
    const response = await this.sendAuthJson<AuthTokenResponse>("POST", "login", payload, true, true);
    this.applyAuthToken(response, true);
    return response;
  }

  refreshAuthAccount(): Promise<AuthTokenResponse> {
    return this.getAuthTokenIfNecessary(true);
  }

  async logoutAuthAccount(): Promise<void> {
    await this.sendAuthJson("POST", "logout", undefined, false);
    this.clearAuthToken();
  }

  changeAuthPassword(payload: ChangePasswordRequest): Promise<ChangePasswordResponse> {
    return this.sendAuthJson<ChangePasswordResponse>("POST", "change-password", payload);
  }

  requestAuthPasswordReset(payload: RequestPasswordResetRequest): Promise<PasswordResetTokenResponse> {
    return this.sendAuthJson<PasswordResetTokenResponse>("POST", "request-password-reset", payload, true, true);
  }

  resetAuthPassword(payload: ResetPasswordRequest): Promise<ResetPasswordResponse> {
    return this.sendAuthJson<ResetPasswordResponse>("POST", "reset-password", payload, true, true);
  }

  getAuthPermissions(): Promise<GetAuthPermissionsResponse> {
    return this.sendAuthJson<GetAuthPermissionsResponse>("GET", "get-permissions");
  }

  getAuthUserList(): Promise<AuthUserListItem[]> {
    return this.sendAuthJson<AuthUserListItem[]>("GET", "get-user-list");
  }

  async getAuthUser(userGuid: string): Promise<AuthUserDetailsResponse> {
    const normalizedUserGuid = this.normalizeRequiredValue(userGuid, "userGuid");
    const [user, roles, permissions] = await Promise.all([
      this.sendAuthJson<AuthUserListItem>("GET", `users/${encodeURIComponent(normalizedUserGuid)}`),
      this.getAuthUserRoles(normalizedUserGuid),
      this.getAuthPermissionRules(normalizedUserGuid, null)
    ]);

    return {
      ...user,
      roles,
      permissions
    };
  }

  async setAuthUser(payload: SetAuthUserRequest): Promise<AuthUserDetailsResponse> {
    const userGuid = this.normalizeOptionalValue(payload.guid);
    const basePayload = {
      externalId: payload.externalId,
      userName: payload.userName,
      displayName: payload.displayName,
      displayCultureName: payload.displayCultureName,
      displayTimeZone: payload.displayTimeZone,
      displayDateFormat: payload.displayDateFormat,
      displayNumberFormat: payload.displayNumberFormat,
      isActive: payload.isActive,
      canManagePermissions: payload.canManagePermissions,
      canManageSchema: payload.canManageSchema,
      menuHierarchy: payload.menuHierarchy
    };

    const user = userGuid
      ? await this.updateAuthUser(userGuid, basePayload)
      : await this.createAuthUser({
          ...basePayload,
          email: "",
          externalId: payload.externalId
        });

    if (!user) {
      throw new ChillSharpClientError("Auth user was not found after setAuthUser execution.");
    }

    await this.syncUserRoles(user.guid, payload.roleGuids);
    await this.syncUserPermissions(user.guid, payload.permissions);
    return this.getAuthUser(user.guid);
  }

  getAuthRoleList(): Promise<AuthRoleListItem[]> {
    return this.sendAuthJson<AuthRoleListItem[]>("GET", "get-role-list");
  }

  getAuthModuleList(): Promise<string[]> {
    return this.sendAuthJson<string[]>("GET", "get-module-list");
  }

  getAuthEntityList(module?: string | null): Promise<string[]> {
    const normalizedModule = this.normalizeQueryValue(module);
    const suffix = normalizedModule === null ? "" : `?module=${encodeURIComponent(normalizedModule)}`;
    return this.sendAuthJson<string[]>("GET", `get-entity-list${suffix}`);
  }

  getAuthQueryList(module?: string | null): Promise<string[]> {
    const normalizedModule = this.normalizeQueryValue(module);
    const suffix = normalizedModule === null ? "" : `?module=${encodeURIComponent(normalizedModule)}`;
    return this.sendAuthJson<string[]>("GET", `get-query-list${suffix}`);
  }

  getAuthModuleEntityList(module?: string | null): Promise<string[]> {
    return this.getAuthEntityList(module);
  }


  getAuthPropertyList(chillType: string): Promise<string[]> {
    const normalizedChillType = this.normalizeRequiredValue(chillType, "chillType");
    return this.sendAuthJson<string[]>("GET", `get-property-list?chillType=${encodeURIComponent(normalizedChillType)}`);
  }

  async getAuthRole(roleGuid: string): Promise<AuthRoleDetailsResponse> {
    const normalizedRoleGuid = this.normalizeRequiredValue(roleGuid, "roleGuid");
    const [role, permissions, users] = await Promise.all([
      this.sendAuthJson<AuthRoleListItem>("GET", `roles/${encodeURIComponent(normalizedRoleGuid)}`),
      this.getAuthPermissionRules(null, normalizedRoleGuid),
      this.getUsersAssignedToRole(normalizedRoleGuid)
    ]);

    return {
      ...role,
      users,
      permissions
    };
  }

  async setAuthRole(payload: SetAuthRoleRequest): Promise<AuthRoleDetailsResponse> {
    const roleGuid = this.normalizeOptionalValue(payload.guid);
    const basePayload = {
      name: payload.name,
      description: payload.description,
      isActive: payload.isActive,
      menuHierarchy: payload.menuHierarchy
    };

    const role = roleGuid
      ? await this.updateAuthRole(roleGuid, basePayload)
      : await this.createAuthRole(basePayload);

    if (!role) {
      throw new ChillSharpClientError("Auth role was not found after setAuthRole execution.");
    }

    await this.syncRoleUsers(role.guid, payload.userGuids);
    await this.syncRolePermissions(role.guid, payload.permissions);
    return this.getAuthRole(role.guid);
  }

  getAuthUsers(): Promise<AuthUserListItem[]> {
    return this.sendAuthJson<AuthUserListItem[]>("GET", "users");
  }

  createAuthUser(payload: CreateAuthUserRequest): Promise<AuthUserListItem> {
    return this.sendAuthJson<AuthUserListItem>("POST", "users", payload);
  }

  updateAuthUser(userGuid: string, payload: UpdateAuthUserRequest): Promise<AuthUserListItem | null> {
    const normalizedUserGuid = this.normalizeRequiredValue(userGuid, "userGuid");
    return this.sendAuthJson<AuthUserListItem | null>("PUT", `users/${encodeURIComponent(normalizedUserGuid)}`, payload);
  }

  async deleteAuthUser(userGuid: string): Promise<void> {
    const normalizedUserGuid = this.normalizeRequiredValue(userGuid, "userGuid");
    await this.sendAuthJson("DELETE", `users/${encodeURIComponent(normalizedUserGuid)}`, undefined, false);
  }

  getAuthUserRoles(userGuid: string): Promise<AuthRoleListItem[]> {
    const normalizedUserGuid = this.normalizeRequiredValue(userGuid, "userGuid");
    return this.sendAuthJson<AuthRoleListItem[]>("GET", `users/${encodeURIComponent(normalizedUserGuid)}/roles`);
  }

  async assignAuthRole(userGuid: string, roleGuid: string): Promise<void> {
    const normalizedUserGuid = this.normalizeRequiredValue(userGuid, "userGuid");
    const normalizedRoleGuid = this.normalizeRequiredValue(roleGuid, "roleGuid");
    await this.sendAuthJson("PUT", `users/${encodeURIComponent(normalizedUserGuid)}/roles/${encodeURIComponent(normalizedRoleGuid)}`, undefined, false);
  }

  async removeAuthRole(userGuid: string, roleGuid: string): Promise<void> {
    const normalizedUserGuid = this.normalizeRequiredValue(userGuid, "userGuid");
    const normalizedRoleGuid = this.normalizeRequiredValue(roleGuid, "roleGuid");
    await this.sendAuthJson("DELETE", `users/${encodeURIComponent(normalizedUserGuid)}/roles/${encodeURIComponent(normalizedRoleGuid)}`, undefined, false);
  }

  getAuthRoles(): Promise<AuthRoleListItem[]> {
    return this.sendAuthJson<AuthRoleListItem[]>("GET", "roles");
  }

  createAuthRole(payload: CreateAuthRoleRequest): Promise<AuthRoleListItem> {
    return this.sendAuthJson<AuthRoleListItem>("POST", "roles", payload);
  }

  updateAuthRole(roleGuid: string, payload: UpdateAuthRoleRequest): Promise<AuthRoleListItem | null> {
    const normalizedRoleGuid = this.normalizeRequiredValue(roleGuid, "roleGuid");
    return this.sendAuthJson<AuthRoleListItem | null>("PUT", `roles/${encodeURIComponent(normalizedRoleGuid)}`, payload);
  }

  async deleteAuthRole(roleGuid: string): Promise<void> {
    const normalizedRoleGuid = this.normalizeRequiredValue(roleGuid, "roleGuid");
    await this.sendAuthJson("DELETE", `roles/${encodeURIComponent(normalizedRoleGuid)}`, undefined, false);
  }

  getAuthPermissionRules(userGuid?: string | null, roleGuid?: string | null): Promise<AuthPermissionRule[]> {
    const queryParts: string[] = [];
    const normalizedUserGuid = this.normalizeOptionalValue(userGuid);
    const normalizedRoleGuid = this.normalizeOptionalValue(roleGuid);
    if (normalizedUserGuid) {
      queryParts.push(`userGuid=${encodeURIComponent(normalizedUserGuid)}`);
    }
    if (normalizedRoleGuid) {
      queryParts.push(`roleGuid=${encodeURIComponent(normalizedRoleGuid)}`);
    }

    const suffix = queryParts.length === 0 ? "" : `?${queryParts.join("&")}`;
    return this.sendAuthJson<AuthPermissionRule[]>("GET", `permissions${suffix}`);
  }

  getAuthPermissionRule(ruleGuid: string): Promise<AuthPermissionRule | null> {
    const normalizedRuleGuid = this.normalizeRequiredValue(ruleGuid, "ruleGuid");
    return this.sendAuthJson<AuthPermissionRule | null>("GET", `permissions/${encodeURIComponent(normalizedRuleGuid)}`);
  }

  createAuthPermissionRule(payload: CreateAuthPermissionRuleRequest): Promise<AuthPermissionRule> {
    return this.sendAuthJson<AuthPermissionRule>("POST", "permissions", payload);
  }

  updateAuthPermissionRule(ruleGuid: string, payload: UpdateAuthPermissionRuleRequest): Promise<AuthPermissionRule | null> {
    const normalizedRuleGuid = this.normalizeRequiredValue(ruleGuid, "ruleGuid");
    return this.sendAuthJson<AuthPermissionRule | null>("PUT", `permissions/${encodeURIComponent(normalizedRuleGuid)}`, payload);
  }

  async deleteAuthPermissionRule(ruleGuid: string): Promise<void> {
    const normalizedRuleGuid = this.normalizeRequiredValue(ruleGuid, "ruleGuid");
    await this.sendAuthJson("DELETE", `permissions/${encodeURIComponent(normalizedRuleGuid)}`, undefined, false);
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

  private async getAuthTokenIfNecessary(forceRefresh = false): Promise<AuthTokenResponse> {
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

  private async getAuthTokenIfNecessaryCore(forceRefresh: boolean): Promise<AuthTokenResponse> {
    if (!forceRefresh && this.hasUsableAccessToken() && !this.shouldRefreshAccessToken()) {
      return this.createCurrentTokenResponse();
    }

    if (this.tokenState.refreshToken && (!forceRefresh || !this.password)) {
      try {
        const refreshed = await this.sendAuthJson<AuthTokenResponse>(
          "POST",
          "refresh",
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
      const token = await this.sendAuthJson<AuthTokenResponse>(
        "POST",
          "login",
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

  private clearAuthToken(): void {
    this.tokenState.accessToken = null;
    this.tokenState.accessTokenIssuedUtc = null;
    this.tokenState.accessTokenExpiresUtc = null;
    this.tokenState.refreshToken = null;
    this.tokenState.refreshTokenExpiresUtc = null;
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

  private createCurrentTokenResponse(): AuthTokenResponse {
    return {
      accessToken: this.tokenState.accessToken ?? "",
      accessTokenIssuedUtc: this.formatDate(this.tokenState.accessTokenIssuedUtc),
      accessTokenExpiresUtc: this.formatDate(this.tokenState.accessTokenExpiresUtc),
      refreshToken: this.tokenState.refreshToken ?? "",
      refreshTokenExpiresUtc: this.formatDate(this.tokenState.refreshTokenExpiresUtc),
      userId: "",
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

  private buildSchemaUrl(relativeUrl: string): string {
    return `${this.getSchemaBaseUrl().replace(/\/$/, "")}/${relativeUrl.replace(/^\/+/, "")}`;
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

  private getSchemaBaseUrl(): string {
    const suffix = "/chill";
    if (this.baseUrl.toLowerCase().endsWith(suffix)) {
      return `${this.baseUrl.slice(0, -suffix.length)}/chill-schema`;
    }

    return `${this.baseUrl.replace(/\/$/, "")}-schema`;
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

  private normalizeQueryValue(value?: string | null): string | null {
    return value == null ? null : value.trim();
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
        withCredentials: this.signalRWithCredentials,
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

  private async getUsersAssignedToRole(roleGuid: string): Promise<AuthUserListItem[]> {
    const users = await this.getAuthUsers();
    const matches = await Promise.all(
      users.map(async (user) => {
        const roles = await this.getAuthUserRoles(user.guid);
        return roles.some((role) => role.guid === roleGuid) ? user : null;
      })
    );

    return matches.filter((user): user is AuthUserListItem => user !== null);
  }

  private async syncUserRoles(userGuid: string, roleGuids: string[]): Promise<void> {
    const desiredRoleGuids = new Set(roleGuids.map((roleGuid) => this.normalizeRequiredValue(roleGuid, "roleGuid")));
    const currentRoles = await this.getAuthUserRoles(userGuid);
    const currentRoleGuids = new Set(currentRoles.map((role) => role.guid));

    for (const roleGuid of desiredRoleGuids) {
      if (!currentRoleGuids.has(roleGuid)) {
        await this.assignAuthRole(userGuid, roleGuid);
      }
    }

    for (const role of currentRoles) {
      if (!desiredRoleGuids.has(role.guid)) {
        await this.removeAuthRole(userGuid, role.guid);
      }
    }
  }

  private async syncUserPermissions(userGuid: string, permissions: AuthPermissionRuleItem[]): Promise<void> {
    const currentRules = await this.getAuthPermissionRules(userGuid, null);
    await this.syncPermissionRules(
      currentRules,
      permissions,
      (permission) => ({
        userGuid,
        roleGuid: null,
        effect: permission.effect,
        action: permission.action,
        scope: permission.scope,
        module: permission.module,
        entityName: permission.entityName,
        propertyName: permission.propertyName,
        appliesToAllProperties: permission.appliesToAllProperties,
        description: permission.description
      }),
      (payload) => this.createAuthPermissionRule(payload),
      (guid, payload) => this.updateAuthPermissionRule(guid, payload),
      (guid) => this.deleteAuthPermissionRule(guid)
    );
  }

  private async syncRoleUsers(roleGuid: string, userGuids: string[]): Promise<void> {
    const desiredUserGuids = new Set(userGuids.map((userGuid) => this.normalizeRequiredValue(userGuid, "userGuid")));
    const currentUsers = await this.getUsersAssignedToRole(roleGuid);
    const currentUserGuids = new Set(currentUsers.map((user) => user.guid));

    for (const userGuid of desiredUserGuids) {
      if (!currentUserGuids.has(userGuid)) {
        await this.assignAuthRole(userGuid, roleGuid);
      }
    }

    for (const user of currentUsers) {
      if (!desiredUserGuids.has(user.guid)) {
        await this.removeAuthRole(user.guid, roleGuid);
      }
    }
  }

  private async syncRolePermissions(roleGuid: string, permissions: AuthPermissionRuleItem[]): Promise<void> {
    const currentRules = await this.getAuthPermissionRules(null, roleGuid);
    await this.syncPermissionRules(
      currentRules,
      permissions,
      (permission) => ({
        userGuid: null,
        roleGuid,
        effect: permission.effect,
        action: permission.action,
        scope: permission.scope,
        module: permission.module,
        entityName: permission.entityName,
        propertyName: permission.propertyName,
        appliesToAllProperties: permission.appliesToAllProperties,
        description: permission.description
      }),
      (payload) => this.createAuthPermissionRule(payload),
      (guid, payload) => this.updateAuthPermissionRule(guid, payload),
      (guid) => this.deleteAuthPermissionRule(guid)
    );
  }

  private async syncPermissionRules(
    currentRules: AuthPermissionRule[],
    desiredRules: AuthPermissionRuleItem[],
    toPayload: (permission: AuthPermissionRuleItem) => CreateAuthPermissionRuleRequest,
    createRule: (payload: CreateAuthPermissionRuleRequest) => Promise<AuthPermissionRule>,
    updateRule: (guid: string, payload: UpdateAuthPermissionRuleRequest) => Promise<AuthPermissionRule | null>,
    deleteRule: (guid: string) => Promise<void>
  ): Promise<void> {
    const desiredByGuid = new Map<string, AuthPermissionRuleItem>();
    const newRules: AuthPermissionRuleItem[] = [];

    for (const permission of desiredRules) {
      const guid = this.normalizeOptionalValue(permission.guid);
      if (guid) {
        desiredByGuid.set(guid, permission);
      } else {
        newRules.push(permission);
      }
    }

    for (const currentRule of currentRules) {
      const desiredRule = desiredByGuid.get(currentRule.guid);
      if (!desiredRule) {
        await deleteRule(currentRule.guid);
        continue;
      }

      await updateRule(currentRule.guid, toPayload(desiredRule));
      desiredByGuid.delete(currentRule.guid);
    }

    for (const desiredRule of desiredByGuid.values()) {
      await createRule(toPayload(desiredRule));
    }

    for (const desiredRule of newRules) {
      await createRule(toPayload(desiredRule));
    }
  }
}



















