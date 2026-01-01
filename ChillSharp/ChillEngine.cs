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


using ChillSharp.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
        public ChillEngine(IChillContext Contex) 
        {
            _Context = Contex;
        }

        internal IChillContext _Context;
        private IDbContextTransaction? _CurrentTransaction;

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
            catch (Exception ex)
            {
                if (opTrans)
                    db.Database.RollbackTransaction();
                throw ex;
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
            catch (Exception ex)
            {
                if (opTrans)
                    db.Database.RollbackTransaction();
                throw ex;
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
            catch (Exception ex)
            {
                if (opTrans)
                    db.Database.RollbackTransaction();
                throw ex;
            }
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
            catch (Exception ex)
            {
                if (opTrans)
                    db.Database.RollbackTransaction();
                throw ex;
            }
        }
    }
}
