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
using System.ComponentModel.DataAnnotations.Schema;

namespace ChillSharp.Auth.Model;

/// <summary>
/// Links users to roles for permission inheritance.
/// </summary>
[ChillEntity(
    "391D0A80-D073-45B5-9A7A-F24A91F65B1F",
    "Auth user role",
    "Ruolo utente auth")]
[Table("auth-user-role")]
public class AuthUserRole
{
    /// <summary>
    /// Identifier of the user in the membership.
    /// </summary>
    [Column("user-guid")]
    [ChillProperty(
        "2C784E32-1345-4F59-8F9C-D65AF419BD23",
        "User guid",
        "Guid utente")]
    public Guid UserGuid { get; set; }

    /// <summary>
    /// User participating in the membership.
    /// </summary>
    [ChillProperty(
        "1A0F6C52-2A9E-4A8A-9E85-64B96A830757",
        "User",
        "Utente")]
    public AuthUser User { get; set; } = null!;

    /// <summary>
    /// Identifier of the role in the membership.
    /// </summary>
    [Column("role-guid")]
    [ChillProperty(
        "FD86C7F0-69B7-4A7E-BD56-0C3159F80D99",
        "Role guid",
        "Guid ruolo")]
    public Guid RoleGuid { get; set; }

    /// <summary>
    /// Role participating in the membership.
    /// </summary>
    [ChillProperty(
        "374A66C3-FF95-4736-970C-CE5F98FC1C84",
        "Role",
        "Ruolo")]
    public AuthRole Role { get; set; } = null!;

    /// <summary>
    /// Datetime when the membership was assigned.
    /// </summary>
    [Column("assigned-utc")]
    [ChillProperty(
        "8A333F89-BED2-4C7F-AF84-9F5C92067F8B",
        "Assigned utc",
        "Assegnato utc")]
    public DateTime AssignedUtc { get; set; } = DateTime.UtcNow;
}
