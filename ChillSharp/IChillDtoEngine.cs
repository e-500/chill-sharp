/*
 * Author: Andrea Piovesan
 * Year: 2025
 * License: GNU Affero General Public License (AGPL) version 3
 *
 * Disclaimer:
 * You are free to use, modify, and distribute it under the terms of the AGPL v3 license.
 * This code comes with no warranty; use it at your own risk.
 * 
 * For further information, please refer to README and LICENSE files.
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
        //ChillDtoEntity? Find(ChillDtoEntity DtoEntity);

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
    }
}
