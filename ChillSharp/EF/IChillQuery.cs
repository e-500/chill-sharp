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

namespace ChillSharp.EF
{
    /// <summary>
    /// Defines the contract for a Chill query object that can be used to build, filter, sort,
    /// and paginate queries against ChillSharp entities of type <typeparamref name="T"/>.
    /// 
    /// <para>
    /// Implementations of this interface provide a standard way to:
    /// <list type="bullet">
    ///   <item><description>Specify query parameters, including optional <see cref="Guid"/> identifiers.</description></item>
    ///   <item><description>Apply filtering, sorting, and pagination logic in a consistent manner.</description></item>
    ///   <item><description>Return an <see cref="IQueryable{T}"/> for execution against the ChillSharp database context.</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or redistribution must comply with the GPLv3 license terms.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    /// <typeparam name="T">The entity type being queried, typically implementing <see cref="IChillEntity"/>.</typeparam>
    public interface IChillQuery<T> : IChillable
    {
        /// <summary>
        /// GUID used to identify a specific entity.
        /// <para>
        /// Useful for direct lookups or offline-friendly synchronization scenarios.
        /// </para>
        /// </summary>
        Guid? Guid { get; set; }

        /// <summary>
        /// Pagination settings to limit and offset query results.
        /// </summary>
        ChillPagination? Pagination { get; set; }

        /// <summary>
        /// Applies additional filtering logic to the query based on the current object's properties.
        /// <para>
        /// By default, implementations may filter by <see cref="Guid"/> if it is set.
        /// Override to provide custom filtering behavior.
        /// </para>
        /// </summary>
        /// <param name="context">The active Chill database context.</param>
        /// <param name="query">The query to filter.</param>
        /// <returns>The filtered <see cref="IQueryable{T}"/>.</returns>
        IQueryable<T> OnQuery(IChillContext Context);

        /// <summary>
        /// Applies sorting logic to the query results.
        /// <para>
        /// By default, this may sort by <see cref="Guid"/> or other standard fields. Override for custom sorting.
        /// </para>
        /// </summary>
        /// <param name="context">The active Chill database context.</param>
        /// <param name="query">The query to sort.</param>
        /// <returns>The sorted <see cref="IQueryable{T}"/>.</returns>
        IQueryable<T> OnSort(IChillContext Context, IQueryable<T> Query);

        /// <summary>
        /// Applies pagination to the query results if <see cref="Pagination"/> is set.
        /// </summary>
        /// <param name="context">The active Chill database context.</param>
        /// <param name="query">The query to paginate.</param>
        /// <returns>The paginated <see cref="IQueryable{T}"/>.</returns>
        IQueryable<T> OnPaginate(IChillContext Context, IQueryable<T> Query);
    }
}
