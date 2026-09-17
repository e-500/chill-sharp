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

export { createChillSharpClient, createChillSharpPlugin, useChillSharpClient } from "./plugin.js";
export {
  useAutocompleteMutation,
  useCurrentUserPreferences,
  useEntityChanges,
  useEntityMutation,
  useQueryMutation,
  useSchema,
  useSchemaList,
  useTest,
  useText,
  useTexts,
  useValidateMutation,
  useVersion
} from "./composables.js";
export type { ChillSharpVueOptions } from "./plugin.js";
export type { UseChillAsyncState, UseChillMutationState, UseChillSubscriptionState } from "./composables.js";
export { API_BASE_PATH, ChillSharpClient, ChillSharpClientError, PermissionAction, PermissionEffect, PermissionScope } from "@chill-sharp/ts-client";
export { CHILL_SHARP_VUE_CLIENT_VERSION } from "./version.js";
export type {
  AuthPermissionRule,
  AuthPermissionRuleItem,
  AuthRoleDetailsResponse,
  AuthRoleListItem,
  AuthRolePermissions,
  AuthTokenResponse,
  AuthUserDetailsResponse,
  AuthUserListItem,
  ChillDtoEntity,
  ChillUserPreferences,
  ChillDtoEntityOptions,
  ChillDtoMenuItem,
  ChillDtoProperty,
  ChillDtoPropertySchema,
  ChillDtoQuery,
  ChillDtoSchema,
  ChillDtoSchemaRelation,
  ChillDtoSchemaRelationLabel,
  ChillDtoSchemaListItem,
  ChillAttachmentUploadFile,
  ChillAttachmentUploadOptions,
  ChillEntityChangeAction,
  ChillEntityChangeCallback,
  ChillEntityChangeNotification,
  ChillEntityChangeSubscription,
  ChillOrdering,
  ChillPagination,
  ChillSharpClientOptions,
  GetAuthPermissionsResponse,
  GetTextRequest,
  GetTextResponse,
  JsonObject,
  JsonPrimitive,
  JsonValue,
  RegisterAuthIdentityRequest,
  SetAuthRoleRequest,
  SetAuthUserRequest
} from "@chill-sharp/ts-client";

