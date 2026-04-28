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

import { Inject, Injectable } from "@angular/core";
import { from, Observable } from "rxjs";
import { ChillSharpClient } from "@chill-sharp/ts-client";
import type {
  AuthRoleDetailsResponse,
  AuthRoleListItem,
  AuthTokenResponse,
  AuthPermissionRule,
  ChangePasswordRequest,
  ChangePasswordResponse,
  CreateAuthPermissionRuleRequest,
  CreateAuthRoleRequest,
  CreateAuthUserRequest,
  AuthUserDetailsResponse,
  AuthUserListItem,
  ChillDtoEntityOptions,
  ChillDtoMenuItem,
  ChillDtoSchema,
  ChillDtoSchemaListItem,
  ChillAttachmentUploadFile,
  ChillAttachmentUploadOptions,
  ChillValidationError,
  ChillEntityChangeNotification,
  ChillEntityChangeSubscription,
  GetAuthPermissionsResponse,
  GetTextRequest,
  GetTextResponse,
  LoginAuthIdentityRequest,
  PasswordResetTokenResponse,
  RequestPasswordResetRequest,
  ResetPasswordRequest,
  ResetPasswordResponse,
  RegisterAuthIdentityRequest,
  JsonObject,
  SetAuthRoleRequest,
  SetAuthUserRequest,
  UpdateAuthPermissionRuleRequest,
  UpdateAuthRoleRequest,
  UpdateAuthUserRequest
} from "@chill-sharp/ts-client";
import { CHILL_SHARP_CLIENT } from "./tokens.js";
import { CHILL_SHARP_NG_CLIENT_VERSION } from "./version.js";

@Injectable({
  providedIn: "root"
})
export class ChillSharpNgClient {
  constructor(@Inject(CHILL_SHARP_CLIENT) private readonly client: ChillSharpClient) {}

  query(dtoQuery: JsonObject): Observable<JsonObject> {
    return from(this.client.query(dtoQuery));
  }

  lookup(dtoQuery: JsonObject): Observable<JsonObject> {
    return from(this.client.lookup(dtoQuery));
  }

  find(dtoEntity: JsonObject): Observable<JsonObject | null> {
    return from(this.client.find(dtoEntity));
  }

  create(dtoEntity: JsonObject): Observable<JsonObject> {
    return from(this.client.create(dtoEntity));
  }

  update(dtoEntity: JsonObject): Observable<JsonObject> {
    return from(this.client.update(dtoEntity));
  }

  delete(dtoEntity: JsonObject): Observable<void> {
    return from(this.client.delete(dtoEntity));
  }

  autocomplete(dto: JsonObject): Observable<JsonObject> {
    return from(this.client.autocomplete(dto));
  }

  validate(dto: JsonObject): Observable<ChillValidationError[]> {
    return from(this.client.validate(dto));
  }

  chunk(operations: JsonObject[]): Observable<JsonObject[]> {
    return from(this.client.chunk(operations));
  }

  uploadAttachment(
    targetEntity: JsonObject,
    file: ChillAttachmentUploadFile,
    options?: ChillAttachmentUploadOptions
  ): Observable<JsonObject[]> {
    return from(this.client.uploadAttachment(targetEntity, file, options));
  }

  uploadAttachments(
    targetEntity: JsonObject,
    files: ChillAttachmentUploadFile[],
    options?: ChillAttachmentUploadOptions
  ): Observable<JsonObject[]> {
    return from(this.client.uploadAttachments(targetEntity, files, options));
  }

  getAttachments(targetEntity: JsonObject): Observable<JsonObject[]> {
    return from(this.client.getAttachments(targetEntity));
  }

  downloadAttachment(attachmentOrGuid: JsonObject | string): Observable<Blob> {
    return from(this.client.downloadAttachment(attachmentOrGuid));
  }

  version(): string {
    return CHILL_SHARP_NG_CLIENT_VERSION;
  }

  test(): Observable<string> {
    return from(this.client.test());
  }

  getSchema(chillType: string, chillViewCode: string, cultureName?: string, update = false): Observable<ChillDtoSchema | null> {
    return from(this.client.getSchema(chillType, chillViewCode, cultureName, update));
  }

  getSchemaList(cultureName?: string): Observable<ChillDtoSchemaListItem[]> {
    return from(this.client.getSchemaList(cultureName));
  }

  setSchema(schema: ChillDtoSchema): Observable<ChillDtoSchema | null> {
    return from(this.client.setSchema(schema));
  }

  getEntityOptions(chillType: string): Observable<ChillDtoEntityOptions> {
    return from(this.client.getEntityOptions(chillType));
  }

  setEntityOptions(entityOptions: ChillDtoEntityOptions): Observable<ChillDtoEntityOptions> {
    return from(this.client.setEntityOptions(entityOptions));
  }

  getMenu(parentGuid?: string | null): Observable<ChillDtoMenuItem[]> {
    return from(this.client.getMenu(parentGuid));
  }

  setMenu(menuItem: ChillDtoMenuItem): Observable<ChillDtoMenuItem> {
    return from(this.client.setMenu(menuItem));
  }


  deleteMenu(menuItemGuid: string): Observable<void> {
    return from(this.client.deleteMenu(menuItemGuid));
  }
  getText(request: GetTextRequest): Observable<GetTextResponse | null> {
    return from(this.client.getText(request));
  }

  getTexts(requests: GetTextRequest[]): Observable<Array<GetTextResponse | null>> {
    return from(this.client.getTexts(requests));
  }

  setText(payload: JsonObject): Observable<GetTextResponse> {
    return from(this.client.setText(payload));
  }

  watchEntityChanges(chillType: string, guid?: string | null): Observable<ChillEntityChangeNotification[]> {
    return new Observable<ChillEntityChangeNotification[]>((subscriber) => {
      let remoteSubscription: ChillEntityChangeSubscription | null = null;
      let isClosed = false;

      void this.client
        .subscribeToEntityChanges(
          chillType,
          async (changes) => {
            subscriber.next(changes);
          },
          guid
        )
        .then(async (subscription) => {
          remoteSubscription = subscription;
          if (isClosed) {
            await subscription.unsubscribe();
          }
        })
        .catch((error) => {
          subscriber.error(error);
        });

      return () => {
        isClosed = true;
        if (remoteSubscription) {
          void remoteSubscription.unsubscribe();
        }
      };
    });
  }

  disconnectEntityChanges(): Observable<void> {
    return from(this.client.disconnectEntityChanges());
  }

  registerAuthAccount(payload: RegisterAuthIdentityRequest): Observable<AuthTokenResponse> {
    return from(this.client.registerAuthAccount(payload));
  }

  loginAuthAccount(payload: LoginAuthIdentityRequest): Observable<AuthTokenResponse> {
    return from(this.client.loginAuthAccount(payload));
  }

  refreshAuthAccount(): Observable<AuthTokenResponse> {
    return from(this.client.refreshAuthAccount());
  }

  logoutAuthAccount(): Observable<void> {
    return from(this.client.logoutAuthAccount());
  }

  changeAuthPassword(payload: ChangePasswordRequest): Observable<ChangePasswordResponse> {
    return from(this.client.changeAuthPassword(payload));
  }

  requestAuthPasswordReset(payload: RequestPasswordResetRequest): Observable<PasswordResetTokenResponse> {
    return from(this.client.requestAuthPasswordReset(payload));
  }

  resetAuthPassword(payload: ResetPasswordRequest): Observable<ResetPasswordResponse> {
    return from(this.client.resetAuthPassword(payload));
  }

  getAuthPermissions(): Observable<GetAuthPermissionsResponse> {
    return from(this.client.getAuthPermissions());
  }

  getAuthUserList(): Observable<AuthUserListItem[]> {
    return from(this.client.getAuthUserList());
  }

  getAuthUser(userGuid: string): Observable<AuthUserDetailsResponse> {
    return from(this.client.getAuthUser(userGuid));
  }

  setAuthUser(payload: SetAuthUserRequest): Observable<AuthUserDetailsResponse> {
    return from(this.client.setAuthUser(payload));
  }

  getAuthUsers(): Observable<AuthUserListItem[]> {
    return from(this.client.getAuthUsers());
  }

  createAuthUser(payload: CreateAuthUserRequest): Observable<AuthUserListItem> {
    return from(this.client.createAuthUser(payload));
  }

  updateAuthUser(userGuid: string, payload: UpdateAuthUserRequest): Observable<AuthUserListItem | null> {
    return from(this.client.updateAuthUser(userGuid, payload));
  }

  deleteAuthUser(userGuid: string): Observable<void> {
    return from(this.client.deleteAuthUser(userGuid));
  }

  getAuthUserRoles(userGuid: string): Observable<AuthRoleListItem[]> {
    return from(this.client.getAuthUserRoles(userGuid));
  }

  assignAuthRole(userGuid: string, roleGuid: string): Observable<void> {
    return from(this.client.assignAuthRole(userGuid, roleGuid));
  }

  removeAuthRole(userGuid: string, roleGuid: string): Observable<void> {
    return from(this.client.removeAuthRole(userGuid, roleGuid));
  }

  getAuthRoleList(): Observable<AuthRoleListItem[]> {
    return from(this.client.getAuthRoleList());
  }

  getAuthModuleList(): Observable<string[]> {
    return from(this.client.getAuthModuleList());
  }

  getAuthEntityList(module?: string | null): Observable<string[]> {
    return from(this.client.getAuthEntityList(module));
  }

  getAuthQueryList(module?: string | null): Observable<string[]> {
    return from(this.client.getAuthQueryList(module));
  }

  getAuthModuleEntityList(module?: string | null): Observable<string[]> {
    return from(this.client.getAuthModuleEntityList(module));
  }

  getAuthPropertyList(chillType: string): Observable<string[]> {
    return from(this.client.getAuthPropertyList(chillType));
  }

  getAuthRole(roleGuid: string): Observable<AuthRoleDetailsResponse> {
    return from(this.client.getAuthRole(roleGuid));
  }

  setAuthRole(payload: SetAuthRoleRequest): Observable<AuthRoleDetailsResponse> {
    return from(this.client.setAuthRole(payload));
  }

  getAuthRoles(): Observable<AuthRoleListItem[]> {
    return from(this.client.getAuthRoles());
  }

  createAuthRole(payload: CreateAuthRoleRequest): Observable<AuthRoleListItem> {
    return from(this.client.createAuthRole(payload));
  }

  updateAuthRole(roleGuid: string, payload: UpdateAuthRoleRequest): Observable<AuthRoleListItem | null> {
    return from(this.client.updateAuthRole(roleGuid, payload));
  }

  deleteAuthRole(roleGuid: string): Observable<void> {
    return from(this.client.deleteAuthRole(roleGuid));
  }

  getAuthPermissionRules(userGuid?: string | null, roleGuid?: string | null): Observable<AuthPermissionRule[]> {
    return from(this.client.getAuthPermissionRules(userGuid, roleGuid));
  }

  getAuthPermissionRule(ruleGuid: string): Observable<AuthPermissionRule | null> {
    return from(this.client.getAuthPermissionRule(ruleGuid));
  }

  createAuthPermissionRule(payload: CreateAuthPermissionRuleRequest): Observable<AuthPermissionRule> {
    return from(this.client.createAuthPermissionRule(payload));
  }

  updateAuthPermissionRule(ruleGuid: string, payload: UpdateAuthPermissionRuleRequest): Observable<AuthPermissionRule | null> {
    return from(this.client.updateAuthPermissionRule(ruleGuid, payload));
  }

  deleteAuthPermissionRule(ruleGuid: string): Observable<void> {
    return from(this.client.deleteAuthPermissionRule(ruleGuid));
  }

  getRawClient(): ChillSharpClient {
    return this.client;
  }
}







