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


using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

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
        public static object? ForeignKey<TEntity, TProperty>(
            this ReferenceEntry<TEntity, TProperty> reference)
            where TEntity : class
            where TProperty : class
        {
            var entry = reference.EntityEntry;

            var navigationMetadata = reference.Metadata as INavigation
                ?? throw new InvalidOperationException($"Navigation '{reference.Metadata.Name}' is not a reference navigation.");

            var fkProps = navigationMetadata.ForeignKey.Properties;

            // If all FKs are null, return null
            if (fkProps.All(p => entry.Property(p.Name).CurrentValue == null))
                return null;

            // If single-column FK, return scalar
            if (fkProps.Count == 1)
                return entry.Property(fkProps[0].Name).CurrentValue;

            // If composite FK, return dictionary
            var values = fkProps.ToDictionary(p => p.Name, p => entry.Property(p.Name).CurrentValue);
            return values;
        }

        ////public static bool IsManyToMany<TEntity, TCollection>(
        ////    this CollectionEntry<TEntity, TCollection> collection)
        ////    where TEntity : class
        ////    where TCollection : class
        //public static bool IsOneToMany(this CollectionEntry collection)
        //{
        //    var navigationMetadata = collection.Metadata as INavigation
        //        ?? throw new InvalidOperationException($"Navigation '{collection.Metadata.Name}' is not a collection navigation.");

        //    return (navigationMetadata.IsCollection && navigationMetadata.Inverse != null && !navigationMetadata.Inverse.IsCollection);
        //}

        //public static bool OneToManyInerseReference(this CollectionEntry collection)
        //{
        //    var navigationMetadata = collection.Metadata as INavigation
        //        ?? throw new InvalidOperationException($"Navigation '{collection.Metadata.Name}' is not a collection navigation.");

        //    navigationMetadata.Inverse.

        //    return (navigationMetadata.IsCollection && navigationMetadata.Inverse != null && navigationMetadata.Inverse.IsCollection);
        //}
    }
}
