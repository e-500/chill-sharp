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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ChillSharp.Api.Controllers
{
    /// <summary>
    /// Provides REST endpoints for executing CRUD operations and queries through the ChillSharp DTO engine.
    /// <para>This controller acts as the main interface for interacting with the ChillSharp data layer.  
    /// It enables querying, finding, creating, updating, and deleting entities through generic DTOs, 
    /// allowing dynamic interaction with the database without tightly coupled models.</para>
    ///
    /// <para>The <see cref="ChillController"/> class exposes CRUD operations and query capabilities
    /// using the <see cref="IChillDtoEngine"/> service. It acts as the main controller for 
    /// ChillApi, enabling clients to query, create, update, and delete entities dynamically 
    /// via DTOs.</para>
    ///
    /// <para>Endpoints:<br/>
    /// <list type="bullet">
    ///   <item><description>POST: api/chill/query  → Executes a data query based on a <see cref="ChillDtoQuery"/>.</description></item>
    ///   <item><description>POST: api/chill/find   → Retrieves a specific entity using a <see cref="ChillDtoEntity"/>.</description></item>
    ///   <item><description>POST: api/chill/create → Creates a new entity in the database.</description></item>
    ///   <item><description>POST: api/chill/update → Updates an existing entity.</description></item>
    ///   <item><description>POST: api/chill/delete → Deletes an existing entity.</description></item>
    /// </list>
    /// </para>
    ///
    /// <para>Each endpoint delegates execution to the injected <see cref="IChillDtoEngine"/> instance, 
    /// which handles the underlying database operations via an <see cref="IChillContext"/> implementation.</para>
    ///
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or removal must comply with GPLv3 licensing terms.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// ©️2025 Andrea Piovesan</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ChillController : ControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChillController"/> class.
        /// </summary>
        /// <param name="ChillEngine">The Chill DTO engine instance used to perform data operations.</param>
        public ChillController(IChillDtoEngine ChillEngine)
        { 
            _ce = ChillEngine; 
        }

        private readonly IChillDtoEngine _ce;

        /// <summary>
        /// Executes a dynamic query using the Chill DTO engine and returns the result set.
        /// </summary>
        /// <param name="DtoQuery">The DTO query containing filters, parameters, and entity type information.</param>
        /// <returns>
        /// A collection of entities matching the specified query criteria, wrapped in an <see cref="IActionResult"/>.
        /// </returns>
        [HttpPost]
        [Route("query")]
        public IActionResult Query(ChillDtoQuery DtoQuery)
        {
            return Ok(_ce.Query(DtoQuery));
        }

        /// <summary>
        /// Locates an entity in the database by its unique identifier using the Chill DTO engine.
        /// </summary>
        /// <param name="DtoEntity">The DTO entity containing the entity type and unique identifier (GUID key).</param>
        /// <returns>
        /// The located entity as a DTO, or <c>null</c> if no matching entity is found.
        /// </returns>
        //[HttpPost]
        //[Route("find")]
        //public IActionResult Find(ChillDtoEntity DtoEntity)
        //{
        //    return Ok(_ce.Find(DtoEntity));
        //}

        /// <summary>
        /// Creates a new entity in the database using the Chill DTO engine and returns the saved version.
        /// </summary>
        /// <param name="DtoEntity">The DTO entity containing the data for the new record.</param>
        /// <returns>
        /// The newly created entity, recalculated and persisted, wrapped in an <see cref="IActionResult"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the entity type is not recognized or cannot be created.
        /// </exception>
        [HttpPost]
        [Route("create")]
        public IActionResult Create(ChillDtoEntity DtoEntity)
        {
            return Ok(_ce.Create(DtoEntity));
        }

        /// <summary>
        /// Updates an existing entity in the database using the Chill DTO engine and returns the updated version.
        /// </summary>
        /// <param name="DtoEntity">The DTO entity containing updated data for the existing record.</param>
        /// <returns>
        /// The updated entity, recalculated and persisted, wrapped in an <see cref="IActionResult"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the entity does not exist in the database.
        /// </exception>
        [HttpPost]
        [Route("update")]
        public IActionResult Update(ChillDtoEntity DtoEntity)
        {
            return Ok(_ce.Update(DtoEntity));
        }

        /// <summary>
        /// Deletes an existing entity from the database using the Chill DTO engine.
        /// </summary>
        /// <param name="DtoEntity">The DTO entity identifying the record to delete.</param>
        /// <returns>
        /// An HTTP 200 OK response if the operation completes successfully.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the entity does not exist in the database.
        /// </exception>
        [HttpPost]
        [Route("delete")]
        public IActionResult Delete(ChillDtoEntity DtoEntity)
        {
            _ce.Delete(DtoEntity);
            return Ok();
        }

        /// <summary>
        /// COMMENT: Execute a chunk of operations using transaction
        /// </summary>
        /// <param name="DtoEntity">The DTO entity identifying the record to delete.</param>
        /// <returns>
        /// An HTTP 200 OK response if the operation completes successfully.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the entity does not exist in the database.
        /// </exception>
        [HttpPost]
        [Route("chunk")]
        public IActionResult Chunk(List<ChillOperation> Chunk)
        {
            Chunk.ForEach(operation => operation.Execute(_ce));
            return Ok(Chunk);
        }
    }
}
