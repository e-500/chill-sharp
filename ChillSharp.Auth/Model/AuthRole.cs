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
/// Defines a role that groups permission rules for multiple users.
/// </summary>
[ChillEntity(
    "A58CD663-78C5-4CF8-84F2-04342FD4B4C1",
    "Auth role",
    "Ruolo auth")]
[Table("auth-role")]
public class AuthRole : ChillEntity
{
    /// <summary>
    /// Unique identifier of the role.
    /// </summary>
    [Key]
    [Column("guid")]
    [ChillProperty(
        "BB09BECA-E4A0-4409-88CE-EB57E5189304",
        "Guid",
        "Guid")]
    public override Guid Guid { get; set; }

    /// <summary>
    /// Name of the role.
    /// </summary>
    [Column("name")]
    [ChillProperty(
        "79EB3C1C-95BD-4EF9-890E-2284E1D83DD4",
        "Name",
        "Nome")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the role.
    /// </summary>
    [Column("description")]
    [ChillProperty(
        "74E61624-C2A4-4626-913F-D71790931B8B",
        "Description",
        "Descrizione")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the role is active.
    /// </summary>
    [Column("is-active")]
    [ChillProperty(
        "F87599E2-A50D-4556-97EC-F5E8BA63A9E0",
        "Is active",
        "Attivo")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Menu hierarchy prefix used to filter the menu tree for users assigned to this role.
    /// </summary>
    [Column("menu-hierarchy")]
    [ChillProperty(
        "E46E84EB-94E3-4E27-B589-3988017F424A",
        "Menu hierarchy",
        "Gerarchia menu")]
    public string MenuHierarchy { get; set; } = string.Empty;

    /// <summary>
    /// User memberships associated with this role.
    /// </summary>
    [ChillProperty(
        "0DAA8BE8-E9F3-438B-8EF3-D284517C01F2",
        "User roles",
        "Ruoli utente")]
    public ICollection<AuthUserRole> UserRoles { get; set; } = new List<AuthUserRole>();

    public override string GetLabel(IChillContext Context)
    {
        return Name;
    }
}

