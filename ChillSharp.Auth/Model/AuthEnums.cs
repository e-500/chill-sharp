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

namespace ChillSharp.Auth.Model;

/// <summary>
/// Defines whether a permission rule grants or forbids an action.
/// </summary>
public enum PermissionEffect
{
    /// <summary>
    /// Grants access to the targeted action.
    /// </summary>
    Allow = 1,

    /// <summary>
    /// Explicitly blocks access to the targeted action.
    /// </summary>
    Deny = 2
}

/// <summary>
/// Lists the actions that can be controlled by the authorization model.
/// </summary>
public enum PermissionAction
{
    /// <summary>
    /// Allows all actions, but with lower precedence than an exact action rule.
    /// </summary>
    FullControl = 0,

    /// <summary>
    /// Allows querying entity data.
    /// </summary>
    Query = 1,

    /// <summary>
    /// Allows creating new entity records.
    /// </summary>
    Create = 2,

    /// <summary>
    /// Allows updating existing entity records.
    /// </summary>
    Update = 3,

    /// <summary>
    /// Allows deleting entity records.
    /// </summary>
    Delete = 4,

    /// <summary>
    /// Allows seeing a specific property in query results.
    /// </summary>
    See = 5,

    /// <summary>
    /// Allows modifying a specific property during create or update.
    /// </summary>
    Modify = 6
}

/// <summary>
/// Identifies the hierarchy level targeted by a permission rule.
/// </summary>
public enum PermissionScope
{
    /// <summary>
    /// Targets a module and its submodules.
    /// </summary>
    Module = 1,

    /// <summary>
    /// Targets a specific entity within a module.
    /// </summary>
    Entity = 2,

    /// <summary>
    /// Targets a specific property within an entity.
    /// </summary>
    Property = 3
}
