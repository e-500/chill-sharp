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

using System.Security.Claims;

namespace ChillSharp.Api;

/// <summary>
/// Describes the entity-level actions that can be authorized by ChillSharp.
/// </summary>
public enum ChillEntityAclAction
{
    /// <summary>
    /// Query or find access.
    /// </summary>
    Query = 1,

    /// <summary>
    /// Create access.
    /// </summary>
    Create = 2,

    /// <summary>
    /// Update access.
    /// </summary>
    Update = 3,

    /// <summary>
    /// Delete access.
    /// </summary>
    Delete = 4
}

/// <summary>
/// Defines the contract for entity-level ACL checks used by the Chill API controller.
/// </summary>
public interface IChillEntityAclService
{
    /// <summary>
    /// Evaluates whether the current principal can perform an action on a logical entity resource.
    /// </summary>
    /// <param name="principal">The authenticated principal to evaluate.</param>
    /// <param name="module">The logical module containing the entity.</param>
    /// <param name="entityName">The logical entity name.</param>
    /// <param name="action">The entity-level action to authorize.</param>
    /// <param name="cancellationToken">Token used to cancel the authorization request.</param>
    /// <returns><see langword="true"/> when the action is allowed; otherwise <see langword="false"/>.</returns>
    Task<bool> AuthorizeAsync(ClaimsPrincipal principal, string module, string entityName, ChillEntityAclAction action, CancellationToken cancellationToken = default);
}
