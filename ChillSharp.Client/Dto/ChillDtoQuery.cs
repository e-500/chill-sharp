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
		public Dictionary<string, dynamic?> Properties { get; set; } = new Dictionary<string, dynamic?>();

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
