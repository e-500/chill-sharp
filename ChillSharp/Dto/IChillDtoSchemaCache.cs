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

using System;
using System.Collections.Generic;

namespace ChillSharp.Dto
{
    /// <summary>
    /// Contract for a thread-safe in-memory cache of Chill API schemas.
    /// Implementations should provide fast lookup and be safe for concurrent use.
    /// </summary>
    public interface IChillDtoSchemaCache
    {
        /// <summary>
        /// Returns a snapshot collection of cached schemas.
        /// </summary>
        IReadOnlyCollection<ChillDtoSchema> Schemas { get; }

        /// <summary>
        /// Attempts to retrieve a schema from the cache.
        /// </summary>
        /// <param name="chillType">The chill type (or null/empty to indicate the default key).</param>
        /// <param name="chillViewCode">The chill view code (or null/empty to indicate the default key).</param>
        /// <param name="schema">When this method returns, contains the retrieved schema if found; otherwise null.</param>
        /// <returns>True if a schema was found; otherwise false.</returns>
        bool TryGet(string chillType, string chillViewCode, out ChillDtoSchema? schema);

        /// <summary>
        /// Adds or updates a schema in the cache and returns the stored schema.
        /// Implementations should throw <see cref="ArgumentNullException"/> if <paramref name="schema"/> is null.
        /// </summary>
        /// <param name="schema">The schema to add or update.</param>
        /// <returns>The added or updated schema.</returns>
        ChillDtoSchema SetSchema(ChillDtoSchema schema);

        /// <summary>
        /// Removes a single schema from the cache.
        /// </summary>
        /// <param name="chillType">The chill type (or null/empty to indicate the default key).</param>
        /// <param name="chillViewCode">The chill view code (or null/empty to indicate the default key).</param>
        void Invalidate(string chillType, string chillViewCode);

        /// <summary>
        /// Clears the entire cache.
        /// </summary>
        void InvalidateAll();
    }
}