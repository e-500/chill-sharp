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
/// Stores refresh-token sessions used to renew short-lived access tokens for authenticated API clients.
/// </summary>
[ChillEntity(
    "96C78BB0-9E15-45E5-9BC9-2F6D3B8A8C06",
    "Auth refresh token",
    "Refresh token auth")]
[Table("auth-refresh-token")]
public class AuthRefreshToken : ChillEntity
{
    /// <summary>
    /// Unique identifier of the persisted refresh-token session.
    /// </summary>
    [Key]
    [Column("guid")]
    [ChillProperty(
        "5901F242-381C-4F54-935B-21A34613A9EE",
        "Guid",
        "Guid")]
    public override Guid Guid { get; set; }

    /// <summary>
    /// Identifier of the ASP.NET Core Identity user owning the session.
    /// </summary>
    [Column("identity-user-id")]
    [ChillProperty(
        "3550E899-46AF-4125-82C8-A3136E3A7DD3",
        "Identity user id",
        "Id utente identity")]
    public string IdentityUserId { get; set; } = string.Empty;

    /// <summary>
    /// User name snapshot stored with the session for fast claim reconstruction.
    /// </summary>
    [Column("user-name")]
    [ChillProperty(
        "9988F96A-C3EE-42C5-9BD5-E9B9E7054B9C",
        "User name",
        "Nome utente")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the refresh token returned to the client.
    /// </summary>
    [Column("token-hash")]
    [ChillProperty(
        "02D17003-EB3D-4517-91E3-C6B59555AD67",
        "Token hash",
        "Hash token")]
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Datetime when the refresh-token session was created.
    /// </summary>
    [Column("created-utc")]
    [ChillProperty(
        "C477E0C9-0C48-4A3F-8D5C-2D5F25B25A53",
        "Created utc",
        "Creato utc")]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Datetime when the refresh token expires.
    /// </summary>
    [Column("expires-utc")]
    [ChillProperty(
        "E1C020D3-48ED-4B1A-BC61-ED6FB555A990",
        "Expires utc",
        "Scade utc")]
    public DateTime ExpiresUtc { get; set; }

    /// <summary>
    /// Datetime when the refresh token was last used to mint a new access token.
    /// </summary>
    [Column("last-used-utc")]
    [ChillProperty(
        "9A07B16B-6294-475A-A69A-C91335518B92",
        "Last used utc",
        "Ultimo uso utc")]
    public DateTime? LastUsedUtc { get; set; }

    /// <summary>
    /// Datetime when the session was revoked and can no longer be refreshed.
    /// </summary>
    [Column("revoked-utc")]
    [ChillProperty(
        "735A4CB1-EA6D-4676-B888-07E33F84FC3A",
        "Revoked utc",
        "Revocato utc")]
    public DateTime? RevokedUtc { get; set; }

    public override string GetLabel(IChillContext Context)
    {
        return $"{UserName} refresh token";
    }
}
