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
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

namespace ChillSharp.EF
{
    /// <summary>
    /// Entity Framework Core additional helper functions
    /// </summary>
    public static class ChillEntryExtension
    {
        /// <summary>
        /// Checks whether the reference navigation has an FK value. 
        /// It can also load the reference if requested with loadIfExist.
        /// Returns true if the FK is set (even if already loaded), otherwise false.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="reference"></param>
        /// <param name="loadIfExist">Request a load if exists</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static bool Exist<TEntity, TProperty>(
            this ReferenceEntry<TEntity, TProperty> reference,
            bool loadIfExist = false)
            where TEntity : class
            where TProperty : class
        {
            var entry = reference.EntityEntry;
            var navigationMetadata = reference.Metadata as INavigation
                ?? throw new InvalidOperationException(
                    $"Navigation '{reference.Metadata.Name}' is not a reference navigation.");

            var fkProps = navigationMetadata.ForeignKey.Properties;
            bool exists = fkProps.All(p => entry.Property(p.Name).CurrentValue != null);

            if (exists && loadIfExist && !reference.IsLoaded)
                reference.Load();

            return exists;
        }

        /// <summary>
        /// Checks whether the reference navigation has an FK value. 
        /// It can also load the reference if requested with loadIfExist.
        /// Returns true if the FK is set (even if already loaded), otherwise false.
        /// </summary>
        /// <param name="reference">The reference entry.</param>
        /// <param name="loadIfExist">Whether to load the reference if it exists.</param>
        /// <returns>True if FK is set, false otherwise.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static bool Exist(this ReferenceEntry reference, bool loadIfExist = false)
        {
            var entry = reference.EntityEntry;
            var navigationMetadata = reference.Metadata as INavigation
                ?? throw new InvalidOperationException(
                    $"Navigation '{reference.Metadata.Name}' is not a reference navigation.");

            // Get FK property metadata (composite keys supported)
            var fkProps = navigationMetadata.ForeignKey.Properties;

            // Check if all FK properties have values
            bool exists = fkProps.All(p => entry.Property(p.Name).CurrentValue != null);

            // Load if exists and not loaded
            if (exists && loadIfExist && !reference.IsLoaded)
                reference.Load();

            return exists;
        }

        /// <summary>
        /// Gets the FK value(s) for this reference navigation without loading it.
        /// Returns:
        /// - null if all FK values are null
        /// - the FK value itself for a single FK
        /// - a dictionary with property names/values for composite keys
        /// </summary>
        //public static object? ForeignKey<TEntity, TProperty>(
        //    this ReferenceEntry<TEntity, TProperty> reference)
        //    where TEntity : class
        //    where TProperty : class
        //{
        //    var entry = reference.EntityEntry;

        //    var navigationMetadata = reference.Metadata as INavigation
        //        ?? throw new InvalidOperationException($"Navigation '{reference.Metadata.Name}' is not a reference navigation.");

        //    var fkProps = navigationMetadata.ForeignKey.Properties;

        //    // If all FKs are null, return null
        //    if (fkProps.All(p => entry.Property(p.Name).CurrentValue == null))
        //        return null;

        //    // If single-column FK, return scalar
        //    if (fkProps.Count == 1)
        //        return entry.Property(fkProps[0].Name).CurrentValue;

        //    // If composite FK, return dictionary
        //    var values = fkProps.ToDictionary(p => p.Name, p => entry.Property(p.Name).CurrentValue);
        //    return values;
        //}

        //public static object? ForeignKey(this ReferenceEntry reference)
        //{
        //    var entry = reference.EntityEntry;

        //    var navigationMetadata = reference.Metadata as INavigation
        //        ?? throw new InvalidOperationException(
        //            $"Navigation '{reference.Metadata.Name}' is not a reference navigation.");

        //    var fkProps = navigationMetadata.ForeignKey.Properties;

        //    if (fkProps.All(p => entry.Property(p.Name).CurrentValue == null))
        //        return null;

        //    if (fkProps.Count == 1)
        //        return entry.Property(fkProps[0].Name).CurrentValue;

        //    return fkProps.ToDictionary(
        //        p => p.Name,
        //        p => entry.Property(p.Name).CurrentValue);
        //}

        public static void ClearForeignKey(this ReferenceEntry reference)
        {
            var entry = reference.EntityEntry;

            var navigationMetadata = reference.Metadata as INavigation
                ?? throw new InvalidOperationException(
                    $"Navigation '{reference.Metadata.Name}' is not a reference navigation.");

            var fkProps = navigationMetadata.ForeignKey.Properties;

            foreach (var fkProp in fkProps)
            {
                entry.Property(fkProp.Name).CurrentValue = null;
            }
        }

        /// <summary>
        /// Checks if the collection navigation represents an implicit many-to-many relationship.
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsImplicitManyToMany(this CollectionEntry collection)
        {
            return collection.Metadata is ISkipNavigation skipNav
                && skipNav.JoinEntityType.HasSharedClrType;
        }

        public static IQueryable<Dictionary<string, object?>> SelectRequiredProperties<TEntity>(
            this IQueryable<TEntity> Query,
            IEnumerable<ChillDtoProperty> RequiredProperties)
            where TEntity : class
        {
            var param = Expression.Parameter(typeof(TEntity), "e");

            var addMethod = typeof(Dictionary<string, object?>)
                .GetMethod("Add")!;

            var bindings = RequiredProperties.Select(p =>
                Expression.ElementInit(
                    addMethod,
                    Expression.Constant(p.PropertyName),
                    Expression.Convert(
                        Expression.Property(param, p.PropertyName),
                        typeof(object)
                    )
                )
            );

            var body = Expression.ListInit(
                Expression.New(typeof(Dictionary<string, object?>)),
                bindings
            );

            var lambda = Expression.Lambda<Func<TEntity, Dictionary<string, object?>>>(body, param);

            return Query.Select(lambda);
        }
    }
}
