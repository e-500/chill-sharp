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
using ChillSharp.Dto;
using ChillSharp.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;

namespace ChillSharp
{
    /// <summary>
    /// Core engine for performing CRUD operations and executing queries on ChillSharp entities.
    /// 
    /// <para>
    /// <see cref="ChillEngine"/> provides a high-level interface for creating, updating,
    /// deleting, and querying <see cref="IChillEntity"/> objects using an EF Core <see cref="DbContext"/>.  
    /// It handles entity lifecycle events, including OnCreate, OnUpdate, OnDelete, and OnSelect,
    /// and manages computed properties such as <c>Label</c>, <c>ShortLabel</c>, and <c>FullTextContent</c>.
    /// </para>
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or redistribution must comply with the GPLv3 license terms.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    public class ChillEngine
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChillEngine"/> with the given Chill context.
        /// </summary>
        /// <param name="Contex">The Chill database context implementing <see cref="IChillContext"/>.</param>
        /// <param name="SchemaCache">Shared schema cache object across multiple contexts</param>
        public ChillEngine(IChillContext Contex) 
        {
            _Context = Contex;
        }

        internal IChillContext _Context;
        private IDbContextTransaction? _CurrentTransaction;

        #region TRANSACTION MANAGEMENT

        /// <summary>
        /// Starts a transaction
        /// </summary>
        public void BeginTransaction()
        {
            var db = (DbContext)_Context;
            if (_CurrentTransaction != null)
                throw new ChillException("Trying to open a second transaction");
            _CurrentTransaction = db.Database.BeginTransaction();
        }

        /// <summary>
        /// Commit an open transaction
        /// </summary>
        public void CommitTransaction()
        {
            var db = (DbContext)_Context;
            if (_CurrentTransaction == null)
                throw new ChillException("Trying to commit without an open transaction");
            db.Database.CommitTransaction();
            _CurrentTransaction = null;
        }

        /// <summary>
        /// Rollback an open transaction
        /// </summary>
        public void RollbackTransaction()
        {
            var db = (DbContext)_Context;
            if (_CurrentTransaction == null)
                throw new ChillException("Trying to rollback without an open transaction");
            db.Database.RollbackTransaction();
            _CurrentTransaction = null;
        }

        #endregion

        #region CRUD OPERATIONS

        /// <summary>
        /// Executes a query represented by an <see cref="IChillQuery{IChillEntity}"/> against the database.
        /// <para>
        /// The query is processed through <c>OnQuery</c>, <c>OnSort</c>, and <c>OnPaginate</c> methods
        /// before execution. After retrieval, <c>OnSelect</c> is called on each entity.
        /// </para>
        /// </summary>
        /// <param name="Query">The Chill query object defining filtering, sorting, and pagination.</param>
        /// <returns>A list of entities matching the query.</returns>
        public IChillEntity? Find(string ChillType, Guid Key)
        {
            // Find entity
            var ctx = (DbContext)_Context;
            var dbSet = _GetDbSet(ctx, ChillType);
            var Entity = _Find(dbSet, Key);
            if (Entity == null)
                return null;
            Entity.OnSelect(_Context);
            return Entity;
        }

        /// <summary>
        /// Executes a query represented by an <see cref="IChillQuery{IChillEntity}"/> against the database.
        /// <para>
        /// The query is processed through <c>OnQuery</c>, <c>OnSort</c>, and <c>OnPaginate</c> methods
        /// before execution. After retrieval, <c>OnSelect</c> is called on each entity.
        /// </para>
        /// </summary>
        /// <param name="Query">The Chill query object defining filtering, sorting, and pagination.</param>
        /// <returns>A list of entities matching the query.</returns>
        public List<IChillEntity> Query(IChillQuery<IChillEntity> Query)
        {
            var db = (DbContext)_Context;
            bool opTrans = false;
            if (_CurrentTransaction == null)
            {
                opTrans = true;
                db.Database.BeginTransaction();
            }
            try
            {
                var q = Query.OnQuery(_Context);
                q = Query.OnSort(_Context, q);
                q = Query.OnPaginate(_Context, q);
                var res = q.ToList();
                res.ForEach(x => x.OnSelect(_Context));
                if (opTrans)
                    db.Database.CommitTransaction();
                return res;
            }
            catch //(Exception ex)
            {
                if (opTrans)
                    db.Database.RollbackTransaction();
                throw;
            }
        }

        /// <summary>
        /// Creates a new entity in the database.
        /// <para>
        /// Executes <c>OnCreate</c> and <c>OnUpdate</c> lifecycle events, persists the entity,
        /// then updates computed properties (<c>Label</c>, <c>ShortLabel</c>, <c>FullTextContent</c>),
        /// and saves changes again.
        /// </para>
        /// </summary>
        /// <param name="Entity">The entity to create.</param>
        /// <returns>The created entity with updated lifecycle fields.</returns>
        public IChillEntity Create(IChillEntity Entity)
        {
            var db = (DbContext)_Context;

            bool opTrans = false;
            if (_CurrentTransaction == null)
            {
                opTrans = true;
                db.Database.BeginTransaction();
            }
            try
            {
                var entry = db.Entry(Entity);
                entry.State = EntityState.Added;
                entry.Entity.OnCreate(_Context);
                entry.Entity.OnUpdate(_Context);
                db.SaveChanges();
                entry.Entity.OnAfterUpdate(_Context);
                entry.Entity.Label = entry.Entity.GetLabel(_Context);
                entry.Entity.ShortLabel = entry.Entity.GetShortLabel(_Context);
                entry.Entity.FullTextContent = entry.Entity.GetFullTextContent(_Context);
                db.SaveChanges();
                if (opTrans)
                    db.Database.CommitTransaction();
                return entry.Entity;
            }
            catch //(Exception ex)
            {
                if (opTrans)
                    db.Database.RollbackTransaction();
                throw;
            }
        }

        /// <summary>
        /// Updates an existing entity in the database.
        /// <para>
        /// Executes <c>OnUpdate</c> and <c>OnAfterUpdate</c> lifecycle events, persists changes,
        /// and updates computed properties (<c>Label</c>, <c>ShortLabel</c>, <c>FullTextContent</c>).
        /// </para>
        /// </summary>
        /// <param name="Entity">The entity with updated values.</param>
        /// <returns>The updated entity with refreshed lifecycle fields.</returns>
        public IChillEntity Update(IChillEntity Entity)
        {
            var db = (DbContext)_Context;
            bool opTrans = false;
            if (_CurrentTransaction == null)
            {
                opTrans = true;
                db.Database.BeginTransaction();
            }
            try
            {
                var entry = db.Entry(Entity);
                entry.State = EntityState.Modified;
                entry.Entity.OnUpdate(_Context);
                db.SaveChanges();
                entry.Entity.OnAfterUpdate(_Context);
                
                // Update only if values change
                string tmp = entry.Entity.GetLabel(_Context);
                if (tmp != entry.Entity.Label)
                    entry.Entity.Label = tmp;
                tmp = entry.Entity.GetShortLabel(_Context);
                if (tmp != entry.Entity.ShortLabel)
                    entry.Entity.ShortLabel = tmp;
                tmp = entry.Entity.GetFullTextContent(_Context);
                if (tmp != entry.Entity.FullTextContent)
                    entry.Entity.FullTextContent = tmp;

                db.SaveChanges();
                if (opTrans)
                    db.Database.CommitTransaction();
                return entry.Entity;
            }
            catch //(Exception ex)
            {
                if (opTrans)
                    db.Database.RollbackTransaction();
                throw;
            }
        }

        /// <summary>
        /// Deletes an existing entity from the database.
        /// <para>
        /// Executes <c>OnDelete</c> and <c>OnAfterDelete</c> lifecycle events before and after deletion.
        /// </para>
        /// </summary>
        /// <param name="Entity">The entity to delete.</param>
        public void Delete(IChillEntity Entity)
        {
            var db = (DbContext)_Context;
            bool opTrans = false;
            if (_CurrentTransaction == null)
            {
                opTrans = true;
                db.Database.BeginTransaction();
            }
            try
            {
                var entry = db.Entry(Entity);
                entry.Entity.OnDelete(_Context);
                entry.State = EntityState.Deleted;
                db.SaveChanges();
                entry.Entity.OnAfterDelete(_Context);
                db.SaveChanges();
                if (opTrans)
                    db.Database.CommitTransaction();
            }
            catch //(Exception ex)
            {
                if (opTrans)
                    db.Database.RollbackTransaction();
                throw;
            }
        }

        #endregion

        #region PUBLIC ENTITY HELPERS

        /// <summary>
        /// Creates a new, detached instance of a Chill entity based on the provided
        /// Chill type identifier.
        /// </summary>
        /// <param name="ChillType">
        /// The short Chill type name used to resolve the fully qualified entity type
        /// within the current context assembly.
        /// </param>
        /// <returns>
        /// A newly instantiated <see cref="IChillEntity"/> that is not attached to
        /// the current <see cref="DbContext"/> and has no tracking state.
        /// </returns>
        /// <exception cref="ChillException">
        /// Thrown when the Chill type cannot be resolved or instantiated using
        /// the current context assembly.
        /// </exception>
        public IChillEntity ActivateDetachedChillEntity(string ChillType)
        {
            var res = ChillTypeResolver.ActivateType(_GetContextAssembly(), ChillType, _Context.GetChillTypePrefix());
            return (IChillEntity)res;
        }

        /// <summary>
        /// Instantiates a <see cref="IChillQuery{IChillEntity}"/> implementation based on the
        /// provided Chill type identifier.
        /// </summary>
        /// <param name="ChillType">
        /// The short Chill type name used to resolve the fully qualified query type
        /// within the current context assembly.
        /// </param>
        /// <returns>
        /// A newly created instance of a type implementing
        /// <see cref="IChillQuery{IChillEntity}"/>.
        /// </returns>
        /// <exception cref="ChillException">
        /// Thrown when the Chill type cannot be resolved or instantiated using
        /// the current context assembly.
        /// </exception>
        public IChillQuery<IChillEntity> ActivateChillQuery(string ChillType)
        {
            var res = ChillTypeResolver.ActivateType(_GetContextAssembly(), ChillType, _Context.GetChillTypePrefix());
            return (IChillQuery<IChillEntity>)res;
        }

        /// <summary>
        /// Creates and returns an instance of the specified chill type using the current context assembly.
        /// </summary>
        /// <remarks>The returned instance is created dynamically based on the provided chill type name.
        /// Ensure that the chill type exists and is accessible in the context assembly before calling this
        /// method.</remarks>
        /// <param name="ChillType">The name of the chill type to activate. Must be a valid type name recognized by the context assembly.</param>
        /// <returns>An object instance of the specified chill type. The returned object will be of the type corresponding to the
        /// provided chill type name.</returns>
        /// <exception cref="ChillException">Thrown if the specified chill type cannot be instantiated using the current context assembly.</exception>
        private object ActivateGenericChillType(string ChillType)
        {
            return ChillTypeResolver.ActivateType(_GetContextAssembly(), ChillType, _Context.GetChillTypePrefix());
        }

        #endregion

        #region CLASS INTERNAL HELPERS

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Assembly _GetContextAssembly()
        {
            return _Context.GetType().Assembly;
        }

        /// <summary>
        /// Builds the full ChillType name by applying the context's ChillType prefix if not already present, and ensuring it does not start or end with a dot.
        /// </summary>
        /// <param name="ChillType"></param>
        /// <returns></returns>
        /// <exception cref="ChillException"></exception>
        private string _PrepareFullChillType(string ChillType)
        {
            return ChillTypeResolver.PrepareFullChillType(ChillType, _Context.GetChillTypePrefix());
        }

        /// <summary>
        /// Return the DbSet for the given ChillType by activating a detached entity to determine the type, then calling DbContext.Set<TEntity>() via reflection.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="ChillType">ChillEntity type</param>
        /// <returns></returns>
        /// <exception cref="ChillException"></exception>
        private object _GetDbSet(DbContext ctx, string ChillType)
        {
            var entityType = ActivateDetachedChillEntity(ChillType).GetType();
            // Call DbContext.Set<TEntity>() dynamically
            var method = typeof(DbContext)
                .GetMethod("Set", Type.EmptyTypes)?
                .MakeGenericMethod(entityType);
            if (method == null)
                throw new ChillException("DbContext.Set(Type.EmptyTypes) method is not available");
            var dbSet = method.Invoke(ctx, null);
            if (dbSet == null)
                throw new ChillException($"DbSet for {ChillType} not found");

            return dbSet;
        }

        /// <summary>
        /// Find a chill entity by Guid by calling DbSet.Find(Guid) via reflection, then checking if the result implements IChillEntity and returning it.
        /// </summary>
        /// <param name="DbSet">DbSet where to look for</param>
        /// <param name="Guid">Primary key of the chill entity</param>
        /// <returns></returns>
        /// <exception cref="ChillException"></exception>
        private IChillEntity? _Find(object DbSet, Guid Guid)
        {
            // Try to get Find method
            var findMethod = DbSet.GetType().GetMethod("Find");

            if (findMethod == null)
                throw new ChillException($"Unable to locate Find() method on DbSet");

            // Invoke Find(Guid)
            var result = findMethod.Invoke(DbSet, new object[] { new object[] { Guid } });

            if (result == null)
                return null;

            // Check entity implements IChillEntity
            if (result is not IChillEntity entity)
                throw new ChillException($"Loaded entity is not an IChillEntity (actual type: {result.GetType().FullName})");

            return (IChillEntity)result;
        }

        #endregion
    }
}
