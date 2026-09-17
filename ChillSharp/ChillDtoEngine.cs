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
using ChillSharp.Api;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp
{
    /// <summary>
    /// Provides a Data Transfer Object (DTO) engine for interacting with ChillSharp entities.
    /// 
    /// <para>
    /// The <see cref="ChillDtoEngine"/> wraps a <see cref="ChillEngine"/> instance to perform CRUD operations
    /// and query execution using DTOs instead of direct EF Core entities.  
    /// It is designed to work with <see cref="ChillDtoEntity"/> and <see cref="ChillDtoQuery"/> objects,
    /// allowing safe, web-friendly, and serializable operations.
    /// </para>
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the terms of the 
    /// GNU Affero General Public License as published by the Free Software Foundation, 
    /// either version 3 of the License, or (at your option) any later version.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    public class ChillDtoEngine : IChillDtoEngine
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChillDtoEngine"/> using a Chill context.
        /// </summary>
        /// <param name="Context">The ChillSharp database context.</param>
        public ChillDtoEngine(
            IChillContext Context,
            IChillEntityChangeDispatcher? changeDispatcher = null)
        {
            _Engine = new ChillEngine(Context);
            _Context = Context;
            _ChangeDispatcher = changeDispatcher;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChillDtoEngine"/> using an existing Chill engine.
        /// </summary>
        /// <param name="Engine">The existing Chill engine instance.</param>
        public ChillDtoEngine(ChillEngine Engine) 
        {
            _Context = Engine._Context;
            _Engine = Engine;
            _ChangeDispatcher = null;
        }

        private IChillContext _Context;
        private ChillEngine _Engine;
        private readonly IChillEntityChangeDispatcher? _ChangeDispatcher;
        private readonly List<ChillEntityChangeNotification> _pendingEntityChanges = [];

        /// <summary>
        /// Starts a transaction
        /// </summary>
        public void BeginTransaction()
        {
            _Engine.BeginTransaction();
        }

        /// <summary>
        /// Commit an open transaction
        /// </summary>
        public void CommitTransaction()
        {
            _Engine.CommitTransaction();
            FlushEntityChanges();
        }

        /// <summary>
        /// Rollback an open transaction
        /// </summary>
        public void RollbackTransaction()
        {
            _Engine.RollbackTransaction();
            _pendingEntityChanges.Clear();
        }

        /// <summary>
        /// Executes a Chill query represented by a <see cref="ChillDtoQuery"/>, 
        /// populates its <see cref="ChillDtoQuery.Results"/> with the results, 
        /// and returns the executed DTO query.
        /// </summary>
        /// <param name="DtoQuery">The DTO query containing parameters and type information.</param>
        /// <returns>
        /// The same <see cref="ChillDtoQuery"/> with <see cref="ChillDtoQuery.Results"/> filled
        /// with the query results.
        /// </returns>
        /// <exception cref="ChillException">Thrown if the query cannot be activated or executed.</exception>
        public ChillDtoQuery Query(ChillDtoQuery DtoQuery)
        {
            // Activate ChillQuery object from ChillType
            var q = _Engine.ActivateChillQuery(DtoQuery.ChillType);

            // Check activated object
            if (q == null || !(q is IChillQuery<IChillEntity>))
                throw new ChillException($"{DtoQuery.ChillType} is not an IChillQuery");

            // Create the query object from Dto setting query params
            IChillQuery<IChillEntity> chillQuery = q as IChillQuery<IChillEntity>;
            DtoQuery.ToQuery(_Context, chillQuery);

            // Get and embed results
            DtoQuery.Results = _Engine.Query(chillQuery).Select(x => new ChillDtoEntity(_Context, x, DtoQuery.ResultProperties)).ToList();

            // Return executed query Dto
            return DtoQuery;
        }

        /// <summary>
        /// Finds a Chill entity based on the provided DTO and returns a corresponding DTO representation.
        /// </summary>
        /// <param name="DtoEntity">The DTO containing the type and GUID of the entity to find.</param>
        /// <returns>
        /// A <see cref="ChillDtoEntity"/> representing the found entity, or <c>null</c> if no matching entity exists.
        /// </returns>
        public ChillDtoEntity? Find(ChillDtoEntity DtoEntity)
        {
            // Find entity
            var e = _Engine.Find(DtoEntity.ChillType, DtoEntity.Guid);
            if (e == null)
                return null;
            return new ChillDtoEntity(_Context, e);
        }

        /// <summary>
        /// Applies autocomplete logic to an entity DTO without persisting it.
        /// </summary>
        /// <param name="DtoEntity">The DTO containing the entity state to autocomplete.</param>
        /// <returns>The autocompleted entity DTO.</returns>
        public ChillDtoEntity Autocomplete(ChillDtoEntity DtoEntity)
        {
            var ctx = (DbContext)_Context;
            var detachedEntity = _Engine.ActivateDetachedChillEntity(DtoEntity.ChillType);
            detachedEntity.Guid = DtoEntity.Guid;

            IChillEntity e = detachedEntity;
            if (DtoEntity.Guid != Guid.Empty)
            {
                var trackedEntity = ctx.Find(detachedEntity.GetType(), DtoEntity.Guid);
                if (trackedEntity is IChillEntity trackedChillEntity)
                {
                    e = trackedChillEntity;
                }
                else
                {
                    ctx.Entry(detachedEntity).State = EntityState.Added;
                }
            }
            else
            {
                ctx.Entry(detachedEntity).State = EntityState.Added;
            }

            DtoEntity.ToEntity(_Context, e);
            e = _Engine.Autocomplete(e);
            DtoEntity.FromEntity(_Context, e);
            return DtoEntity;
        }

        /// <summary>
        /// Applies autocomplete logic to a query DTO without executing it.
        /// </summary>
        /// <param name="DtoQuery">The DTO containing the query state to autocomplete.</param>
        /// <returns>The autocompleted query DTO.</returns>
        public ChillDtoQuery Autocomplete(ChillDtoQuery DtoQuery)
        {
            var q = _Engine.ActivateChillQuery(DtoQuery.ChillType);
            DtoQuery.ToQuery(_Context, q);
            q = _Engine.Autocomplete(q);
            DtoQuery.FromQuery(_Context, q);
            return DtoQuery;
        }

        /// <summary>
        /// Executes a generic full-text lookup directly against an entity type.
        /// </summary>
        /// <param name="DtoQuery">The DTO containing the target entity type and lookup parameters.</param>
        /// <returns>The same DTO with lookup results embedded.</returns>
        public ChillDtoQuery Lookup(ChillDtoQuery DtoQuery)
        {
            DtoQuery.Results = _Engine.Lookup(
                    DtoQuery.ChillType,
                    DtoQuery.Properties.GetValueOrDefault(nameof(ChillQuery.FullTextSearch))?.ToString(),
                    DtoQuery.Pagination)
                .Select(x => new ChillDtoEntity(_Context, x, DtoQuery.ResultProperties))
                .ToList();

            return DtoQuery;
        }

        /// <summary>
        /// Validates an entity DTO without persisting changes.
        /// </summary>
        /// <param name="DtoEntity">The DTO containing the entity state to validate.</param>
        /// <returns>The validation errors returned by the entity.</returns>
        public IEnumerable<ChillValidationError> Validate(ChillDtoEntity DtoEntity)
        {
            var e = ResolveEntityForDirtyOperation(DtoEntity);
            DtoEntity.ToEntity(_Context, e);
            return _Engine.Validate(e);
        }

        /// <summary>
        /// Validates a query DTO without executing it.
        /// </summary>
        /// <param name="DtoQuery">The DTO containing the query state to validate.</param>
        /// <returns>The validation errors returned by the query.</returns>
        public IEnumerable<ChillValidationError> Validate(ChillDtoQuery DtoQuery)
        {
            var q = _Engine.ActivateChillQuery(DtoQuery.ChillType);
            DtoQuery.ToQuery(_Context, q);
            return _Engine.Validate(q);
        }

        /// <summary>
        /// Creates a new entity in the database based on the provided DTO and returns the persisted DTO.
        /// </summary>
        /// <param name="DtoEntity">The DTO containing data for the new entity.</param>
        /// <returns>The newly created <see cref="ChillDtoEntity"/>.</returns>
        public ChillDtoEntity Create(ChillDtoEntity DtoEntity)
        {
            var e = _Engine.ActivateDetachedChillEntity(DtoEntity.ChillType);

            // Add to context with ADDED state
            var ctx = (DbContext)_Context;
            e.Guid = DtoEntity.Guid;
            if (e.Guid == Guid.Empty)
                DtoEntity.Guid = e.Guid = Guid.NewGuid();

            e = ctx.Entry(e).Entity;
            ctx.Entry(e).State = EntityState.Added;

            DtoEntity.ToEntity(_Context, e);
            e = _Engine.Create(e);
            DtoEntity.FromEntity(_Context, e);
            QueueEntityChange(e, ChillEntityChangeNotification.CreatedAction);
            return DtoEntity;
        }

        /// <summary>
        /// Updates an existing entity in the database based on the provided DTO and returns the updated DTO.
        /// </summary>
        /// <param name="DtoEntity">The DTO containing updated values for the entity.</param>
        /// <returns>The updated <see cref="ChillDtoEntity"/>.</returns>
        public ChillDtoEntity Update(ChillDtoEntity DtoEntity)
        {
            if (DtoEntity.Guid == Guid.Empty)
                throw new ChillException("Can't update without a valid guid");

            // Find entity
            var e = _Engine.Find(DtoEntity.ChillType, DtoEntity.Guid);

            // Check Find returned something
            if (e == null)
                throw new ChillException(
                    $"Entity of type {DtoEntity.ChillType} with Guid {DtoEntity.Guid} was not found"
                );

            DtoEntity.ToEntity(_Context, e);
            e = _Engine.Update(e);
            DtoEntity.FromEntity(_Context, e);
            QueueEntityChange(e, ChillEntityChangeNotification.UpdatedAction);
            return DtoEntity;
        }

        /// <summary>
        /// Deletes an existing entity in the database based on the provided DTO.
        /// </summary>
        /// <param name="DtoEntity">The DTO identifying the entity to delete.</param>
        public void Delete(ChillDtoEntity DtoEntity)
        {
            if (DtoEntity.Guid == Guid.Empty)
                throw new ChillException("Can't update without a valid guid");

            // Find entity
            var e = _Engine.Find(DtoEntity.ChillType, DtoEntity.Guid);

            // Check Find returned something
            if (e == null)
                throw new ChillException(
                    $"Entity of type {DtoEntity.ChillType} with Guid {DtoEntity.Guid} was not found"
                );

            DtoEntity.ToEntity(_Context, e);
            _Engine.Delete(e);
            QueueEntityChange(DtoEntity.ChillType, DtoEntity.Guid, ChillEntityChangeNotification.DeletedAction);
        }

        private void QueueEntityChange(IChillEntity entity, string action)
        {
            QueueEntityChange(
                ChillTypeResolver.NormalizeChillType(entity.GetType(), _Context.GetChillTypePrefix()),
                entity.Guid,
                action);
        }

        private void QueueEntityChange(string chillType, Guid guid, string action)
        {
            if (_ChangeDispatcher == null || string.IsNullOrWhiteSpace(chillType) || guid == Guid.Empty || string.IsNullOrWhiteSpace(action))
                return;

            _pendingEntityChanges.Add(new ChillEntityChangeNotification
            {
                ChillType = NormalizeChillType(chillType),
                Guid = guid,
                Action = action
            });

            if (!_Engine.HasOpenTransaction)
            {
                FlushEntityChanges();
            }
        }

        private void FlushEntityChanges()
        {
            if (_ChangeDispatcher == null || _pendingEntityChanges.Count == 0)
                return;

            var changes = _pendingEntityChanges.ToArray();
            _pendingEntityChanges.Clear();
            _ChangeDispatcher.DispatchAsync(changes).GetAwaiter().GetResult();
        }

        private string NormalizeChillType(string chillType)
        {
            var resolvedType = ChillTypeResolver.ResolveType(_Context.GetType().Assembly, chillType, _Context.GetChillTypePrefix());
            return ChillTypeResolver.NormalizeChillType(resolvedType, _Context.GetChillTypePrefix());
        }

        private IChillEntity ResolveEntityForDirtyOperation(ChillDtoEntity dtoEntity)
        {
            var ctx = (DbContext)_Context;
            var detachedEntity = _Engine.ActivateDetachedChillEntity(dtoEntity.ChillType);
            detachedEntity.Guid = dtoEntity.Guid;

            if (dtoEntity.Guid != Guid.Empty)
            {
                var trackedEntity = ctx.Find(detachedEntity.GetType(), dtoEntity.Guid);
                if (trackedEntity is IChillEntity trackedChillEntity)
                {
                    return trackedChillEntity;
                }
            }

            ctx.Entry(detachedEntity).State = EntityState.Added;
            return detachedEntity;
        }
    }
}
