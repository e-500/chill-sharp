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

using ChillSharp.Annotations;
using ChillSharp.EF;
using System.ComponentModel.DataAnnotations;

namespace ChillSharp.Auth.Model;

[ChillEntity]
public class AuthPermissionRule : ChillEntity
{
    [Key]
    public override Guid Guid { get; set; }

    [ChillProperty]
    public Guid? UserGuid { get; set; }

    [ChillProperty]
    public AuthUser? User { get; set; }

    [ChillProperty]
    public Guid? RoleGuid { get; set; }

    [ChillProperty]
    public AuthRole? Role { get; set; }

    [ChillProperty]
    public PermissionEffect Effect { get; set; }

    [ChillProperty]
    public PermissionAction Action { get; set; }

    [ChillProperty]
    public PermissionScope Scope { get; set; }

    [ChillProperty]
    public string Module { get; set; } = string.Empty;

    [ChillProperty]
    public string? EntityName { get; set; }

    [ChillProperty]
    public string? PropertyName { get; set; }

    [ChillProperty]
    public bool AppliesToAllProperties { get; set; }

    [ChillProperty]
    public string Description { get; set; } = string.Empty;

    [ChillProperty]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public override string GetLabel(IChillContext Context)
    {
        return $"{Effect} {Action} {Module}";
    }
}
