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

using System.Text.Json;

namespace ChillSharp.Client.Dto
{
    /// <summary>
    /// Represents a lightweight, serializable Data Transfer Object (DTO) for defining and executing
    /// dynamic queries or commands using the ChillSharp engine.
    ///
    /// <para>
    /// The <see cref="ChillDtoQuery"/> is designed to be **web-friendly**, omitting EF Core
    /// navigation properties and collection structures. It allows clients to:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Provide query parameters or command arguments via <see cref="Fields"/>.</description></item>
    ///   <item><description>Execute the query or command on the server through the Chill API engine.</description></item>
    ///   <item><description>Receive the results back via the <see cref="Results"/> collection.</description></item>
    /// </list>
    ///
    /// <para>
    /// This class acts as a generic carrier between the client and the ChillSharp engine,
    /// enabling flexible, type-safe data operations without exposing internal EF Core entities.
    /// </para>
    ///
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or redistribution must comply with the GPLv3 license terms.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    public class ChillDtoQuery
    {
        /// <summary>
        /// Optional string identifying the entity type or category.
        /// Useful for polymorphic handling of different entity types in a generic web model.
        /// </summary>
        public string ChillType { get; set; } = string.Empty;

		/// <summary>
		/// A dictionary mapping field names (property keys) to their corresponding values.
		/// Each value is wrapped in a ChillFieldValue, which includes metadata about the field type.
		/// This allows for flexible, dynamic serialization of entity fields.
		/// </summary>
		public Dictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();

        /// <summary>
        /// List of properties of the entity to be obtained with this query
        /// </summary>
        public List<ChillDtoProperty>? ResultProperties { get; set; } = null;

		/// <summary>
		/// A list of entities returned as the result of query execution.
		/// This collection remains empty until the query is executed by the ChillSharp engine.
		/// </summary>
		public List<ChillDtoEntity> Results { get; set; } = new List<ChillDtoEntity>();
    }
}
