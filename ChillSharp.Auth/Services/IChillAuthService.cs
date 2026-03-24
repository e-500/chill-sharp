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

namespace ChillSharp.Auth.Services;

/// <summary>
/// Exposes the auth library operations for managing users, roles, rules, and permission evaluation.
/// </summary>
public interface IChillAuthService
{
    /// <summary>
    /// Returns the current user's direct permissions together with role permissions.
    /// </summary>
    Task<GetAuthPermissionsResponse> GetPermissionsAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the simplified user list used by management UIs.
    /// </summary>
    Task<IReadOnlyList<AuthUserListItemResponse>> GetUserListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the detailed user payload with role assignments and direct permissions.
    /// </summary>
    Task<AuthUserDetailsResponse?> GetManagedUserAsync(Guid userGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a user together with roles and direct permissions.
    /// </summary>
    Task<AuthUserDetailsResponse> SetUserAsync(SetAuthUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the simplified role list used by management UIs.
    /// </summary>
    Task<IReadOnlyList<AuthRoleListItemResponse>> GetRoleListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the detailed role payload with assigned users and direct permissions.
    /// </summary>
    Task<AuthRoleDetailsResponse?> GetManagedRoleAsync(Guid roleGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a role together with users and permissions.
    /// </summary>
    Task<AuthRoleDetailsResponse> SetRoleAsync(SetAuthRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all authorization users.
    /// </summary>
    Task<IReadOnlyList<AuthUser>> GetUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single authorization user by identifier.
    /// </summary>
    Task<AuthUser?> GetUserAsync(Guid userGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single authorization user by external identity identifier.
    /// </summary>
    Task<AuthUser?> GetUserByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new authorization user.
    /// </summary>
    Task<AuthUser> CreateUserAsync(CreateAuthUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing authorization user.
    /// </summary>
    Task<AuthUser?> UpdateUserAsync(Guid userGuid, UpdateAuthUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an authorization user.
    /// </summary>
    Task<bool> DeleteUserAsync(Guid userGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all authorization roles.
    /// </summary>
    Task<IReadOnlyList<AuthRole>> GetRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single authorization role by identifier.
    /// </summary>
    Task<AuthRole?> GetRoleAsync(Guid roleGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new authorization role.
    /// </summary>
    Task<AuthRole> CreateRoleAsync(CreateAuthRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing authorization role.
    /// </summary>
    Task<AuthRole?> UpdateRoleAsync(Guid roleGuid, UpdateAuthRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an authorization role.
    /// </summary>
    Task<bool> DeleteRoleAsync(Guid roleGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the roles assigned to a user.
    /// </summary>
    Task<IReadOnlyList<AuthRole>> GetUserRolesAsync(Guid userGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    Task<bool> AssignRoleAsync(Guid userGuid, Guid roleGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    Task<bool> RemoveRoleAsync(Guid userGuid, Guid roleGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns permission rules filtered by optional user or role.
    /// </summary>
    Task<IReadOnlyList<AuthPermissionRule>> GetPermissionRulesAsync(Guid? userGuid = null, Guid? roleGuid = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single permission rule by identifier.
    /// </summary>
    Task<AuthPermissionRule?> GetPermissionRuleAsync(Guid ruleGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new permission rule.
    /// </summary>
    Task<AuthPermissionRule> CreatePermissionRuleAsync(CreateAuthPermissionRuleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing permission rule.
    /// </summary>
    Task<AuthPermissionRule?> UpdatePermissionRuleAsync(Guid ruleGuid, UpdateAuthPermissionRuleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a permission rule.
    /// </summary>
    Task<bool> DeletePermissionRuleAsync(Guid ruleGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether a user can perform an entity-level action.
    /// </summary>
    Task<PermissionEvaluationResult> EvaluateEntityPermissionAsync(EvaluateEntityPermissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether a user can perform a property-level action.
    /// </summary>
    Task<PermissionEvaluationResult> EvaluatePropertyPermissionAsync(EvaluatePropertyPermissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether a user can perform a property-level action across a set of properties.
    /// </summary>
    Task<PropertyPermissionSetResult> EvaluatePropertySetPermissionAsync(EvaluatePropertySetPermissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached auth-management access decisions.
    /// </summary>
    void InvalidateManagementAccess(string? externalId = null);
}
