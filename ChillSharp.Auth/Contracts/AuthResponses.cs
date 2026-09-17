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

namespace ChillSharp.Auth.Contracts;

/// <summary>
/// Describes the resolved outcome of a permission evaluation request.
/// </summary>
public class PermissionEvaluationResult
{
    /// <summary>
    /// Gets or sets whether the requested action is allowed.
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Gets or sets the effect of the matched rule, if any.
    /// </summary>
    public PermissionEffect? MatchedEffect { get; set; }

    /// <summary>
    /// Gets or sets a human-readable explanation of the resolution result.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the rule that produced the result.
    /// </summary>
    public Guid? RuleGuid { get; set; }

    /// <summary>
    /// Gets or sets whether the matched rule came from a user or role assignment.
    /// </summary>
    public string? RuleSource { get; set; }
}

/// <summary>
/// Contains a permission evaluation result for a single property.
/// </summary>
public class PropertyPermissionResult
{
    /// <summary>
    /// Gets or sets the name of the evaluated property.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the evaluation result for the property.
    /// </summary>
    public PermissionEvaluationResult Result { get; set; } = new();
}

/// <summary>
/// Groups permission evaluation results for multiple properties.
/// </summary>
public class PropertyPermissionSetResult
{
    /// <summary>
    /// Gets or sets the evaluated property results.
    /// </summary>
    public IReadOnlyList<PropertyPermissionResult> Properties { get; set; } = Array.Empty<PropertyPermissionResult>();
}

/// <summary>
/// Simple user item returned by auth management list endpoints.
/// </summary>
public class AuthUserListItemResponse
{
    public Guid Guid { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DisplayCultureName { get; set; } = string.Empty;
    public string DisplayTimeZone { get; set; } = string.Empty;
    public string DisplayDateFormat { get; set; } = string.Empty;
    public string DisplayNumberFormat { get; set; } = string.Empty;
    public string PreferredTheme { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool CanManagePermissions { get; set; }
    public bool CanManageSchema { get; set; }
    public string MenuHierarchy { get; set; } = string.Empty;
}

/// <summary>
/// Simple role item returned by auth management list endpoints.
/// </summary>
public class AuthRoleListItemResponse
{
    public Guid Guid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string MenuHierarchy { get; set; } = string.Empty;
}

/// <summary>
/// Permission rule returned by auth management endpoints.
/// </summary>
public class AuthPermissionRuleResponse
{
    public Guid Guid { get; set; }
    public PermissionEffect Effect { get; set; }
    public PermissionAction Action { get; set; }
    public PermissionScope Scope { get; set; }
    public string Module { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? PropertyName { get; set; }
    public bool AppliesToAllProperties { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

/// <summary>
/// Role item expanded with its permissions.
/// </summary>
public class AuthRolePermissionsResponse : AuthRoleListItemResponse
{
    public IReadOnlyList<AuthPermissionRuleResponse> Permissions { get; set; } = Array.Empty<AuthPermissionRuleResponse>();
}

/// <summary>
/// Returns the current user's direct permissions and role permissions.
/// </summary>
public class GetAuthPermissionsResponse
{
    public AuthUserListItemResponse? User { get; set; }
    public IReadOnlyList<AuthPermissionRuleResponse> Permissions { get; set; } = Array.Empty<AuthPermissionRuleResponse>();
    public IReadOnlyList<AuthRolePermissionsResponse> Roles { get; set; } = Array.Empty<AuthRolePermissionsResponse>();
}

/// <summary>
/// Detailed user payload returned by get-user and set-user.
/// </summary>
public class AuthUserDetailsResponse : AuthUserListItemResponse
{
    public IReadOnlyList<AuthRoleListItemResponse> Roles { get; set; } = Array.Empty<AuthRoleListItemResponse>();
    public IReadOnlyList<AuthPermissionRuleResponse> Permissions { get; set; } = Array.Empty<AuthPermissionRuleResponse>();
}

/// <summary>
/// Detailed role payload returned by get-role and set-role.
/// </summary>
public class AuthRoleDetailsResponse : AuthRoleListItemResponse
{
    public IReadOnlyList<AuthUserListItemResponse> Users { get; set; } = Array.Empty<AuthUserListItemResponse>();
    public IReadOnlyList<AuthPermissionRuleResponse> Permissions { get; set; } = Array.Empty<AuthPermissionRuleResponse>();
}



