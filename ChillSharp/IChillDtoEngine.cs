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

using ChillSharp.Dto;
using ChillSharp.EF;
using System.Reflection;

namespace ChillSharp
{
    /// <summary>
    /// Defines the contract for a DTO engine that interacts with ChillSharp entities via DTOs.
    /// 
    /// <para>
    /// The <see cref="IChillDtoEngine"/> provides high-level CRUD and query operations on 
    /// <see cref="ChillDtoEntity"/> and <see cref="ChillDtoQuery"/> objects, allowing safe, 
    /// web-friendly manipulation of entities without working directly with EF Core entities.  
    /// This interface is typically implemented by <see cref="ChillDtoEngine"/>.
    /// </para>
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or redistribution must comply with the GPLv3 license terms.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    public interface IChillDtoEngine
    {
        /// <summary>
        /// Starts a transaction
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// Commit an open transaction
        /// </summary>
        void CommitTransaction();

        /// <summary>
        /// Rollback an open transaction
        /// </summary>
        void RollbackTransaction();

        /// <summary>
        /// Executes a query represented by a <see cref="ChillDtoQuery"/> and populates its results.
        /// </summary>
        /// <param name="DtoQuery">The DTO query containing parameters and type information.</param>
        /// <returns>
        /// The same <see cref="ChillDtoQuery"/> with its <c>Results</c> property filled with matching entities.
        /// </returns>
        ChillDtoQuery Query(ChillDtoQuery DtoQuery);

        /// <summary>
        /// Finds an existing entity in the database based on the provided DTO.
        /// </summary>
        /// <param name="DtoEntity">The DTO identifying the entity to find.</param>
        /// <returns>
        /// A <see cref="ChillDtoEntity"/> representing the entity if found, or <c>null</c> if not found.
        /// </returns>
        ChillDtoEntity? Find(ChillDtoEntity DtoEntity);

        /// <summary>
        /// Creates a new entity in the database from the provided DTO and returns the persisted DTO.
        /// </summary>
        /// <param name="DtoEntity">The DTO containing data for the new entity.</param>
        /// <returns>The created <see cref="ChillDtoEntity"/> with updated values from the database.</returns>
        ChillDtoEntity Create(ChillDtoEntity DtoEntity);

        /// <summary>
        /// Updates an existing entity in the database using the values from the provided DTO.
        /// </summary>
        /// <param name="DtoEntity">The DTO containing updated values for the entity.</param>
        /// <returns>The updated <see cref="ChillDtoEntity"/> reflecting changes persisted in the database.</returns>
        ChillDtoEntity Update(ChillDtoEntity DtoEntity);

        /// <summary>
        /// Deletes the entity identified by the provided DTO from the database.
        /// </summary>
        /// <param name="DtoEntity">The DTO identifying the entity to delete.</param>
        void Delete(ChillDtoEntity DtoEntity);

        /// <summary>
        /// Retrieves the schema definition for a specified chill type and view code.
        /// </summary>
        /// <param name="ChillType">The identifier representing the chill type for which the schema is requested. Cannot be null or empty.</param>
        /// <param name="ChillViewCode">The code representing the specific view of the chill type. Cannot be null or empty.</param>
        /// <returns>A ChillDtoSchema object containing the schema definition for the specified chill type and view code. Returns
        /// null if no matching schema is found.</returns>
        ChillDtoSchema? GetSchema(string ChillType, string ChillViewCode);

        /// <summary>
        /// Sets the schema definition used for DTO operations and returns the previous schema.
        /// </summary>
        /// <remarks>Use this method to change the schema used for DTO serialization or validation. Changing the
        /// schema may affect subsequent DTO processing.</remarks>
        /// <param name="Schema">The schema to be applied for DTO operations. Cannot be null.</param>
        /// <returns>The previous schema definition before the update. Returns null if no schema was previously set.</returns>
        ChillDtoSchema SetSchema(ChillDtoSchema Schema);
    }
}
