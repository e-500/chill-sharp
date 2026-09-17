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
/// Stores dynamically registered OAuth clients so MCP clients can reconnect after server restarts.
/// </summary>
[ChillEntity(
    "3EAD674A-0F78-4A61-A0C5-AF244E994318",
    "Auth OAuth client",
    "Client OAuth auth")]
[Table("auth-oauth-client")]
public class AuthOAuthClient : ChillEntity
{
    /// <summary>
    /// Unique identifier of the persisted OAuth client registration.
    /// </summary>
    [Key]
    [Column("guid")]
    [ChillProperty(
        "C8F11E33-8F7F-4B7E-A397-26634C5E18A1",
        "Guid",
        "Guid")]
    public override Guid Guid { get; set; }

    /// <summary>
    /// Public OAuth client identifier returned by dynamic client registration.
    /// </summary>
    [Column("client-id")]
    [ChillProperty(
        "E9672D7C-1F39-4E4C-B2F7-20E9C3674DE9",
        "Client id",
        "Id client")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Optional display name supplied by the registering OAuth client.
    /// </summary>
    [Column("client-name")]
    [ChillProperty(
        "4B2B95D4-A03F-4A65-B123-BF334F428381",
        "Client name",
        "Nome client")]
    public string? ClientName { get; set; }

    /// <summary>
    /// JSON-serialized list of redirect URIs registered for this client.
    /// </summary>
    [Column("redirect-uris-json")]
    [ChillProperty(
        "98F1259A-6673-411A-9CC9-523CA4741E77",
        "Redirect URIs json",
        "Json URI redirect")]
    public string RedirectUrisJson { get; set; } = "[]";

    /// <summary>
    /// Unix timestamp, in seconds, when the client id was issued.
    /// </summary>
    [Column("client-id-issued-at")]
    [ChillProperty(
        "321DD3DF-185A-4D1B-99CF-46E0EBF0D061",
        "Client id issued at",
        "Id client emesso il")]
    public long ClientIdIssuedAt { get; set; }

    public override string GetLabel(IChillContext Context)
    {
        return string.IsNullOrWhiteSpace(ClientName) ? ClientId : ClientName;
    }
}
