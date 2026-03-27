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
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Auth.Services;

/// <summary>
/// Default implementation of the authorization service backed by <see cref="IChillAuthDbContext"/>.
/// </summary>
public class ChillAuthService : IChillAuthService
{
    #region Fields
    private readonly IChillAuthDbContext _context;
    private readonly IChillContext _chillContext;
    private readonly IChillAuthManagementAccessCache _managementAccessCache;
    #endregion

    #region Construction
    /// <summary>
    /// Initializes the service with the auth persistence abstraction.
    /// </summary>
    /// <param name="context">The auth store used for reads and writes.</param>
    public ChillAuthService(IChillAuthDbContext context, IChillContext chillContext, IChillAuthManagementAccessCache managementAccessCache)
    {
        _context = context;
        _chillContext = chillContext;
        _managementAccessCache = managementAccessCache;
    }
    #endregion

    #region Current User
    /// <inheritdoc />
    public async Task<GetAuthPermissionsResponse> GetPermissionsAsync(string externalId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserByExternalIdAsync(externalId, cancellationToken);
        if (user is null)
        {
            return new GetAuthPermissionsResponse();
        }

        var roleAssignments = await _context.UserRoles
            .AsNoTracking()
            .Where(x => x.UserGuid == user.Guid)
            .Select(x => x.Role)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var roleGuids = roleAssignments.Select(x => x.Guid).ToList();
        var rules = await _context.PermissionRules
            .AsNoTracking()
            .Where(x => x.UserGuid == user.Guid || (x.RoleGuid.HasValue && roleGuids.Contains(x.RoleGuid.Value)))
            .OrderBy(x => x.Scope)
            .ThenBy(x => x.Module)
            .ThenBy(x => x.EntityName)
            .ThenBy(x => x.PropertyName)
            .ToListAsync(cancellationToken);

        return new GetAuthPermissionsResponse
        {
            User = ToUserListItem(user),
            Permissions = rules
                .Where(x => x.UserGuid == user.Guid)
                .Select(ToPermissionRuleResponse)
                .ToList(),
            Roles = roleAssignments
                .Select(role => new AuthRolePermissionsResponse
                {
                    Guid = role.Guid,
                    Name = role.Name,
                    Description = role.Description,
                    IsActive = role.IsActive,
                    Permissions = rules
                        .Where(x => x.RoleGuid == role.Guid)
                        .Select(ToPermissionRuleResponse)
                        .ToList()
                })
                .ToList()
        };
    }
    #endregion

    #region Management UI
    /// <inheritdoc />
    public async Task<IReadOnlyList<AuthUserListItemResponse>> GetUserListAsync(CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(x => x.UserName)
            .ToListAsync(cancellationToken);
        return users.Select(ToUserListItem).ToList();
    }

    /// <inheritdoc />
    public async Task<AuthUserDetailsResponse?> GetManagedUserAsync(Guid userGuid, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Guid == userGuid, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = await _context.UserRoles
            .AsNoTracking()
            .Where(x => x.UserGuid == userGuid)
            .Select(x => x.Role)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var permissions = await _context.PermissionRules
            .AsNoTracking()
            .Where(x => x.UserGuid == userGuid)
            .OrderBy(x => x.Scope)
            .ThenBy(x => x.Module)
            .ThenBy(x => x.EntityName)
            .ThenBy(x => x.PropertyName)
            .ToListAsync(cancellationToken);

        return ToUserDetailsResponse(user, roles, permissions);
    }

    /// <inheritdoc />
    public async Task<AuthUserDetailsResponse> SetUserAsync(SetAuthUserRequest request, CancellationToken cancellationToken = default)
    {
        ValidateUser(request.ExternalId, request.UserName);

        var roleGuids = request.RoleGuids
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (roleGuids.Count > 0)
        {
            var existingRoleCount = await _context.Roles.CountAsync(x => roleGuids.Contains(x.Guid), cancellationToken);
            if (existingRoleCount != roleGuids.Count)
            {
                throw new ArgumentException("One or more referenced roles do not exist.");
            }
        }

        AuthUser user;
        if (request.Guid.HasValue && request.Guid.Value != Guid.Empty)
        {
            user = await _context.Users.FirstOrDefaultAsync(x => x.Guid == request.Guid.Value, cancellationToken)
                ?? throw new ArgumentException("The referenced user does not exist.");
        }
        else
        {
            user = new AuthUser
            {
                Guid = Guid.NewGuid()
            };
            _context.Users.Add(user);
        }

        user.ExternalId = request.ExternalId.Trim();
        user.UserName = request.UserName.Trim();
        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = request.IsActive;
        user.CanManagePermissions = request.CanManagePermissions;
        user.CanManageSchema = request.CanManageSchema;

        await _context.SaveChangesAsync(cancellationToken);
        await SyncUserRolesAsync(user.Guid, roleGuids, cancellationToken);
        await SyncPermissionRulesAsync(user.Guid, null, request.Permissions, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        InvalidateManagementAccess(user.ExternalId);

        return (await GetManagedUserAsync(user.Guid, cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuthRoleListItemResponse>> GetRoleListAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        return roles.Select(ToRoleListItem).ToList();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetModuleListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_chillContext.GetModuleList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetEntityListAsync(string? module = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_chillContext.GetEntities(module));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetQueryListAsync(string? module = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_chillContext.GetQueries(module));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetPropertyListAsync(string chillType, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_chillContext.GetProperties(chillType));
    }

    /// <inheritdoc />
    public async Task<AuthRoleDetailsResponse?> GetManagedRoleAsync(Guid roleGuid, CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Guid == roleGuid, cancellationToken);
        if (role is null)
        {
            return null;
        }

        var users = await _context.UserRoles
            .AsNoTracking()
            .Where(x => x.RoleGuid == roleGuid)
            .Select(x => x.User)
            .OrderBy(x => x.UserName)
            .ToListAsync(cancellationToken);

        var permissions = await _context.PermissionRules
            .AsNoTracking()
            .Where(x => x.RoleGuid == roleGuid)
            .OrderBy(x => x.Scope)
            .ThenBy(x => x.Module)
            .ThenBy(x => x.EntityName)
            .ThenBy(x => x.PropertyName)
            .ToListAsync(cancellationToken);

        return ToRoleDetailsResponse(role, users, permissions);
    }

    /// <inheritdoc />
    public async Task<AuthRoleDetailsResponse> SetRoleAsync(SetAuthRoleRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRole(request.Name);

        var userGuids = request.UserGuids
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (userGuids.Count > 0)
        {
            var existingUserCount = await _context.Users.CountAsync(x => userGuids.Contains(x.Guid), cancellationToken);
            if (existingUserCount != userGuids.Count)
            {
                throw new ArgumentException("One or more referenced users do not exist.");
            }
        }

        AuthRole role;
        if (request.Guid.HasValue && request.Guid.Value != Guid.Empty)
        {
            role = await _context.Roles.FirstOrDefaultAsync(x => x.Guid == request.Guid.Value, cancellationToken)
                ?? throw new ArgumentException("The referenced role does not exist.");
        }
        else
        {
            role = new AuthRole
            {
                Guid = Guid.NewGuid()
            };
            _context.Roles.Add(role);
        }

        role.Name = request.Name.Trim();
        role.Description = request.Description.Trim();
        role.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        await SyncRoleUsersAsync(role.Guid, userGuids, cancellationToken);
        await SyncPermissionRulesAsync(null, role.Guid, request.Permissions, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        InvalidateManagementAccess();

        return (await GetManagedRoleAsync(role.Guid, cancellationToken))!;
    }
    #endregion

    #region Users
    /// <inheritdoc />
    public async Task<IReadOnlyList<AuthUser>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(x => x.UserName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<AuthUser?> GetUserAsync(Guid userGuid, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Guid == userGuid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AuthUser?> GetUserByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return Task.FromResult<AuthUser?>(null);
        }

        var normalized = externalId.Trim();
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExternalId == normalized, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthUser> CreateUserAsync(CreateAuthUserRequest request, CancellationToken cancellationToken = default)
    {
        ValidateUser(request.ExternalId, request.UserName);

        var user = new AuthUser
        {
            Guid = Guid.NewGuid(),
            ExternalId = request.ExternalId.Trim(),
            UserName = request.UserName.Trim(),
            DisplayName = request.DisplayName.Trim(),
            IsActive = request.IsActive,
            CanManagePermissions = request.CanManagePermissions,
            CanManageSchema = request.CanManageSchema
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        InvalidateManagementAccess(user.ExternalId);
        return user;
    }

    /// <inheritdoc />
    public async Task<AuthUser?> UpdateUserAsync(Guid userGuid, UpdateAuthUserRequest request, CancellationToken cancellationToken = default)
    {
        ValidateUser(request.ExternalId, request.UserName);

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Guid == userGuid, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.ExternalId = request.ExternalId.Trim();
        user.UserName = request.UserName.Trim();
        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = request.IsActive;
        user.CanManagePermissions = request.CanManagePermissions;
        user.CanManageSchema = request.CanManageSchema;

        await _context.SaveChangesAsync(cancellationToken);
        InvalidateManagementAccess(user.ExternalId);
        return user;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAsync(Guid userGuid, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Guid == userGuid, cancellationToken);
        if (user is null)
        {
            return false;
        }

        InvalidateManagementAccess(user.ExternalId);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
    #endregion

    #region Roles
    /// <inheritdoc />
    public async Task<IReadOnlyList<AuthRole>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<AuthRole?> GetRoleAsync(Guid roleGuid, CancellationToken cancellationToken = default)
    {
        return _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Guid == roleGuid, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthRole> CreateRoleAsync(CreateAuthRoleRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRole(request.Name);

        var role = new AuthRole
        {
            Guid = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            IsActive = request.IsActive
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
        InvalidateManagementAccess();
        return role;
    }

    /// <inheritdoc />
    public async Task<AuthRole?> UpdateRoleAsync(Guid roleGuid, UpdateAuthRoleRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRole(request.Name);

        var role = await _context.Roles.FirstOrDefaultAsync(x => x.Guid == roleGuid, cancellationToken);
        if (role is null)
        {
            return null;
        }

        role.Name = request.Name.Trim();
        role.Description = request.Description.Trim();
        role.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        InvalidateManagementAccess();
        return role;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRoleAsync(Guid roleGuid, CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(x => x.Guid == roleGuid, cancellationToken);
        if (role is null)
        {
            return false;
        }

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken);
        InvalidateManagementAccess();
        return true;
    }
    #endregion

    #region Role Assignments
    /// <inheritdoc />
    public async Task<IReadOnlyList<AuthRole>> GetUserRolesAsync(Guid userGuid, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Where(x => x.UserGuid == userGuid)
            .Select(x => x.Role)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AssignRoleAsync(Guid userGuid, Guid roleGuid, CancellationToken cancellationToken = default)
    {
        var userExists = await _context.Users.AnyAsync(x => x.Guid == userGuid, cancellationToken);
        var roleExists = await _context.Roles.AnyAsync(x => x.Guid == roleGuid, cancellationToken);
        if (!userExists || !roleExists)
        {
            return false;
        }

        var exists = await _context.UserRoles.AnyAsync(x => x.UserGuid == userGuid && x.RoleGuid == roleGuid, cancellationToken);
        if (exists)
        {
            return true;
        }

        _context.UserRoles.Add(new AuthUserRole
        {
            UserGuid = userGuid,
            RoleGuid = roleGuid,
            AssignedUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        InvalidateManagementAccess();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveRoleAsync(Guid userGuid, Guid roleGuid, CancellationToken cancellationToken = default)
    {
        var membership = await _context.UserRoles.FirstOrDefaultAsync(x => x.UserGuid == userGuid && x.RoleGuid == roleGuid, cancellationToken);
        if (membership is null)
        {
            return false;
        }

        _context.UserRoles.Remove(membership);
        await _context.SaveChangesAsync(cancellationToken);
        InvalidateManagementAccess();
        return true;
    }
    #endregion

    #region Permission Rules
    /// <inheritdoc />
    public async Task<IReadOnlyList<AuthPermissionRule>> GetPermissionRulesAsync(Guid? userGuid = null, Guid? roleGuid = null, CancellationToken cancellationToken = default)
    {
        var query = _context.PermissionRules.AsNoTracking().AsQueryable();

        if (userGuid.HasValue)
        {
            query = query.Where(x => x.UserGuid == userGuid.Value);
        }

        if (roleGuid.HasValue)
        {
            query = query.Where(x => x.RoleGuid == roleGuid.Value);
        }

        return await query
            .OrderBy(x => x.UserGuid.HasValue ? 0 : 1)
            .ThenBy(x => x.Scope)
            .ThenBy(x => x.Module)
            .ThenBy(x => x.EntityName)
            .ThenBy(x => x.PropertyName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<AuthPermissionRule?> GetPermissionRuleAsync(Guid ruleGuid, CancellationToken cancellationToken = default)
    {
        return _context.PermissionRules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Guid == ruleGuid, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthPermissionRule> CreatePermissionRuleAsync(CreateAuthPermissionRuleRequest request, CancellationToken cancellationToken = default)
    {
        await ValidatePermissionRuleAsync(request.UserGuid, request.RoleGuid, request.Scope, request.Module, request.EntityName, request.PropertyName, request.AppliesToAllProperties, cancellationToken);

        var rule = new AuthPermissionRule
        {
            Guid = Guid.NewGuid(),
            UserGuid = request.UserGuid,
            RoleGuid = request.RoleGuid,
            Effect = request.Effect,
            Action = request.Action,
            Scope = request.Scope,
            Module = NormalizeModule(request.Module),
            EntityName = NormalizeEntity(request.EntityName),
            PropertyName = NormalizeProperty(request.PropertyName, request.AppliesToAllProperties),
            AppliesToAllProperties = request.AppliesToAllProperties,
            Description = request.Description.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        _context.PermissionRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);
        InvalidateManagementAccess();
        return rule;
    }

    /// <inheritdoc />
    public async Task<AuthPermissionRule?> UpdatePermissionRuleAsync(Guid ruleGuid, UpdateAuthPermissionRuleRequest request, CancellationToken cancellationToken = default)
    {
        await ValidatePermissionRuleAsync(request.UserGuid, request.RoleGuid, request.Scope, request.Module, request.EntityName, request.PropertyName, request.AppliesToAllProperties, cancellationToken);

        var rule = await _context.PermissionRules.FirstOrDefaultAsync(x => x.Guid == ruleGuid, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        rule.UserGuid = request.UserGuid;
        rule.RoleGuid = request.RoleGuid;
        rule.Effect = request.Effect;
        rule.Action = request.Action;
        rule.Scope = request.Scope;
        rule.Module = NormalizeModule(request.Module);
        rule.EntityName = NormalizeEntity(request.EntityName);
        rule.PropertyName = NormalizeProperty(request.PropertyName, request.AppliesToAllProperties);
        rule.AppliesToAllProperties = request.AppliesToAllProperties;
        rule.Description = request.Description.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        InvalidateManagementAccess();
        return rule;
    }

    /// <inheritdoc />
    public async Task<bool> DeletePermissionRuleAsync(Guid ruleGuid, CancellationToken cancellationToken = default)
    {
        var rule = await _context.PermissionRules.FirstOrDefaultAsync(x => x.Guid == ruleGuid, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        _context.PermissionRules.Remove(rule);
        await _context.SaveChangesAsync(cancellationToken);
        InvalidateManagementAccess();
        return true;
    }
    #endregion

    #region Permission Evaluation
    /// <inheritdoc />
    public async Task<PermissionEvaluationResult> EvaluateEntityPermissionAsync(EvaluateEntityPermissionRequest request, CancellationToken cancellationToken = default)
    {
        ValidateEntityEvaluation(request.Action, request.Module, request.EntityName);

        var candidate = await ResolveRuleAsync(
            request.UserGuid,
            request.Action,
            NormalizeModule(request.Module),
            NormalizeEntity(request.EntityName)!,
            null,
            cancellationToken);

        return candidate?.ToResult() ?? DefaultDeny("No matching entity permission rule.");
    }

    /// <inheritdoc />
    public async Task<PermissionEvaluationResult> EvaluatePropertyPermissionAsync(EvaluatePropertyPermissionRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePropertyEvaluation(request.Action, request.Module, request.EntityName, request.PropertyName);

        var candidate = await ResolveRuleAsync(
            request.UserGuid,
            request.Action,
            NormalizeModule(request.Module),
            NormalizeEntity(request.EntityName)!,
            NormalizeProperty(request.PropertyName, false),
            cancellationToken);

        return candidate?.ToResult() ?? DefaultDeny("No matching property permission rule.");
    }

    /// <inheritdoc />
    public async Task<PropertyPermissionSetResult> EvaluatePropertySetPermissionAsync(EvaluatePropertySetPermissionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PropertyNames.Count == 0)
        {
            return new PropertyPermissionSetResult();
        }

        if (request.Action is not PermissionAction.See and not PermissionAction.Modify)
        {
            throw new ArgumentException("Property-set evaluation supports only SEE and MODIFY actions.");
        }

        NormalizeModule(request.Module);
        RequireEntity(request.EntityName);

        var results = new List<PropertyPermissionResult>(request.PropertyNames.Count);
        foreach (var propertyName in request.PropertyNames)
        {
            var evaluation = await EvaluatePropertyPermissionAsync(new EvaluatePropertyPermissionRequest
            {
                UserGuid = request.UserGuid,
                Action = request.Action,
                Module = request.Module,
                EntityName = request.EntityName,
                PropertyName = propertyName
            }, cancellationToken);

            results.Add(new PropertyPermissionResult
            {
                PropertyName = propertyName,
                Result = evaluation
            });
        }

        return new PropertyPermissionSetResult
        {
            Properties = results
        };
    }
    #endregion

    #region Cache
    public void InvalidateManagementAccess(string? externalId = null)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            _managementAccessCache.InvalidateAll();
        else
            _managementAccessCache.Invalidate(externalId);
    }
    #endregion

    #region Synchronization Helpers
    private async Task SyncUserRolesAsync(Guid userGuid, IReadOnlyList<Guid> requestedRoleGuids, CancellationToken cancellationToken)
    {
        var existingMemberships = await _context.UserRoles
            .Where(x => x.UserGuid == userGuid)
            .ToListAsync(cancellationToken);

        var requestedSet = requestedRoleGuids.ToHashSet();
        foreach (var membership in existingMemberships.Where(x => !requestedSet.Contains(x.RoleGuid)))
        {
            _context.UserRoles.Remove(membership);
        }

        var existingSet = existingMemberships.Select(x => x.RoleGuid).ToHashSet();
        foreach (var roleGuid in requestedRoleGuids.Where(x => !existingSet.Contains(x)))
        {
            _context.UserRoles.Add(new AuthUserRole
            {
                UserGuid = userGuid,
                RoleGuid = roleGuid,
                AssignedUtc = DateTime.UtcNow
            });
        }
    }

    private async Task SyncRoleUsersAsync(Guid roleGuid, IReadOnlyList<Guid> requestedUserGuids, CancellationToken cancellationToken)
    {
        var existingMemberships = await _context.UserRoles
            .Where(x => x.RoleGuid == roleGuid)
            .ToListAsync(cancellationToken);

        var requestedSet = requestedUserGuids.ToHashSet();
        foreach (var membership in existingMemberships.Where(x => !requestedSet.Contains(x.UserGuid)))
        {
            _context.UserRoles.Remove(membership);
        }

        var existingSet = existingMemberships.Select(x => x.UserGuid).ToHashSet();
        foreach (var userGuid in requestedUserGuids.Where(x => !existingSet.Contains(x)))
        {
            _context.UserRoles.Add(new AuthUserRole
            {
                UserGuid = userGuid,
                RoleGuid = roleGuid,
                AssignedUtc = DateTime.UtcNow
            });
        }
    }

    private async Task SyncPermissionRulesAsync(Guid? userGuid, Guid? roleGuid, IReadOnlyList<AuthPermissionRuleItem> requestedRules, CancellationToken cancellationToken)
    {
        if (userGuid.HasValue == roleGuid.HasValue)
        {
            throw new ArgumentException("Permission synchronization requires either a user or a role target.");
        }

        foreach (var rule in requestedRules)
        {
            await ValidatePermissionRuleAsync(userGuid, roleGuid, rule.Scope, rule.Module, rule.EntityName, rule.PropertyName, rule.AppliesToAllProperties, cancellationToken);
        }

        var existingRules = await _context.PermissionRules
            .Where(x => x.UserGuid == userGuid && x.RoleGuid == roleGuid)
            .ToListAsync(cancellationToken);

        var unmatchedExisting = existingRules.ToDictionary(x => x.Guid);
        var matchedExistingGuids = new HashSet<Guid>();

        foreach (var requestRule in requestedRules)
        {
            AuthPermissionRule? target = null;
            if (requestRule.Guid.HasValue && requestRule.Guid.Value != Guid.Empty)
            {
                unmatchedExisting.TryGetValue(requestRule.Guid.Value, out target);
            }

            if (target is null)
            {
                target = existingRules
                    .Where(x => !matchedExistingGuids.Contains(x.Guid))
                    .FirstOrDefault(x => PermissionRuleSemanticKey.FromEntity(x) == PermissionRuleSemanticKey.FromRequest(requestRule));
            }

            if (target is null)
            {
                target = new AuthPermissionRule
                {
                    Guid = requestRule.Guid is { } requestGuid && requestGuid != Guid.Empty ? requestGuid : Guid.NewGuid(),
                    UserGuid = userGuid,
                    RoleGuid = roleGuid,
                    CreatedUtc = DateTime.UtcNow
                };
                _context.PermissionRules.Add(target);
            }

            ApplyPermissionRule(target, userGuid, roleGuid, requestRule);
            matchedExistingGuids.Add(target.Guid);
            unmatchedExisting.Remove(target.Guid);
        }

        foreach (var obsoleteRule in existingRules.Where(x => !matchedExistingGuids.Contains(x.Guid)))
        {
            _context.PermissionRules.Remove(obsoleteRule);
        }
    }
    #endregion

    #region Resolution Helpers
    private async Task<ResolvedRule?> ResolveRuleAsync(Guid userGuid, PermissionAction action, string module, string entityName, string? propertyName, CancellationToken cancellationToken)
    {
        var roleGuids = await _context.UserRoles
            .AsNoTracking()
            .Where(x => x.UserGuid == userGuid)
            .Select(x => x.RoleGuid)
            .ToListAsync(cancellationToken);

        var candidates = await _context.PermissionRules
            .AsNoTracking()
            .Where(x => x.Action == action || x.Action == PermissionAction.FullControl)
            .Where(x => x.UserGuid == userGuid || (x.RoleGuid.HasValue && roleGuids.Contains(x.RoleGuid.Value)))
            .ToListAsync(cancellationToken);

        return candidates
            .Select(x => BuildResolvedRule(x, action, module, entityName, propertyName))
            .Where(x => x is not null)
            .Select(x => x!)
            .OrderBy(x => x.Order)
            .FirstOrDefault();
    }

    private static ResolvedRule? BuildResolvedRule(AuthPermissionRule rule, PermissionAction requestedAction, string module, string entityName, string? propertyName)
    {
        var scopeSpecificity = GetScopeSpecificity(rule, propertyName);
        if (scopeSpecificity == 0)
        {
            return null;
        }

        if (!ModuleMatches(rule.Module, module))
        {
            return null;
        }

        if (scopeSpecificity >= 2)
        {
            var normalizedEntity = NormalizeEntity(rule.EntityName);
            if (!string.Equals(normalizedEntity, entityName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        if (scopeSpecificity == 3)
        {
            if (propertyName is null)
            {
                return null;
            }

            if (!rule.AppliesToAllProperties)
            {
                var normalizedProperty = NormalizeProperty(rule.PropertyName, false);
                if (!string.Equals(normalizedProperty, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
            }
        }

        var subjectRank = rule.UserGuid.HasValue ? 0 : 1;
        var actionRank = rule.Action == requestedAction ? 0 : 1;
        var effectRank = rule.Effect == PermissionEffect.Deny ? 0 : 1;

        return new ResolvedRule(rule, (subjectRank, scopeSpecificity * -1, actionRank, effectRank));
    }

    private static int GetScopeSpecificity(AuthPermissionRule rule, string? propertyName)
    {
        return rule.Scope switch
        {
            PermissionScope.Property when propertyName is not null => 3,
            PermissionScope.Entity => 2,
            PermissionScope.Module => 1,
            _ => 0
        };
    }
    #endregion

    #region Validation Helpers
    private async Task ValidatePermissionRuleAsync(Guid? userGuid, Guid? roleGuid, PermissionScope scope, string module, string? entityName, string? propertyName, bool appliesToAllProperties, CancellationToken cancellationToken)
    {
        if (userGuid.HasValue == roleGuid.HasValue)
        {
            throw new ArgumentException("A permission rule must target either a user or a role.");
        }

        if (userGuid.HasValue && !await _context.Users.AnyAsync(x => x.Guid == userGuid.Value, cancellationToken))
        {
            throw new ArgumentException("The referenced user does not exist.");
        }

        if (roleGuid.HasValue && !await _context.Roles.AnyAsync(x => x.Guid == roleGuid.Value, cancellationToken))
        {
            throw new ArgumentException("The referenced role does not exist.");
        }

        NormalizeModule(module);

        switch (scope)
        {
            case PermissionScope.Module:
                if (!string.IsNullOrWhiteSpace(entityName) || !string.IsNullOrWhiteSpace(propertyName) || appliesToAllProperties)
                {
                    throw new ArgumentException("Module rules cannot define entity or property values.");
                }
                break;
            case PermissionScope.Entity:
                RequireEntity(entityName);
                if (!string.IsNullOrWhiteSpace(propertyName) || appliesToAllProperties)
                {
                    throw new ArgumentException("Entity rules cannot define property values.");
                }
                break;
            case PermissionScope.Property:
                RequireEntity(entityName);
                if (!appliesToAllProperties)
                {
                    RequireProperty(propertyName);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scope));
        }
    }

    private static void ValidateUser(string externalId, string userName)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("ExternalId is required.");
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("UserName is required.");
        }
    }

    private static void ValidateRole(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Role name is required.");
        }
    }
    #endregion

    #region Normalization Helpers
    private static void ValidateEntityEvaluation(PermissionAction action, string module, string entityName)
    {
        if (action is PermissionAction.See or PermissionAction.Modify)
        {
            throw new ArgumentException("SEE and MODIFY require property-level evaluation.");
        }

        NormalizeModule(module);
        NormalizeEntity(entityName);
    }

    private static void ValidatePropertyEvaluation(PermissionAction action, string module, string entityName, string propertyName)
    {
        if (action is not PermissionAction.See and not PermissionAction.Modify)
        {
            throw new ArgumentException("Only SEE and MODIFY can be evaluated at property scope.");
        }

        NormalizeModule(module);
        NormalizeEntity(entityName);
        NormalizeProperty(propertyName, false);
    }

    private static bool ModuleMatches(string ruleModule, string requestedModule)
    {
        if (string.Equals(ruleModule, requestedModule, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return requestedModule.StartsWith(ruleModule + ".", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeModule(string module)
    {
        var normalized = module.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Module is required.");
        }

        return normalized;
    }

    private static string? NormalizeEntity(string? entityName)
    {
        if (entityName is null)
        {
            return null;
        }

        var normalized = entityName.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("EntityName is required.");
        }

        if (normalized.Contains('.'))
        {
            throw new ArgumentException("EntityName cannot contain dots.");
        }

        return normalized;
    }

    private static string RequireEntity(string? entityName)
    {
        return NormalizeEntity(entityName) ?? throw new ArgumentException("EntityName is required.");
    }

    private static string? NormalizeProperty(string? propertyName, bool appliesToAllProperties)
    {
        if (appliesToAllProperties)
        {
            return null;
        }

        if (propertyName is null)
        {
            return null;
        }

        var normalized = propertyName.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("PropertyName is required.");
        }

        if (normalized.Contains('.'))
        {
            throw new ArgumentException("PropertyName cannot contain dots.");
        }

        return normalized;
    }

    private static string RequireProperty(string? propertyName)
    {
        return NormalizeProperty(propertyName, false) ?? throw new ArgumentException("PropertyName is required.");
    }
    #endregion

    #region Mapping Helpers
    private static PermissionEvaluationResult DefaultDeny(string reason)
    {
        return new PermissionEvaluationResult
        {
            IsAllowed = false,
            Reason = reason
        };
    }

    private static void ApplyPermissionRule(AuthPermissionRule target, Guid? userGuid, Guid? roleGuid, AuthPermissionRuleItem source)
    {
        target.UserGuid = userGuid;
        target.RoleGuid = roleGuid;
        target.Effect = source.Effect;
        target.Action = source.Action;
        target.Scope = source.Scope;
        target.Module = NormalizeModule(source.Module);
        target.EntityName = NormalizeEntity(source.EntityName);
        target.PropertyName = NormalizeProperty(source.PropertyName, source.AppliesToAllProperties);
        target.AppliesToAllProperties = source.AppliesToAllProperties;
        target.Description = source.Description.Trim();
    }

    private static AuthUserListItemResponse ToUserListItem(AuthUser user)
    {
        return new AuthUserListItemResponse
        {
            Guid = user.Guid,
            ExternalId = user.ExternalId,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            CanManagePermissions = user.CanManagePermissions,
            CanManageSchema = user.CanManageSchema
        };
    }

    private static AuthRoleListItemResponse ToRoleListItem(AuthRole role)
    {
        return new AuthRoleListItemResponse
        {
            Guid = role.Guid,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive
        };
    }

    private static AuthPermissionRuleResponse ToPermissionRuleResponse(AuthPermissionRule rule)
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

    private static AuthUserDetailsResponse ToUserDetailsResponse(AuthUser user, IReadOnlyList<AuthRole> roles, IReadOnlyList<AuthPermissionRule> permissions)
    {
        return new AuthUserDetailsResponse
        {
            Guid = user.Guid,
            ExternalId = user.ExternalId,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            CanManagePermissions = user.CanManagePermissions,
            CanManageSchema = user.CanManageSchema,
            Roles = roles.Select(ToRoleListItem).ToList(),
            Permissions = permissions.Select(ToPermissionRuleResponse).ToList()
        };
    }

    private static AuthRoleDetailsResponse ToRoleDetailsResponse(AuthRole role, IReadOnlyList<AuthUser> users, IReadOnlyList<AuthPermissionRule> permissions)
    {
        return new AuthRoleDetailsResponse
        {
            Guid = role.Guid,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            Users = users.Select(ToUserListItem).ToList(),
            Permissions = permissions.Select(ToPermissionRuleResponse).ToList()
        };
    }
    #endregion

    #region Private Records
    private sealed record PermissionRuleSemanticKey(
        PermissionEffect Effect,
        PermissionAction Action,
        PermissionScope Scope,
        string Module,
        string? EntityName,
        string? PropertyName,
        bool AppliesToAllProperties)
    {
        public static PermissionRuleSemanticKey FromEntity(AuthPermissionRule rule)
        {
            return new PermissionRuleSemanticKey(
                rule.Effect,
                rule.Action,
                rule.Scope,
                NormalizeModule(rule.Module),
                NormalizeEntity(rule.EntityName),
                NormalizeProperty(rule.PropertyName, rule.AppliesToAllProperties),
                rule.AppliesToAllProperties);
        }

        public static PermissionRuleSemanticKey FromRequest(AuthPermissionRuleItem rule)
        {
            return new PermissionRuleSemanticKey(
                rule.Effect,
                rule.Action,
                rule.Scope,
                NormalizeModule(rule.Module),
                NormalizeEntity(rule.EntityName),
                NormalizeProperty(rule.PropertyName, rule.AppliesToAllProperties),
                rule.AppliesToAllProperties);
        }
    }

    private sealed record ResolvedRule(AuthPermissionRule Rule, (int SubjectRank, int SpecificityRank, int ActionRank, int EffectRank) Order)
    {
        public PermissionEvaluationResult ToResult()
        {
            return new PermissionEvaluationResult
            {
                IsAllowed = Rule.Effect == PermissionEffect.Allow,
                MatchedEffect = Rule.Effect,
                Reason = BuildReason(),
                RuleGuid = Rule.Guid,
                RuleSource = Rule.UserGuid.HasValue ? "User" : "Role"
            };
        }

        private string BuildReason()
        {
            var subject = Rule.UserGuid.HasValue ? "user" : "role";
            var resource = Rule.Scope switch
            {
                PermissionScope.Module => Rule.Module,
                PermissionScope.Entity => $"{Rule.Module}.{Rule.EntityName}",
                PermissionScope.Property when Rule.AppliesToAllProperties => $"{Rule.Module}.{Rule.EntityName}.*",
                _ => $"{Rule.Module}.{Rule.EntityName}.{Rule.PropertyName}"
            };

            return $"{subject} {Rule.Scope} rule matched on {resource}.";
        }
    }
    #endregion
}
