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

using ChillSharp.Auth.Model;
using System.ComponentModel.DataAnnotations;

namespace ChillSharp.Auth.Contracts;

/// <summary>
/// Request payload for creating a new authorization user.
/// </summary>
public class CreateAuthUserRequest
{
    /// <summary>
    /// Gets or sets the external identity provider identifier.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique user name.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name shown in the UI.
    /// </summary>
    [MaxLength(256)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the user can manage the auth API.
    /// </summary>
    public bool CanManagePermissions { get; set; }
}

/// <summary>
/// Request payload for updating an existing authorization user.
/// </summary>
public class UpdateAuthUserRequest
{
    /// <summary>
    /// Gets or sets the external identity provider identifier.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique user name.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name shown in the UI.
    /// </summary>
    [MaxLength(256)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the user can manage the auth API.
    /// </summary>
    public bool CanManagePermissions { get; set; }
}

/// <summary>
/// Request payload for creating a new authorization role.
/// </summary>
public class CreateAuthRoleRequest
{
    /// <summary>
    /// Gets or sets the unique role name.
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role description.
    /// </summary>
    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the role is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request payload for updating an existing authorization role.
/// </summary>
public class UpdateAuthRoleRequest : CreateAuthRoleRequest
{
}

/// <summary>
/// Request payload for creating a new permission rule.
/// </summary>
public class CreateAuthPermissionRuleRequest
{
    /// <summary>
    /// Gets or sets the target user identifier for direct user rules.
    /// </summary>
    public Guid? UserGuid { get; set; }

    /// <summary>
    /// Gets or sets the target role identifier for role-based rules.
    /// </summary>
    public Guid? RoleGuid { get; set; }

    /// <summary>
    /// Gets or sets the effect applied by the rule.
    /// </summary>
    public PermissionEffect Effect { get; set; }

    /// <summary>
    /// Gets or sets the action controlled by the rule.
    /// </summary>
    public PermissionAction Action { get; set; }

    /// <summary>
    /// Gets or sets the hierarchy scope targeted by the rule.
    /// </summary>
    public PermissionScope Scope { get; set; }

    /// <summary>
    /// Gets or sets the targeted module prefix.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the targeted entity name for entity and property rules.
    /// </summary>
    [MaxLength(128)]
    public string? EntityName { get; set; }

    /// <summary>
    /// Gets or sets the targeted property name for property rules.
    /// </summary>
    [MaxLength(128)]
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets or sets whether a property rule applies to all properties of the entity.
    /// </summary>
    public bool AppliesToAllProperties { get; set; }

    /// <summary>
    /// Gets or sets a free-text description for the rule.
    /// </summary>
    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Request payload for updating an existing permission rule.
/// </summary>
public class UpdateAuthPermissionRuleRequest : CreateAuthPermissionRuleRequest
{
}

/// <summary>
/// Editable permission-rule payload used by the new auth management endpoints.
/// </summary>
public class AuthPermissionRuleItem
{
    /// <summary>
    /// Gets or sets the permission-rule identifier when editing an existing rule.
    /// </summary>
    public Guid? Guid { get; set; }

    /// <summary>
    /// Gets or sets the effect applied by the rule.
    /// </summary>
    public PermissionEffect Effect { get; set; }

    /// <summary>
    /// Gets or sets the action controlled by the rule.
    /// </summary>
    public PermissionAction Action { get; set; }

    /// <summary>
    /// Gets or sets the hierarchy scope targeted by the rule.
    /// </summary>
    public PermissionScope Scope { get; set; }

    /// <summary>
    /// Gets or sets the targeted module prefix.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the targeted entity name for entity and property rules.
    /// </summary>
    [MaxLength(128)]
    public string? EntityName { get; set; }

    /// <summary>
    /// Gets or sets the targeted property name for property rules.
    /// </summary>
    [MaxLength(128)]
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets or sets whether a property rule applies to all properties of the entity.
    /// </summary>
    public bool AppliesToAllProperties { get; set; }

    /// <summary>
    /// Gets or sets a free-text description for the rule.
    /// </summary>
    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Request payload for creating or updating a user together with role assignments and direct permissions.
/// </summary>
public class SetAuthUserRequest
{
    /// <summary>
    /// Gets or sets the user identifier. Leave empty to create a new user.
    /// </summary>
    public Guid? Guid { get; set; }

    /// <summary>
    /// Gets or sets the external identity provider identifier.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique user name.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name shown in the UI.
    /// </summary>
    [MaxLength(256)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the user can manage auth permissions.
    /// </summary>
    public bool CanManagePermissions { get; set; }

    /// <summary>
    /// Gets or sets the full role assignment list for the user.
    /// </summary>
    public IReadOnlyList<Guid> RoleGuids { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Gets or sets the full list of direct permissions for the user.
    /// </summary>
    public IReadOnlyList<AuthPermissionRuleItem> Permissions { get; set; } = Array.Empty<AuthPermissionRuleItem>();
}

/// <summary>
/// Request payload for creating or updating a role together with its users and permissions.
/// </summary>
public class SetAuthRoleRequest
{
    /// <summary>
    /// Gets or sets the role identifier. Leave empty to create a new role.
    /// </summary>
    public Guid? Guid { get; set; }

    /// <summary>
    /// Gets or sets the unique role name.
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role description.
    /// </summary>
    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the role is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the full list of users assigned to the role.
    /// </summary>
    public IReadOnlyList<Guid> UserGuids { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Gets or sets the full list of role permissions.
    /// </summary>
    public IReadOnlyList<AuthPermissionRuleItem> Permissions { get; set; } = Array.Empty<AuthPermissionRuleItem>();
}

/// <summary>
/// Request payload for evaluating an entity-level permission.
/// </summary>
public class EvaluateEntityPermissionRequest
{
    /// <summary>
    /// Gets or sets the user whose effective permissions should be evaluated.
    /// </summary>
    public Guid UserGuid { get; set; }

    /// <summary>
    /// Gets or sets the entity-level action to evaluate.
    /// </summary>
    public PermissionAction Action { get; set; }

    /// <summary>
    /// Gets or sets the targeted module.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the targeted entity name.
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string EntityName { get; set; } = string.Empty;
}

/// <summary>
/// Request payload for evaluating a single property permission.
/// </summary>
public class EvaluatePropertyPermissionRequest : EvaluateEntityPermissionRequest
{
    /// <summary>
    /// Gets or sets the targeted property name.
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string PropertyName { get; set; } = string.Empty;
}

/// <summary>
/// Request payload for evaluating multiple property permissions in one request.
/// </summary>
public class EvaluatePropertySetPermissionRequest : EvaluateEntityPermissionRequest
{
    /// <summary>
    /// Gets or sets the property names to evaluate.
    /// </summary>
    public IReadOnlyList<string> PropertyNames { get; set; } = Array.Empty<string>();
}
