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
/// Stores the application users that can receive direct permissions and role assignments.
/// </summary>
[ChillEntity(
    "F973E77C-EF3D-4B45-9BE3-7E3923DC5634",
    "Auth user",
    "Utente auth")]
[Table("auth-user")]
public class AuthUser : ChillEntity
{
    /// <summary>
    /// Unique identifier of the user.
    /// </summary>
    [Key]
    [Column("guid")]
    [ChillProperty(
        "04CA4B7A-0A92-44A0-B4D0-C5934F82B9A6",
        "Guid",
        "Guid")]
    public override Guid Guid { get; set; }

    /// <summary>
    /// External identity provider identifier for the user.
    /// </summary>
    [Column("external-id")]
    [ChillProperty(
        "8393B92D-8341-4E18-ABCD-25540FC9B61E",
        "External id",
        "Id esterno")]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Login name used by the user.
    /// </summary>
    [Column("user-name")]
    [ChillProperty(
        "D5E149AF-3F0A-4059-B15A-58AFAB8E3E03",
        "User name",
        "Nome utente")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Human readable display name for the user.
    /// </summary>
    [Column("display-name")]
    [ChillProperty(
        "2D0447D9-4471-4C4B-819A-6C2EE67E73D6",
        "Display name",
        "Nome visualizzato")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the user is active.
    /// </summary>
    [Column("is-active")]
    [ChillProperty(
        "277BBA8E-5542-4C13-A03C-D82DB9B581CF",
        "Is active",
        "Attivo")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Role memberships assigned to the user.
    /// </summary>
    [ChillProperty(
        "7D665B44-C4A1-4AF1-88CF-8D272DC4C2D9",
        "User roles",
        "Ruoli utente")]
    public ICollection<AuthUserRole> UserRoles { get; set; } = new List<AuthUserRole>();

    public override string GetLabel(IChillContext Context)
    {
        return string.IsNullOrWhiteSpace(DisplayName) ? UserName : DisplayName;
    }
}
