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

public class CreateAuthUserRequest
{
    [Required]
    [MaxLength(256)]
    public string ExternalId { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdateAuthUserRequest
{
    [Required]
    [MaxLength(256)]
    public string ExternalId { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class CreateAuthRoleRequest
{
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdateAuthRoleRequest : CreateAuthRoleRequest
{
}

public class CreateAuthPermissionRuleRequest
{
    public Guid? UserGuid { get; set; }
    public Guid? RoleGuid { get; set; }
    public PermissionEffect Effect { get; set; }
    public PermissionAction Action { get; set; }
    public PermissionScope Scope { get; set; }

    [Required]
    [MaxLength(256)]
    public string Module { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? EntityName { get; set; }

    [MaxLength(128)]
    public string? PropertyName { get; set; }

    public bool AppliesToAllProperties { get; set; }

    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;
}

public class UpdateAuthPermissionRuleRequest : CreateAuthPermissionRuleRequest
{
}

public class EvaluateEntityPermissionRequest
{
    public Guid UserGuid { get; set; }
    public PermissionAction Action { get; set; }

    [Required]
    [MaxLength(256)]
    public string Module { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string EntityName { get; set; } = string.Empty;
}

public class EvaluatePropertyPermissionRequest : EvaluateEntityPermissionRequest
{
    [Required]
    [MaxLength(128)]
    public string PropertyName { get; set; } = string.Empty;
}

public class EvaluatePropertySetPermissionRequest : EvaluateEntityPermissionRequest
{
    public IReadOnlyList<string> PropertyNames { get; set; } = Array.Empty<string>();
}
