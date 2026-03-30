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

export { ChillSharpProvider, useChillSharpClient } from "./context.js";
export {
  useEntityChanges,
  useEntityMutation,
  useQueryMutation,
  useSchema,
  useSchemaList,
  useTest,
  useText,
  useTexts,
  useVersion
} from "./hooks.js";
export type { ChillSharpProviderProps } from "./context.js";
export type { UseChillAsyncState, UseChillMutationState, UseChillSubscriptionState } from "./hooks.js";
export { CHILL_SHARP_REACT_CLIENT_VERSION } from "./version.js";
export {
  ChillSharpClient,
  ChillSharpClientError,
  PermissionAction,
  PermissionEffect,
  PermissionScope
} from "chill-sharp-ts-client";
export type {
  AuthPermissionRule,
  AuthPermissionRuleItem,
  AuthRoleDetailsResponse,
  AuthRoleListItem,
  AuthRolePermissions,
  AuthTokenResponse,
  AuthUserDetailsResponse,
  AuthUserListItem,
  ChillDtoEntityOptions,
  ChillDtoPropertySchema,
  ChillDtoSchema,
  ChillDtoSchemaListItem,
  ChillEntityChangeAction,
  ChillEntityChangeCallback,
  ChillEntityChangeNotification,
  ChillEntityChangeSubscription,
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
} from "chill-sharp-ts-client";

