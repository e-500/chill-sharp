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

using ChillSharp.Schema.Contracts;

namespace ChillSharp.Schema;

/// <summary>
/// Defines the contract for loading and persisting Chill DTO schemas independently from the core engine.
/// </summary>
public interface IChillSchemaService
{
    /// <summary>
    /// Loads or builds the schema for a Chill type and view code.
    /// </summary>
    /// <param name="chillType">The logical Chill type identifier.</param>
    /// <param name="chillViewCode">The logical Chill view code.</param>
    /// <param name="cultureName">Optional explicit culture used to localize schema labels.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The resolved schema, or <see langword="null"/> when no schema can be resolved.</returns>
    Task<ChillDtoSchema?> GetSchemaAsync(string chillType, string chillViewCode, string? cultureName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a schema definition.
    /// </summary>
    /// <param name="schema">The schema to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted schema.</returns>
    Task<ChillDtoSchema> SetSchemaAsync(ChillDtoSchema schema, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the persisted runtime options for a Chill entity type.
    /// </summary>
    /// <param name="chillType">The logical Chill type identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The resolved entity options, falling back to defaults when no persisted row exists.</returns>
    Task<ChillDtoEntityOptions> GetEntityOptionsAsync(string chillType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists runtime options for a Chill entity type.
    /// </summary>
    /// <param name="entityOptions">The entity options to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted entity options.</returns>
    Task<ChillDtoEntityOptions> SetEntityOptionsAsync(ChillDtoEntityOptions entityOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the root menu items or the direct children of a menu item.
    /// </summary>
    /// <param name="parentGuid">Optional parent menu-item identifier. <see langword="null"/> returns root nodes.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching menu items ordered for UI consumption.</returns>
    Task<IReadOnlyList<ChillDtoMenuItem>> GetMenuAsync(Guid? parentGuid = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a menu item.
    /// </summary>
    /// <param name="menuItem">The menu item to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted menu item.</returns>
    Task<ChillDtoMenuItem> SetMenuAsync(ChillDtoMenuItem menuItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a menu item and all of its descendants.
    /// </summary>
    /// <param name="menuItemGuid">The identifier of the menu item to delete.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task DeleteMenuAsync(Guid menuItemGuid, CancellationToken cancellationToken = default);
}
