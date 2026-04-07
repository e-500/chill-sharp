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
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
    ///   <item><description>POST: api/chill/lookup → Executes a generic full-text lookup against an entity type.</description></item>
    ///   <item><description>POST: api/chill/find   → Retrieves a specific entity using a <see cref="ChillDtoEntity"/>.</description></item>
    ///   <item><description>POST: api/chill/create → Creates a new entity in the database.</description></item>
    ///   <item><description>POST: api/chill/update → Updates an existing entity.</description></item>
    ///   <item><description>POST: api/chill/delete → Deletes an existing entity.</description></item>
    ///   <item><description>POST: api/chill/autocomplete → Applies autocomplete logic to an entity or query DTO.</description></item>
    ///   <item><description>POST: api/chill/validate → Validates an entity or query DTO and returns validation errors.</description></item>
    /// </list>
    /// </para>
    ///
    /// <para>Each endpoint delegates execution to the injected <see cref="IChillDtoEngine"/> instance, 
    /// which handles the underlying database operations via an <see cref="IChillContext"/> implementation.</para>
    ///
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the terms of the 
    /// GNU Affero General Public License as published by the Free Software Foundation, 
    /// either version 3 of the License, or (at your option) any later version.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// ©️2025 Andrea Piovesan</para>
    /// </summary>
    [ApiController]
    [Route("api/chill")]
    public class ChillController : ControllerBase
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ChillController"/> class.
        /// </summary>
        /// <param name="ChillEngine">The Chill DTO engine instance used to perform data operations.</param>
        /// <param name="chillContext">The Chill context used to resolve entity metadata.</param>
        /// <param name="entityAclService">Optional entity ACL service used for authorization checks.</param>
        public ChillController(IChillDtoEngine ChillEngine, IChillContext chillContext, IChillEntityAclService? entityAclService = null)
        {
            _ce = ChillEngine;
            _context = chillContext;
            _entityAclService = entityAclService;
        }

        private readonly IChillDtoEngine _ce;
        private readonly IChillContext _context;
        private readonly IChillEntityAclService? _entityAclService;

        /// <summary>
        /// Executes a dynamic query using the Chill DTO engine and returns the result set.
        /// </summary>
        /// <param name="DtoQuery">The DTO query containing filters, parameters, and entity type information.</param>
        /// <returns>
        /// A collection of entities matching the specified query criteria, wrapped in an <see cref="IActionResult"/>.
        /// </returns>
        [HttpPost]
        [Route("query")]
        public async Task<IActionResult> Query(ChillDtoQuery DtoQuery, CancellationToken cancellationToken)
        {
            var authorizationResult = await EnsureEntityAccessAsync(DtoQuery.ChillType, ChillEntityAclAction.Query, isQueryType: true, cancellationToken);
            if (authorizationResult != null)
                return authorizationResult;
            return Ok(_ce.Query(DtoQuery));
        }

        /// <summary>
        /// Executes a generic full-text lookup against the entity type specified by <see cref="ChillDtoQuery.ChillType"/>.
        /// </summary>
        /// <param name="DtoQuery">The lookup DTO containing the entity type, search text, and requested result properties.</param>
        /// <returns>The lookup DTO with matching entities in <see cref="ChillDtoQuery.Results"/>.</returns>
        [HttpPost]
        [Route("lookup")]
        public async Task<IActionResult> Lookup(ChillDtoQuery DtoQuery, CancellationToken cancellationToken)
        {
            var authorizationResult = await EnsureEntityAccessAsync(DtoQuery.ChillType, ChillEntityAclAction.Query, isQueryType: false, cancellationToken);
            if (authorizationResult != null)
                return authorizationResult;
            return Ok(_ce.Lookup(DtoQuery));
        }

        /// <summary>
        /// Locates an entity in the database by its unique identifier using the Chill DTO engine.
        /// </summary>
        /// <param name="DtoEntity">The DTO entity containing the entity type and unique identifier (GUID key).</param>
        /// <returns>
        /// The located entity as a DTO, or <c>null</c> if no matching entity is found.
        /// </returns>
        [HttpPost]
        [Route("find")]
        public async Task<IActionResult> Find(ChillDtoEntity DtoEntity, CancellationToken cancellationToken)
        {
            var authorizationResult = await EnsureEntityAccessAsync(DtoEntity.ChillType, ChillEntityAclAction.Query, isQueryType: false, cancellationToken);
            if (authorizationResult != null)
                return authorizationResult;
            return Ok(_ce.Find(DtoEntity));
        }

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
        public async Task<IActionResult> Create(ChillDtoEntity DtoEntity, CancellationToken cancellationToken)
        {
            var authorizationResult = await EnsureEntityAccessAsync(DtoEntity.ChillType, ChillEntityAclAction.Create, isQueryType: false, cancellationToken);
            if (authorizationResult != null)
                return authorizationResult;
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
        public async Task<IActionResult> Update(ChillDtoEntity DtoEntity, CancellationToken cancellationToken)
        {
            var authorizationResult = await EnsureEntityAccessAsync(DtoEntity.ChillType, ChillEntityAclAction.Update, isQueryType: false, cancellationToken);
            if (authorizationResult != null)
                return authorizationResult;
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
        public async Task<IActionResult> Delete(ChillDtoEntity DtoEntity, CancellationToken cancellationToken)
        {
            var authorizationResult = await EnsureEntityAccessAsync(DtoEntity.ChillType, ChillEntityAclAction.Delete, isQueryType: false, cancellationToken);
            if (authorizationResult != null)
                return authorizationResult;
            _ce.Delete(DtoEntity);
            return Ok();
        }

        /// <summary>
        /// Applies autocomplete logic to an entity or query DTO and returns the updated payload.
        /// </summary>
        /// <param name="payload">The incoming entity or query DTO payload.</param>
        /// <returns>The autocompleted DTO payload.</returns>
        [HttpPost]
        [Route("autocomplete")]
        public async Task<IActionResult> Autocomplete(JsonElement payload, CancellationToken cancellationToken)
        {
            if (!TryGetPropertyIgnoreCase(payload, "ChillType", out var chillTypeElement))
                return BadRequest("ChillType is required.");

            var chillType = chillTypeElement.GetString();
            if (string.IsNullOrWhiteSpace(chillType))
                return BadRequest("ChillType is required.");

            var resolvedType = ChillTypeResolver.ResolveType(_context.GetType().Assembly, chillType, _context.GetChillTypePrefix());
            if (typeof(IChillQuery<IChillEntity>).IsAssignableFrom(resolvedType))
            {
                var authorizationResult = await EnsureEntityAccessAsync(chillType, ChillEntityAclAction.Query, isQueryType: true, cancellationToken);
                if (authorizationResult != null)
                    return authorizationResult;

                var dtoQuery = payload.Deserialize<ChillDtoQuery>(_jsonOptions);
                if (dtoQuery == null)
                    return BadRequest("Invalid autocomplete query payload.");

                return Ok(_ce.Autocomplete(dtoQuery));
            }

            var entityAuthorizationResult = await EnsureEntityAccessAsync(chillType, ChillEntityAclAction.Update, isQueryType: false, cancellationToken);
            if (entityAuthorizationResult != null)
                return entityAuthorizationResult;

            var dtoEntity = payload.Deserialize<ChillDtoEntity>(_jsonOptions);
            if (dtoEntity == null)
                return BadRequest("Invalid autocomplete entity payload.");

            return Ok(_ce.Autocomplete(dtoEntity));
        }

        /// <summary>
        /// Validates an entity or query DTO and returns the validation errors.
        /// </summary>
        /// <param name="payload">The incoming entity or query DTO payload.</param>
        /// <returns>The validation errors returned by the underlying type.</returns>
        [HttpPost]
        [Route("validate")]
        public async Task<IActionResult> Validate(JsonElement payload, CancellationToken cancellationToken)
        {
            if (!TryGetPropertyIgnoreCase(payload, "ChillType", out var chillTypeElement))
                return BadRequest("ChillType is required.");

            var chillType = chillTypeElement.GetString();
            if (string.IsNullOrWhiteSpace(chillType))
                return BadRequest("ChillType is required.");

            var resolvedType = ChillTypeResolver.ResolveType(_context.GetType().Assembly, chillType, _context.GetChillTypePrefix());
            if (typeof(IChillQuery<IChillEntity>).IsAssignableFrom(resolvedType))
            {
                var authorizationResult = await EnsureEntityAccessAsync(chillType, ChillEntityAclAction.Query, isQueryType: true, cancellationToken);
                if (authorizationResult != null)
                    return authorizationResult;

                var dtoQuery = payload.Deserialize<ChillDtoQuery>(_jsonOptions);
                if (dtoQuery == null)
                    return BadRequest("Invalid validate query payload.");

                return Ok(_ce.Validate(dtoQuery));
            }

            var entityAuthorizationResult = await EnsureEntityAccessAsync(chillType, ChillEntityAclAction.Update, isQueryType: false, cancellationToken);
            if (entityAuthorizationResult != null)
                return entityAuthorizationResult;

            var dtoEntity = payload.Deserialize<ChillDtoEntity>(_jsonOptions);
            if (dtoEntity == null)
                return BadRequest("Invalid validate entity payload.");

            return Ok(_ce.Validate(dtoEntity));
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
        public async Task<IActionResult> Chunk(List<ChillOperation> Chunk, CancellationToken cancellationToken)
        {
            foreach (var operation in Chunk)
            {
                var authorizationResult = await EnsureChunkOperationAccessAsync(operation, cancellationToken);
                if (authorizationResult != null)
                    return authorizationResult;
            }

            Chunk.ForEach(operation => operation.Execute(_ce));
            return Ok(Chunk);
        }

        /// <summary>
        /// Evaluates entity-level ACL requirements for every operation in a chunk before executing the batch.
        /// </summary>
        /// <param name="operation">The operation currently being validated.</param>
        /// <param name="cancellationToken">Token used to cancel the authorization check.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> representing a failed authorization result, or <see langword="null"/> when
        /// the operation is allowed.
        /// </returns>
        private async Task<IActionResult?> EnsureChunkOperationAccessAsync(ChillOperation operation, CancellationToken cancellationToken)
        {
            switch (operation.Verb?.ToLowerInvariant())
            {
                case ChillOperationVerb.QUERY when operation.Query != null:
                    return await EnsureEntityAccessAsync(operation.Query.ChillType, ChillEntityAclAction.Query, isQueryType: true, cancellationToken);
                case ChillOperationVerb.FIND when operation.Entity != null:
                    return await EnsureEntityAccessAsync(operation.Entity.ChillType, ChillEntityAclAction.Query, isQueryType: false, cancellationToken);
                case ChillOperationVerb.CREATE when operation.Entity != null:
                    return await EnsureEntityAccessAsync(operation.Entity.ChillType, ChillEntityAclAction.Create, isQueryType: false, cancellationToken);
                case ChillOperationVerb.UPDATE when operation.Entity != null:
                    return await EnsureEntityAccessAsync(operation.Entity.ChillType, ChillEntityAclAction.Update, isQueryType: false, cancellationToken);
                case ChillOperationVerb.DELETE when operation.Entity != null:
                    return await EnsureEntityAccessAsync(operation.Entity.ChillType, ChillEntityAclAction.Delete, isQueryType: false, cancellationToken);
                case ChillOperationVerb.AUTOCOMPLETE when operation.Query != null:
                    return await EnsureEntityAccessAsync(operation.Query.ChillType, ChillEntityAclAction.Query, isQueryType: true, cancellationToken);
                case ChillOperationVerb.AUTOCOMPLETE when operation.Entity != null:
                    return await EnsureEntityAccessAsync(operation.Entity.ChillType, ChillEntityAclAction.Update, isQueryType: false, cancellationToken);
                case ChillOperationVerb.VALIDATE when operation.Query != null:
                    return await EnsureEntityAccessAsync(operation.Query.ChillType, ChillEntityAclAction.Query, isQueryType: true, cancellationToken);
                case ChillOperationVerb.VALIDATE when operation.Entity != null:
                    return await EnsureEntityAccessAsync(operation.Entity.ChillType, ChillEntityAclAction.Update, isQueryType: false, cancellationToken);
            }

            return null;
        }

        /// <summary>
        /// Resolves the logical resource targeted by the request and performs an entity-level ACL check when available.
        /// </summary>
        /// <param name="chillType">The incoming Chill type or Chill query type.</param>
        /// <param name="action">The entity-level action to authorize.</param>
        /// <param name="isQueryType">
        /// Indicates whether <paramref name="chillType"/> represents a query type that must first be mapped back to
        /// its target entity type.
        /// </param>
        /// <param name="cancellationToken">Token used to cancel the authorization check.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> representing a failed authorization result, or <see langword="null"/> when
        /// authorization succeeds or no ACL service is configured.
        /// </returns>
        private async Task<IActionResult?> EnsureEntityAccessAsync(string chillType, ChillEntityAclAction action, bool isQueryType, CancellationToken cancellationToken)
        {
            if (_entityAclService == null || User.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var resource = isQueryType ? ResolveQueryResource(chillType) : ResolveEntityResource(chillType);
            var isAllowed = await _entityAclService.AuthorizeAsync(User, resource.Module, resource.EntityName, action, cancellationToken);
            return isAllowed ? null : Forbid();
        }

        /// <summary>
        /// Converts an entity Chill type into the logical ACL resource pair composed of module and entity name.
        /// </summary>
        /// <param name="chillType">The Chill entity type received from the API payload.</param>
        /// <returns>The logical module and entity name used by ChillSharp.Auth permission rules.</returns>
        private (string Module, string EntityName) ResolveEntityResource(string chillType)
        {
            var fullType = PrepareFullChillType(chillType);
            var prefix = _context.GetChillTypePrefix().TrimEnd('.');
            var shortType = fullType.StartsWith(prefix + ".", StringComparison.Ordinal) ? fullType.Substring(prefix.Length + 1) : fullType;
            var lastDot = shortType.LastIndexOf('.');
            if (lastDot < 0)
            {
                return ("General", shortType);
            }

            return (shortType.Substring(0, lastDot), shortType.Substring(lastDot + 1));
        }

        /// <summary>
        /// Resolves the entity targeted by a Chill query type so ACL checks can run against the entity resource.
        /// </summary>
        /// <param name="chillType">The Chill query type received from the API payload.</param>
        /// <returns>The logical module and entity name inferred from the query generic argument.</returns>
        private (string Module, string EntityName) ResolveQueryResource(string chillType)
        {
            var queryType = ChillTypeResolver.ResolveType(_context.GetType().Assembly, chillType, _context.GetChillTypePrefix());

            var entityType = ChillQueryTypeResolver.ResolveRelatedEntityType(queryType);
            if (entityType == null)
            {
                return ResolveEntityResource(chillType);
            }

            var prefix = _context.GetChillTypePrefix().TrimEnd('.');
            var entityFullName = entityType.FullName ?? entityType.Name;
            var shortType = entityFullName.StartsWith(prefix + ".", StringComparison.Ordinal) ? entityFullName.Substring(prefix.Length + 1) : entityFullName;
            var lastDot = shortType.LastIndexOf('.');
            if (lastDot < 0)
            {
                return ("General", shortType);
            }

            return (shortType.Substring(0, lastDot), shortType.Substring(lastDot + 1));
        }

        /// <summary>
        /// Normalizes a Chill type name and expands it to the full type name expected by the current Chill context.
        /// </summary>
        /// <param name="chillType">The incoming short or fully qualified Chill type string.</param>
        /// <returns>The normalized fully qualified type name.</returns>
        private string PrepareFullChillType(string chillType)
        {
            return ChillTypeResolver.PrepareFullChillType(chillType, _context.GetChillTypePrefix());
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement payload, string propertyName, out JsonElement value)
        {
            foreach (var property in payload.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

    }
}
