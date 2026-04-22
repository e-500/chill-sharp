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

namespace ChillSharp.EF
{
    /// <summary>
    /// Abstract base class representing a Chill query object.
    /// 
    /// <para>
    /// This class provides a standard interface for defining queries against ChillSharp entities.
    /// It implements <see cref="IChillable"/>, <see cref="IChillValidable"/>, and <see cref="IChillQuery{T}"/>,
    /// offering default behaviors for filtering, sorting, pagination, and validation.  
    /// Concrete query classes should inherit from this base and implement <see cref="GetQueryable"/>.
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
    public class ChillQuery : IChillValidable, IChillQuery<IChillEntity>
    {
        /// <summary>
        /// Optional GUID used as a primary key for the entity.
        /// <para>
        /// Encourages offline-friendly entity creation and synchronization.  
        /// Can be decorated with the <c>[Key]</c> attribute for EF Core mapping.
        /// </para>
        /// </summary>
        [ChillProperty]
        public virtual Guid? Guid { get; set; }

        /// <summary>
        /// Optional free-text search string applied against entity full-text content.
        /// </summary>
        [ChillProperty(
            UniquePropertyKeyString: "4A01F180-A5DD-41CE-AD5B-58452F83192B",
            PrimaryLanguageLabel: "Full-text search",
            SecondaryLanguageLabel: "Ricerca full-text",
            MinLength: 0,
            MaxLength: 4096,
            MCPDescription = "Generic full-text search terms for this query. " +
                "Use this property when the user asks for broad keyword search instead of a specific structured filter. " +
                "ChillSharp splits the text on whitespace, trims empty tokens, ignores duplicate tokens case-insensitively, " +
                "normalizes each token with ChillFullTextSearchNormalizer, and applies AND matching against " +
                "IChillEntity.FullTextContent so every token must be present. Empty or whitespace-only values are ignored.")]
        public virtual string FullTextSearch { get; set; } = string.Empty;

        /// <summary>
        /// Optional pagination settings for the query results.
        /// </summary>
        public ChillPagination? Pagination { get; set; } = null;

        /// <summary>
        /// Optional ordering settings for the query results.
        /// </summary>
        public ChillOrdering? Ordering { get; set; } = new();

        #region IChillQuery implementation
        /// <summary>
        /// Applies additional filtering to the query.
        /// <para>
        /// By default, filters by <see cref="Guid"/> if provided.
        /// Override to add custom filtering logic.
        /// </para>
        /// </summary>
        /// <param name="Context">The active Chill database context.</param>
        /// <param name="Query">The query to filter.</param>
        /// <returns>The filtered <see cref="IQueryable{IChillEntity}"/>.</returns>
        public virtual IQueryable<IChillEntity> OnQuery(IChillContext Context)
        {
            var entityType = ChillQueryTypeResolver.ResolveRelatedEntityType(GetType());
            if (entityType == null)
                throw new ChillException($"Unable to resolve the entity type for query '{GetType().FullName ?? GetType().Name}'.");

            var query = ChillEngine.GetQueryable(Context, entityType);
            if (Guid.HasValue)
                query = query.Where(x => x.Guid == Guid.Value);

            return query;
        }

        /// <summary>
        /// Applies tokenized full-text filtering against <see cref="IChillEntity.FullTextContent"/>.
        /// Each token must be present for the entity to match.
        /// </summary>
        /// <param name="Context">The active Chill database context.</param>
        /// <param name="Query">The query to filter.</param>
        /// <returns>The filtered <see cref="IQueryable{IChillEntity}"/>.</returns>
        public virtual IQueryable<IChillEntity> OnSearch(IChillContext Context, IQueryable<IChillEntity> Query)
        {
            if (string.IsNullOrWhiteSpace(FullTextSearch))
                return Query;

            var tokens = FullTextSearch
                .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var token in tokens)
            {
                var currentToken = ChillFullTextSearchNormalizer.Normalize(token);
                Query = Query.Where(x => !string.IsNullOrEmpty(x.FullTextContent) && x.FullTextContent.Contains(currentToken));
            }

            return Query;
        }
        
        /// <summary>
        /// Applies default ordering to the query.
        /// </summary>
        /// <param name="Context">The active Chill database context.</param>
        /// <param name="Query">The query to order.</param>
        /// <returns>The ordered <see cref="IQueryable{IChillEntity}"/>.</returns>
        public virtual IQueryable<IChillEntity> OnOrderingBy(IChillContext Context, IQueryable<IChillEntity> Query)
        {
            var entityType = ChillQueryTypeResolver.ResolveRelatedEntityType(GetType()) ?? typeof(IChillEntity);
            return ChillOrderingApplier.ApplyOrdering(Query, Ordering, entityType);
        }

        /// <summary>
        /// Applies pagination to the query results if <see cref="Pagination"/> is set.
        /// </summary>
        /// <param name="Context">The active Chill database context.</param>
        /// <param name="Query">The query to paginate.</param>
        /// <returns>The paginated <see cref="IQueryable{IChillEntity}"/>.</returns>
        public virtual IQueryable<IChillEntity> OnPaginate(IChillContext Context, IQueryable<IChillEntity> Query)
        {
            if (Pagination == null)
                return Query;

            return Query.Skip((Pagination.Page - 1) * Pagination.PageResults).Take((Pagination.PageResults));
        }
        #endregion

        #region IChillValidation implementation
        /// <summary>
        /// Called to fill or adjust entity fields based on the current ("dirty") state.
        /// <para>
        /// Typically invoked by <c>AUTOCOMPLETE()</c> before validation or persistence.
        /// </para>
        /// </summary>
        /// <param name="Context">The active Chill database context.</param>
        public virtual void OnAutocomplete(IChillContext Context) { }

        /// <summary>
        /// Validates entity fields before an update or persistence operation.
        /// <para>
        /// Called by:
        /// </para>
        /// <list type="bullet">
        ///   <item><description><c>VALIDATE()</c> — returns validation issues without throwing exceptions.</description></item>
        ///   <item><description><c>UPDATE()</c> — throws an exception if validation fails.</description></item>
        /// </list>
        /// </summary>
        /// <param name="Context">The active Chill database context.</param>
        /// <returns>A collection of <see cref="ChillValidationError"/> representing validation issues.</returns>
        public virtual IEnumerable<ChillValidationError> OnValidation(IChillContext Context) { return new List<ChillValidationError>(); }

        /// <summary>
        /// Returns optional validation message definitions that can translate GUID-based
        /// DataAnnotations error messages using ChillSharp primary/secondary texts.
        /// </summary>
        /// <param name="Context">The active Chill database context.</param>
        /// <returns>The validation message definitions available for the query.</returns>
        public virtual IEnumerable<ChillValidationMessageDefinition> GetValidationMessageDefinitions(IChillContext Context) { return new List<ChillValidationMessageDefinition>(); }

        // ChillSharp invokes validation through the IChillValidable interface, not through the concrete query.
        // This keeps DataAnnotations validation for Chill properties always enabled while preserving the simple
        // user override surface on the public virtual OnValidation(...) method.
        IEnumerable<ChillValidationError> IChillValidable.OnValidation(IChillContext Context)
        {
            var errors = new List<ChillValidationError>();
            errors.AddRange(ChillDataAnnotationsValidator.ValidateChillProperties(this, Context, GetValidationMessageDefinitions(Context)));
            errors.AddRange(OnValidation(Context));
            return errors;
        }
        #endregion
    }
}
