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
    /// Preferred culture name shown for the user in the UI.
    /// </summary>
    [Column("display-culture-name")]
    [ChillProperty(
        "6A440406-6F41-4D8B-970C-8FD81F8850D5",
        "Display culture name",
        "Nome cultura visualizzata")]
    public string DisplayCultureName { get; set; } = string.Empty;

    /// <summary>
    /// Preferred time zone shown for the user in the UI.
    /// </summary>
    [Column("display-time-zone")]
    [ChillProperty(
        "67134A9C-9D63-4E4F-B6A3-283416B5A396",
        "Display time zone",
        "Fuso orario visualizzato")]
    public string DisplayTimeZone { get; set; } = string.Empty;

    /// <summary>
    /// Preferred date format shown for the user in the UI.
    /// </summary>
    [Column("display-date-format")]
    [ChillProperty(
        "8F734213-5F67-453B-90A6-BD95B7567F63",
        "Display date format",
        "Formato data visualizzato")]
    public string DisplayDateFormat { get; set; } = string.Empty;

    /// <summary>
    /// Preferred number format shown for the user in the UI.
    /// </summary>
    [Column("display-number-format")]
    [ChillProperty(
        "B73F5C36-DBA1-4C1C-B8FB-3BF75FAFCD9C",
        "Display number format",
        "Formato numerico visualizzato")]
    public string DisplayNumberFormat { get; set; } = string.Empty;

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
    /// Indicates whether the user can access the auth-management API and manage users, roles, and permission rules.
    /// </summary>
    [Column("can-manage-permissions")]
    [ChillProperty(
        "40846A6A-6B66-470E-902B-E95A8DF1B6DE",
        "Can manage permissions",
        "Può gestire permessi")]
    public bool CanManagePermissions { get; set; }

    /// <summary>
    /// Indicates whether the user can access the schema-management API and manage schema settings.
    /// </summary>
    [Column("can-manage-schema")]
    [ChillProperty(
        "58000C5A-26B5-485E-B769-56EA360B28A9",
        "Can manage schema",
        "Può gestire schema")]
    public bool CanManageSchema { get; set; }

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
