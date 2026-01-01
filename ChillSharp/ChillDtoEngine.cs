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
using Microsoft.EntityFrameworkCore;
using System.Reflection;

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
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or redistribution must comply with the GPLv3 license terms.<br/>
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

        public ChillDtoEngine(IChillContext Context)
        {
            _Engine = new ChillEngine(Context);
            _Context = Context;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChillDtoEngine"/> using an existing Chill engine.
        /// </summary>
        /// <param name="Engine">The existing Chill engine instance.</param>
        public ChillDtoEngine(ChillEngine Engine) 
        {
            _Context = Engine._Context;
            _Engine = Engine;
        }

        private IChillContext _Context;
        private ChillEngine _Engine;

        private Assembly _GetContextAssembly()
        {
            return _Context.GetType().Assembly;
        }

        private string _PrepareFullChillType(string ChillType)
        {
            var chillTypePrefixWithDot = _Context.GetChillTypePrefix();
            if (!string.IsNullOrEmpty(chillTypePrefixWithDot) && !chillTypePrefixWithDot.EndsWith("."))
                chillTypePrefixWithDot += ".";
            if (string.IsNullOrEmpty(ChillType))
                throw new ChillException($"Entity type full name ({ChillType}) is invalid");
            if (ChillType.StartsWith("."))
                ChillType = ChillType.Substring(1);
            if (ChillType.EndsWith("."))
                ChillType = ChillType.Substring(ChillType.Length - 1);

            if (!ChillType.StartsWith(chillTypePrefixWithDot))
                ChillType = $"{chillTypePrefixWithDot}{ChillType}";
            return ChillType;
        }

        private IChillEntity _ActivateChillEntity(string ChillType)
        {
            string fullChillType = _PrepareFullChillType(ChillType);
            var res = _GetContextAssembly().CreateInstance(fullChillType);
            if (res == null)
                throw new ChillException($"Activator was unable to activate ({fullChillType}) using current context assembly");
            return (IChillEntity)res;
        }

        private IChillQuery<IChillEntity> _ActivateChillQuery(string ChillType)
        {
            string fullChillType = _PrepareFullChillType(ChillType);
            var res = _GetContextAssembly().CreateInstance(fullChillType);
            if (res == null)
                throw new ChillException($"Activator was unable to activate ({fullChillType}) using current context assembly");
            return (IChillQuery<IChillEntity>)res;
        }

        //private IChillEntity? _FindChillEntity(ChillDtoEntity DtoEntity)
        //{
        //    string fullChillType = _PrepareFullChillType(DtoEntity.ChillType);
        //    var entityClass = _GetContextAssembly().GetType(fullChillType);
        //    if (entityClass == null)
        //        throw new ChillException($"Activator was unable to activate ({fullChillType}) using current context assembly");
        //    var entitySelectMethod = entityClass.GetMethod("Find", BindingFlags.Public | BindingFlags.Static);
        //    if (entitySelectMethod == null)
        //        throw new ChillException($"Activator was unable to get ({fullChillType}.{entitySelectMethod}()) method using current context assembly");

        //    return (IChillEntity?)entitySelectMethod.Invoke(null, new object[] { _Context, DtoEntity.Guid });
        //}

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
        }

        /// <summary>
        /// Rollback an open transaction
        /// </summary>
        public void RollbackTransaction()
        {
            _Engine.RollbackTransaction();
        }

        /// <summary>
        /// Finds a Chill entity based on the provided DTO and returns a corresponding DTO representation.
        /// </summary>
        /// <param name="DtoEntity">The DTO containing the type and GUID of the entity to find.</param>
        /// <returns>
        /// A <see cref="ChillDtoEntity"/> representing the found entity, or <c>null</c> if no matching entity exists.
        /// </returns>
        //public ChillDtoEntity? Find(ChillDtoEntity DtoEntity)
        //{
        //    var Entity = _FindChillEntity(DtoEntity);
        //    if (Entity == null) 
        //        return null;
        //    return new ChillDtoEntity(_Context, Entity);
        //}

        /// <summary>
        /// Creates a new entity in the database based on the provided DTO and returns the persisted DTO.
        /// </summary>
        /// <param name="DtoEntity">The DTO containing data for the new entity.</param>
        /// <returns>The newly created <see cref="ChillDtoEntity"/>.</returns>
        public ChillDtoEntity Create(ChillDtoEntity DtoEntity)
        {
            var e = _ActivateChillEntity(DtoEntity.ChillType);

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
            return DtoEntity;
        }

        /// <summary>
        /// Updates an existing entity in the database based on the provided DTO and returns the updated DTO.
        /// </summary>
        /// <param name="DtoEntity">The DTO containing updated values for the entity.</param>
        /// <returns>The updated <see cref="ChillDtoEntity"/>.</returns>
        public ChillDtoEntity Update(ChillDtoEntity DtoEntity)
        {
            var e = _ActivateChillEntity(DtoEntity.ChillType);

            // Add to context with ADDED state
            var ctx = (DbContext)_Context;
            e.Guid = DtoEntity.Guid;
            if (e.Guid == Guid.Empty)
                throw new ChillException("Can't update without a valid guid");

            e = ctx.Entry(e).Entity;
            ctx.Entry(e).State = EntityState.Modified;

            DtoEntity.ToEntity(_Context, e);
            e = _Engine.Update(e);
            DtoEntity.FromEntity(_Context, e);
            return DtoEntity;
        }

        /// <summary>
        /// Deletes an existing entity in the database based on the provided DTO.
        /// </summary>
        /// <param name="DtoEntity">The DTO identifying the entity to delete.</param>
        public void Delete(ChillDtoEntity DtoEntity)
        {
            var e = _ActivateChillEntity(DtoEntity.ChillType);
            var ctx = (DbContext)_Context;
            e.Guid = DtoEntity.Guid;
            if (e.Guid == Guid.Empty)
                throw new ChillException("Can't update without a valid guid");

            e = ctx.Entry(e).Entity;

            DtoEntity.ToEntity(_Context, e);
            _Engine.Delete(e);
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
            var q = _ActivateChillQuery(DtoQuery.ChillType);
            
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
    }
}
