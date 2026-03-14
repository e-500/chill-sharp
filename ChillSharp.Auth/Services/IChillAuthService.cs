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

public interface IChillAuthService
{
    Task<IReadOnlyList<AuthUser>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<AuthUser?> GetUserAsync(Guid userGuid, CancellationToken cancellationToken = default);
    Task<AuthUser> CreateUserAsync(CreateAuthUserRequest request, CancellationToken cancellationToken = default);
    Task<AuthUser?> UpdateUserAsync(Guid userGuid, UpdateAuthUserRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(Guid userGuid, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuthRole>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<AuthRole?> GetRoleAsync(Guid roleGuid, CancellationToken cancellationToken = default);
    Task<AuthRole> CreateRoleAsync(CreateAuthRoleRequest request, CancellationToken cancellationToken = default);
    Task<AuthRole?> UpdateRoleAsync(Guid roleGuid, UpdateAuthRoleRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteRoleAsync(Guid roleGuid, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuthRole>> GetUserRolesAsync(Guid userGuid, CancellationToken cancellationToken = default);
    Task<bool> AssignRoleAsync(Guid userGuid, Guid roleGuid, CancellationToken cancellationToken = default);
    Task<bool> RemoveRoleAsync(Guid userGuid, Guid roleGuid, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuthPermissionRule>> GetPermissionRulesAsync(Guid? userGuid = null, Guid? roleGuid = null, CancellationToken cancellationToken = default);
    Task<AuthPermissionRule?> GetPermissionRuleAsync(Guid ruleGuid, CancellationToken cancellationToken = default);
    Task<AuthPermissionRule> CreatePermissionRuleAsync(CreateAuthPermissionRuleRequest request, CancellationToken cancellationToken = default);
    Task<AuthPermissionRule?> UpdatePermissionRuleAsync(Guid ruleGuid, UpdateAuthPermissionRuleRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeletePermissionRuleAsync(Guid ruleGuid, CancellationToken cancellationToken = default);

    Task<PermissionEvaluationResult> EvaluateEntityPermissionAsync(EvaluateEntityPermissionRequest request, CancellationToken cancellationToken = default);
    Task<PermissionEvaluationResult> EvaluatePropertyPermissionAsync(EvaluatePropertyPermissionRequest request, CancellationToken cancellationToken = default);
    Task<PropertyPermissionSetResult> EvaluatePropertySetPermissionAsync(EvaluatePropertySetPermissionRequest request, CancellationToken cancellationToken = default);
}
