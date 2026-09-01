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

using ChillSharp.Auth.Contracts;
using ChillSharp.Auth.Model;
using ChillSharp;
using System.Net.Http;
using System.Linq;

namespace ChillSharp.Client
{
    /// <summary>
    /// Adds client methods for interacting with the ChillSharp auth API module.
    /// </summary>
    public partial class ChillSharpClient
    {
        /// <summary>
        /// Returns the current authenticated user's culture, time-zone, date-format, and number-format preferences.
        /// </summary>
        public ChillUserPreferences GetCurrentUserPreferences()
        {
            var result = SendAuthJson<ChillUserPreferences>(HttpMethod.Get, "current-user-preferences");
            if (result == null) throw new ChillClientException("Unexpected null current user preferences result");
            return result;
        }

        /// <summary>
        /// Registers a new Identity account and stores the returned token pair inside the client.
        /// </summary>
        public AuthTokenResponse RegisterAuthAccount(RegisterAuthIdentityRequest request)
        {
            var result = SendAuthJson<AuthTokenResponse>(HttpMethod.Post, "register", request, allowAnonymous: true);
            if (result == null) throw new ChillClientException("Unexpected null auth register result");
            ApplyAuthToken(result, forgetPassword: true);
            return result;
        }

        /// <summary>
        /// Authenticates an Identity account with user name and password and stores the returned token pair.
        /// </summary>
        public AuthTokenResponse LoginAuthAccount(LoginAuthIdentityRequest request)
        {
            var result = SendAuthJson<AuthTokenResponse>(HttpMethod.Post, "login", request, allowAnonymous: true);
            if (result == null) throw new ChillClientException("Unexpected null auth login result");
            ApplyAuthToken(result, forgetPassword: true);
            return result;
        }

        /// <summary>
        /// Exchanges the current refresh token for a new token pair and stores it inside the client.
        /// </summary>
        public AuthTokenResponse RefreshAuthAccount()
        {
            var result = GetAuthTokenWithPasswordIfNecessary(forceRefresh: true);
            if (result == null) throw new ChillClientException("Unexpected null auth refresh result");
            return result;
        }

        /// <summary>
        /// Revokes the current authenticated session and clears the local token state.
        /// </summary>
        public void LogoutAuthAccount()
        {
            SendAuthJson<object>(HttpMethod.Post, "logout", payload: null, expectResponseBody: false);
            ClearAuthToken();
        }

        /// <summary>
        /// Changes the password of the authenticated user.
        /// </summary>
        public ChangePasswordResponse ChangeAuthPassword(ChangePasswordRequest request)
        {
            var result = SendAuthJson<ChangePasswordResponse>(HttpMethod.Post, "change-password", request);
            if (result == null) throw new ChillClientException("Unexpected null change-password result");
            return result;
        }

        /// <summary>
        /// Requests a password-reset token for a user.
        /// </summary>
        public PasswordResetTokenResponse RequestAuthPasswordReset(RequestPasswordResetRequest request)
        {
            var result = SendAuthJson<PasswordResetTokenResponse>(HttpMethod.Post, "request-password-reset", request, allowAnonymous: true);
            if (result == null) throw new ChillClientException("Unexpected null request-password-reset result");
            return result;
        }

        /// <summary>
        /// Resets a password by using a reset token.
        /// </summary>
        public ResetPasswordResponse ResetAuthPassword(ResetPasswordRequest request)
        {
            var result = SendAuthJson<ResetPasswordResponse>(HttpMethod.Post, "reset-password", request, allowAnonymous: true);
            if (result == null) throw new ChillClientException("Unexpected null reset-password result");
            return result;
        }

        /// <summary>
        /// Returns the current authenticated user's direct permissions and role permissions.
        /// </summary>
        public GetAuthPermissionsResponse GetAuthPermissions()
        {
            var result = SendAuthJson<GetAuthPermissionsResponse>(HttpMethod.Get, "get-permissions");
            if (result == null) throw new ChillClientException("Unexpected null get-permissions result");
            return result;
        }

        /// <summary>
        /// Returns the simplified auth user list used by management UIs.
        /// </summary>
        public List<AuthUserListItemResponse> GetAuthUserList()
        {
            return SendAuthJson<List<AuthUserListItemResponse>>(HttpMethod.Get, "get-user-list") ?? new List<AuthUserListItemResponse>();
        }

        /// <summary>
        /// Returns the full managed user payload.
        /// </summary>
        public AuthUserDetailsResponse? GetAuthManagedUser(Guid userGuid)
        {
            var user = GetAuthUser(userGuid);
            if (user == null)
            {
                return null;
            }

            return new AuthUserDetailsResponse
            {
                Guid = user.Guid,
                ExternalId = user.ExternalId,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                DisplayCultureName = user.DisplayCultureName,
                DisplayTimeZone = user.DisplayTimeZone,
                DisplayDateFormat = user.DisplayDateFormat,
                DisplayNumberFormat = user.DisplayNumberFormat,
                IsActive = user.IsActive,
                CanManagePermissions = user.CanManagePermissions,
                CanManageSchema = user.CanManageSchema,
                MenuHierarchy = user.MenuHierarchy,
                Roles = GetAuthUserRoles(userGuid).Select(MapRole).ToList(),
                Permissions = GetAuthPermissionRules(userGuid: userGuid).Select(MapPermissionRule).ToList()
            };
        }

        /// <summary>
        /// Creates or updates a user together with roles and direct permissions.
        /// </summary>
        public AuthUserDetailsResponse SetAuthUser(SetAuthUserRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            AuthUser user;
            if (request.Guid.HasValue)
            {
                user = UpdateAuthUser(request.Guid.Value, new UpdateAuthUserRequest
                {
                    ExternalId = request.ExternalId,
                    UserName = request.UserName,
                    DisplayName = request.DisplayName,
                    DisplayCultureName = request.DisplayCultureName,
                    DisplayTimeZone = request.DisplayTimeZone,
                    DisplayDateFormat = request.DisplayDateFormat,
                    DisplayNumberFormat = request.DisplayNumberFormat,
                    IsActive = request.IsActive,
                    CanManagePermissions = request.CanManagePermissions,
                    CanManageSchema = request.CanManageSchema,
                    MenuHierarchy = request.MenuHierarchy
                }) ?? throw new ChillClientException("Unexpected null auth user result");
            }
            else
            {
                user = CreateAuthUser(new CreateAuthUserRequest
                {
                    ExternalId = request.ExternalId,
                    Email = string.Empty,
                    UserName = request.UserName,
                    DisplayName = request.DisplayName,
                    DisplayCultureName = request.DisplayCultureName,
                    DisplayTimeZone = request.DisplayTimeZone,
                    DisplayDateFormat = request.DisplayDateFormat,
                    DisplayNumberFormat = request.DisplayNumberFormat,
                    IsActive = request.IsActive,
                    CanManagePermissions = request.CanManagePermissions,
                    CanManageSchema = request.CanManageSchema,
                    MenuHierarchy = request.MenuHierarchy
                });
            }

            SyncUserRoles(user.Guid, request.RoleGuids);
            SyncUserPermissionRules(user.Guid, request.Permissions);
            return GetAuthManagedUser(user.Guid) ?? throw new ChillClientException("Unexpected null managed auth user result");
        }

        /// <summary>
        /// Returns the simplified auth role list used by management UIs.
        /// </summary>
        public List<AuthRoleListItemResponse> GetAuthRoleList()
        {
            return SendAuthJson<List<AuthRoleListItemResponse>>(HttpMethod.Get, "get-role-list") ?? new List<AuthRoleListItemResponse>();
        }

        /// <summary>
        /// Returns the distinct logical modules available from the current Chill context.
        /// </summary>
        public List<string> GetAuthModuleList()
        {
            return SendAuthJson<List<string>>(HttpMethod.Get, "get-module-list") ?? new List<string>();
        }

        /// <summary>
        /// Returns the distinct entities available for the specified logical module.
        /// </summary>
        public List<string> GetAuthEntityList(string? module = null)
        {
            var suffix = module == null ? string.Empty : $"?module={Uri.EscapeDataString(module)}";
            return SendAuthJson<List<string>>(HttpMethod.Get, $"get-entity-list{suffix}") ?? new List<string>();
        }

        /// <summary>
        /// Returns the distinct queries available for the specified logical module.
        /// </summary>
        public List<string> GetAuthQueryList(string? module = null)
        {
            var suffix = module == null ? string.Empty : $"?module={Uri.EscapeDataString(module)}";
            return SendAuthJson<List<string>>(HttpMethod.Get, $"get-query-list{suffix}") ?? new List<string>();
        }

        /// <summary>
        /// Backward-compatible alias for the previous pluralized method name.
        /// </summary>
        public List<string> GetAuthEntities(string? module = null)
        {
            return GetAuthEntityList(module);
        }

        /// <summary>
        /// Backward-compatible alias for the previous pluralized method name.
        /// </summary>
        public List<string> GetAuthQueries(string? module = null)
        {
            return GetAuthQueryList(module);
        }

        /// <summary>
        /// Returns the distinct properties available for the specified Chill type.
        /// </summary>
        public List<string> GetAuthPropertyList(string chillType)
        {
            return SendAuthJson<List<string>>(HttpMethod.Get, $"get-property-list?chillType={Uri.EscapeDataString(chillType)}") ?? new List<string>();
        }

        /// <summary>
        /// Backward-compatible alias for the legacy module-entity list client call.
        /// </summary>
        public List<string> GetAuthModuleEntityList(string? module)
        {
            return GetAuthEntityList(module);
        }

        /// <summary>
        /// Returns the full managed role payload.
        /// </summary>
        public AuthRoleDetailsResponse? GetAuthManagedRole(Guid roleGuid)
        {
            var role = GetAuthRole(roleGuid);
            if (role == null)
            {
                return null;
            }

            return new AuthRoleDetailsResponse
            {
                Guid = role.Guid,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive,
                MenuHierarchy = role.MenuHierarchy,
                Users = GetUsersAssignedToRole(roleGuid).Select(MapUser).ToList(),
                Permissions = GetAuthPermissionRules(roleGuid: roleGuid).Select(MapPermissionRule).ToList()
            };
        }

        /// <summary>
        /// Creates or updates a role together with users and direct permissions.
        /// </summary>
        public AuthRoleDetailsResponse SetAuthRole(SetAuthRoleRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            AuthRole role;
            if (request.Guid.HasValue)
            {
                role = UpdateAuthRole(request.Guid.Value, new UpdateAuthRoleRequest
                {
                    Name = request.Name,
                    Description = request.Description,
                    IsActive = request.IsActive,
                    MenuHierarchy = request.MenuHierarchy
                }) ?? throw new ChillClientException("Unexpected null auth role result");
            }
            else
            {
                role = CreateAuthRole(new CreateAuthRoleRequest
                {
                    Name = request.Name,
                    Description = request.Description,
                    IsActive = request.IsActive,
                    MenuHierarchy = request.MenuHierarchy
                });
            }

            SyncRoleUsers(role.Guid, request.UserGuids);
            SyncRolePermissionRules(role.Guid, request.Permissions);
            return GetAuthManagedRole(role.Guid) ?? throw new ChillClientException("Unexpected null managed auth role result");
        }

        /// <summary>
        /// Returns all auth users.
        /// </summary>
        public List<AuthUser> GetAuthUsers()
        {
            return SendAuthJson<List<AuthUser>>(HttpMethod.Get, "users") ?? new List<AuthUser>();
        }

        /// <summary>
        /// Returns a single auth user by identifier.
        /// </summary>
        public AuthUser? GetAuthUser(Guid userGuid)
        {
            return SendAuthJson<AuthUser>(HttpMethod.Get, $"users/{userGuid}");
        }

        /// <summary>
        /// Creates a new auth user.
        /// </summary>
        public AuthUser CreateAuthUser(CreateAuthUserRequest request)
        {
            var result = SendAuthJson<AuthUser>(HttpMethod.Post, "users", request);
            if (result == null) throw new ChillClientException("Unexpected null auth user result");
            return result;
        }

        /// <summary>
        /// Updates an existing auth user.
        /// </summary>
        public AuthUser? UpdateAuthUser(Guid userGuid, UpdateAuthUserRequest request)
        {
            return SendAuthJson<AuthUser>(HttpMethod.Put, $"users/{userGuid}", request);
        }

        /// <summary>
        /// Deletes an auth user.
        /// </summary>
        public void DeleteAuthUser(Guid userGuid)
        {
            SendAuthJson<object>(HttpMethod.Delete, $"users/{userGuid}", expectResponseBody: false);
        }

        /// <summary>
        /// Returns the roles assigned to a user.
        /// </summary>
        public List<AuthRole> GetAuthUserRoles(Guid userGuid)
        {
            return SendAuthJson<List<AuthRole>>(HttpMethod.Get, $"users/{userGuid}/roles") ?? new List<AuthRole>();
        }

        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        public void AssignAuthRole(Guid userGuid, Guid roleGuid)
        {
            SendAuthJson<object>(HttpMethod.Put, $"users/{userGuid}/roles/{roleGuid}", payload: null, expectResponseBody: false);
        }

        /// <summary>
        /// Removes a role assignment from a user.
        /// </summary>
        public void RemoveAuthRole(Guid userGuid, Guid roleGuid)
        {
            SendAuthJson<object>(HttpMethod.Delete, $"users/{userGuid}/roles/{roleGuid}", payload: null, expectResponseBody: false);
        }

        /// <summary>
        /// Returns all auth roles.
        /// </summary>
        public List<AuthRole> GetAuthRoles()
        {
            return SendAuthJson<List<AuthRole>>(HttpMethod.Get, "roles") ?? new List<AuthRole>();
        }

        /// <summary>
        /// Returns a single auth role by identifier.
        /// </summary>
        public AuthRole? GetAuthRole(Guid roleGuid)
        {
            return SendAuthJson<AuthRole>(HttpMethod.Get, $"roles/{roleGuid}");
        }

        /// <summary>
        /// Creates a new auth role.
        /// </summary>
        public AuthRole CreateAuthRole(CreateAuthRoleRequest request)
        {
            var result = SendAuthJson<AuthRole>(HttpMethod.Post, "roles", request);
            if (result == null) throw new ChillClientException("Unexpected null auth role result");
            return result;
        }

        /// <summary>
        /// Updates an existing auth role.
        /// </summary>
        public AuthRole? UpdateAuthRole(Guid roleGuid, UpdateAuthRoleRequest request)
        {
            return SendAuthJson<AuthRole>(HttpMethod.Put, $"roles/{roleGuid}", request);
        }

        /// <summary>
        /// Deletes an auth role.
        /// </summary>
        public void DeleteAuthRole(Guid roleGuid)
        {
            SendAuthJson<object>(HttpMethod.Delete, $"roles/{roleGuid}", payload: null, expectResponseBody: false);
        }

        /// <summary>
        /// Returns permission rules filtered by optional user and role identifiers.
        /// </summary>
        public List<AuthPermissionRule> GetAuthPermissionRules(Guid? userGuid = null, Guid? roleGuid = null)
        {
            var query = new List<string>();
            if (userGuid.HasValue)
                query.Add($"userGuid={Uri.EscapeDataString(userGuid.Value.ToString())}");
            if (roleGuid.HasValue)
                query.Add($"roleGuid={Uri.EscapeDataString(roleGuid.Value.ToString())}");

            var suffix = query.Count == 0 ? string.Empty : "?" + string.Join("&", query);
            return SendAuthJson<List<AuthPermissionRule>>(HttpMethod.Get, $"permissions{suffix}") ?? new List<AuthPermissionRule>();
        }

        /// <summary>
        /// Returns a single permission rule by identifier.
        /// </summary>
        public AuthPermissionRule? GetAuthPermissionRule(Guid ruleGuid)
        {
            return SendAuthJson<AuthPermissionRule>(HttpMethod.Get, $"permissions/{ruleGuid}");
        }

        /// <summary>
        /// Creates a permission rule.
        /// </summary>
        public AuthPermissionRule CreateAuthPermissionRule(CreateAuthPermissionRuleRequest request)
        {
            var result = SendAuthJson<AuthPermissionRule>(HttpMethod.Post, "permissions", request);
            if (result == null) throw new ChillClientException("Unexpected null auth permission rule result");
            return result;
        }

        /// <summary>
        /// Updates a permission rule.
        /// </summary>
        public AuthPermissionRule? UpdateAuthPermissionRule(Guid ruleGuid, UpdateAuthPermissionRuleRequest request)
        {
            return SendAuthJson<AuthPermissionRule>(HttpMethod.Put, $"permissions/{ruleGuid}", request);
        }

        /// <summary>
        /// Deletes a permission rule.
        /// </summary>
        public void DeleteAuthPermissionRule(Guid ruleGuid)
        {
            SendAuthJson<object>(HttpMethod.Delete, $"permissions/{ruleGuid}", payload: null, expectResponseBody: false);
        }

        /// <summary>
        /// Evaluates an entity-level permission for a user.
        /// </summary>
        public PermissionEvaluationResult EvaluateAuthEntityPermission(EvaluateEntityPermissionRequest request)
        {
            throw new ChillClientException("Permission evaluation endpoints are not exposed by the merged auth-management controller.");
        }

        /// <summary>
        /// Evaluates a property-level permission for a user.
        /// </summary>
        public PermissionEvaluationResult EvaluateAuthPropertyPermission(EvaluatePropertyPermissionRequest request)
        {
            throw new ChillClientException("Permission evaluation endpoints are not exposed by the merged auth-management controller.");
        }

        /// <summary>
        /// Evaluates a property-level permission for multiple properties.
        /// </summary>
        public PropertyPermissionSetResult EvaluateAuthPropertySetPermission(EvaluatePropertySetPermissionRequest request)
        {
            throw new ChillClientException("Permission evaluation endpoints are not exposed by the merged auth-management controller.");
        }

        private static AuthUserListItemResponse MapUser(AuthUser user)
        {
            return new AuthUserListItemResponse
            {
                Guid = user.Guid,
                ExternalId = user.ExternalId,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                DisplayCultureName = user.DisplayCultureName,
                DisplayTimeZone = user.DisplayTimeZone,
                DisplayDateFormat = user.DisplayDateFormat,
                DisplayNumberFormat = user.DisplayNumberFormat,
                IsActive = user.IsActive,
                CanManagePermissions = user.CanManagePermissions,
                CanManageSchema = user.CanManageSchema,
                MenuHierarchy = user.MenuHierarchy
            };
        }

        private static AuthRoleListItemResponse MapRole(AuthRole role)
        {
            return new AuthRoleListItemResponse
            {
                Guid = role.Guid,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive,
                MenuHierarchy = role.MenuHierarchy
            };
        }

        private static AuthPermissionRuleResponse MapPermissionRule(AuthPermissionRule rule)
        {
            return new AuthPermissionRuleResponse
            {
                Guid = rule.Guid,
                Effect = rule.Effect,
                Action = rule.Action,
                Scope = rule.Scope,
                Module = rule.Module,
                EntityName = rule.EntityName,
                PropertyName = rule.PropertyName,
                AppliesToAllProperties = rule.AppliesToAllProperties,
                Description = rule.Description,
                CreatedUtc = rule.CreatedUtc
            };
        }

        private List<AuthUser> GetUsersAssignedToRole(Guid roleGuid)
        {
            return GetAuthUsers()
                .Where(user => GetAuthUserRoles(user.Guid).Any(role => role.Guid == roleGuid))
                .ToList();
        }

        private void SyncUserRoles(Guid userGuid, IReadOnlyList<Guid> desiredRoleGuids)
        {
            var desiredRoleGuidSet = new HashSet<Guid>(desiredRoleGuids);
            var currentRoleGuidSet = GetAuthUserRoles(userGuid).Select(role => role.Guid).ToHashSet();

            foreach (var roleGuid in desiredRoleGuidSet.Except(currentRoleGuidSet))
            {
                AssignAuthRole(userGuid, roleGuid);
            }

            foreach (var roleGuid in currentRoleGuidSet.Except(desiredRoleGuidSet))
            {
                RemoveAuthRole(userGuid, roleGuid);
            }
        }

        private void SyncUserPermissionRules(Guid userGuid, IReadOnlyList<AuthPermissionRuleItem> desiredPermissions)
        {
            var currentRules = GetAuthPermissionRules(userGuid: userGuid);
            SyncPermissionRules(
                currentRules,
                desiredPermissions,
                permission => new CreateAuthPermissionRuleRequest
                {
                    UserGuid = userGuid,
                    RoleGuid = null,
                    Effect = permission.Effect,
                    Action = permission.Action,
                    Scope = permission.Scope,
                    Module = permission.Module,
                    EntityName = permission.EntityName,
                    PropertyName = permission.PropertyName,
                    AppliesToAllProperties = permission.AppliesToAllProperties,
                    Description = permission.Description
                });
        }

        private void SyncRoleUsers(Guid roleGuid, IReadOnlyList<Guid> desiredUserGuids)
        {
            var desiredUserGuidSet = new HashSet<Guid>(desiredUserGuids);
            var currentUserGuidSet = GetUsersAssignedToRole(roleGuid).Select(user => user.Guid).ToHashSet();

            foreach (var userGuid in desiredUserGuidSet.Except(currentUserGuidSet))
            {
                AssignAuthRole(userGuid, roleGuid);
            }

            foreach (var userGuid in currentUserGuidSet.Except(desiredUserGuidSet))
            {
                RemoveAuthRole(userGuid, roleGuid);
            }
        }

        private void SyncRolePermissionRules(Guid roleGuid, IReadOnlyList<AuthPermissionRuleItem> desiredPermissions)
        {
            var currentRules = GetAuthPermissionRules(roleGuid: roleGuid);
            SyncPermissionRules(
                currentRules,
                desiredPermissions,
                permission => new CreateAuthPermissionRuleRequest
                {
                    UserGuid = null,
                    RoleGuid = roleGuid,
                    Effect = permission.Effect,
                    Action = permission.Action,
                    Scope = permission.Scope,
                    Module = permission.Module,
                    EntityName = permission.EntityName,
                    PropertyName = permission.PropertyName,
                    AppliesToAllProperties = permission.AppliesToAllProperties,
                    Description = permission.Description
                });
        }

        private void SyncPermissionRules(
            IReadOnlyList<AuthPermissionRule> currentRules,
            IReadOnlyList<AuthPermissionRuleItem> desiredPermissions,
            Func<AuthPermissionRuleItem, CreateAuthPermissionRuleRequest> createRequestFactory)
        {
            var desiredByGuid = desiredPermissions
                .Where(permission => permission.Guid.HasValue)
                .ToDictionary(permission => permission.Guid!.Value);

            foreach (var currentRule in currentRules)
            {
                if (!desiredByGuid.TryGetValue(currentRule.Guid, out var desiredPermission))
                {
                    DeleteAuthPermissionRule(currentRule.Guid);
                    continue;
                }

                UpdateAuthPermissionRule(currentRule.Guid, new UpdateAuthPermissionRuleRequest
                {
                    UserGuid = createRequestFactory(desiredPermission).UserGuid,
                    RoleGuid = createRequestFactory(desiredPermission).RoleGuid,
                    Effect = desiredPermission.Effect,
                    Action = desiredPermission.Action,
                    Scope = desiredPermission.Scope,
                    Module = desiredPermission.Module,
                    EntityName = desiredPermission.EntityName,
                    PropertyName = desiredPermission.PropertyName,
                    AppliesToAllProperties = desiredPermission.AppliesToAllProperties,
                    Description = desiredPermission.Description
                });

                desiredByGuid.Remove(currentRule.Guid);
            }

            foreach (var remainingPermission in desiredByGuid.Values)
            {
                CreateAuthPermissionRule(createRequestFactory(remainingPermission));
            }

            foreach (var newPermission in desiredPermissions.Where(permission => !permission.Guid.HasValue))
            {
                CreateAuthPermissionRule(createRequestFactory(newPermission));
            }
        }
    }
}
