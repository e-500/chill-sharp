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
using System.ComponentModel.DataAnnotations.Schema;

namespace ChillSharp.Auth.Model;

/// <summary>
/// Stores allow and deny permission rules for users or roles across module, entity, and property scopes.
/// </summary>
[ChillEntity(
    "5BE5AAB0-F376-4D56-9762-86F11A6E83AB",
    "Auth permission rule",
    "Regola permesso auth")]
[Table("auth-permission-rule")]
public class AuthPermissionRule : ChillEntity
{
    /// <summary>
    /// Unique identifier of the permission rule.
    /// </summary>
    [Key]
    [Column("guid")]
    [ChillProperty(
        "AF24E155-209F-4538-B5FD-F041DC9966B1",
        "Guid",
        "Guid")]
    public override Guid Guid { get; set; }

    /// <summary>
    /// Target user identifier when the rule applies directly to a user.
    /// </summary>
    [Column("user-guid")]
    [ChillProperty(
        "6A53E172-1E0C-44FC-A0D2-B0A3EC601825",
        "User guid",
        "Guid utente")]
    public Guid? UserGuid { get; set; }

    /// <summary>
    /// Target user when the rule applies directly to a user.
    /// </summary>
    [ChillProperty(
        "644E03CA-C953-4926-91A6-A13B41FBF736",
        "User",
        "Utente")]
    public AuthUser? User { get; set; }

    /// <summary>
    /// Target role identifier when the rule applies through a role.
    /// </summary>
    [Column("role-guid")]
    [ChillProperty(
        "B11E2B66-DCD0-4740-B8EC-0FE4564D2F4C",
        "Role guid",
        "Guid ruolo")]
    public Guid? RoleGuid { get; set; }

    /// <summary>
    /// Target role when the rule applies through a role.
    /// </summary>
    [ChillProperty(
        "5051CCB4-8489-41FD-B917-E0CF68279AFD",
        "Role",
        "Ruolo")]
    public AuthRole? Role { get; set; }

    /// <summary>
    /// Permission effect applied by the rule.
    /// </summary>
    [Column("effect")]
    [ChillProperty(
        "6E256105-B9BD-4F3D-92A2-773D4C69FFCA",
        "Effect",
        "Effetto")]
    public PermissionEffect Effect { get; set; }

    /// <summary>
    /// Action controlled by the rule.
    /// </summary>
    [Column("action")]
    [ChillProperty(
        "D0F4EAE5-378A-4F2A-BE2A-AFD431E1D4D3",
        "Action",
        "Azione")]
    public PermissionAction Action { get; set; }

    /// <summary>
    /// Scope of the rule in the permission hierarchy.
    /// </summary>
    [Column("scope")]
    [ChillProperty(
        "CCAF54F5-17F8-4716-B1D8-68249468CC90",
        "Scope",
        "Ambito")]
    public PermissionScope Scope { get; set; }

    /// <summary>
    /// Module prefix targeted by the rule.
    /// </summary>
    [Column("module")]
    [ChillProperty(
        "6D45E883-3CDE-4BB3-A050-362C4101BD11",
        "Module",
        "Modulo")]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Entity name targeted by the rule when scope is entity or property.
    /// </summary>
    [Column("entity-name")]
    [ChillProperty(
        "7AD74E94-7EBA-4F84-9D3E-EFB7089FCF62",
        "Entity name",
        "Nome entita")]
    public string? EntityName { get; set; }

    /// <summary>
    /// Property name targeted by the rule when scope is property.
    /// </summary>
    [Column("property-name")]
    [ChillProperty(
        "AE17FA6B-4E8A-4327-A0FB-E73D887B69C6",
        "Property name",
        "Nome proprieta")]
    public string? PropertyName { get; set; }

    /// <summary>
    /// Indicates whether the property rule applies to all properties of the entity.
    /// </summary>
    [Column("applies-to-all-properties")]
    [ChillProperty(
        "D87BF786-E38D-45BC-BB57-982B1E8B5784",
        "Applies to all properties",
        "Applica a tutte le proprieta")]
    public bool AppliesToAllProperties { get; set; }

    /// <summary>
    /// Free text description of the permission rule.
    /// </summary>
    [Column("description")]
    [ChillProperty(
        "1CC8F852-314D-42A5-B89A-9530E4069F75",
        "Description",
        "Descrizione")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Datetime when the rule was created.
    /// </summary>
    [Column("created-utc")]
    [ChillProperty(
        "78766DBB-7BDB-4D26-84AB-3A5A08F0D31D",
        "Created utc",
        "Creato utc")]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public override string GetLabel(IChillContext Context)
    {
        return $"{Effect} {Action} {Module}";
    }
}
