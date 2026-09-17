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
using ChillSharp.EF;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace ChillSharp.Dto
{
    /// <summary>
    /// Schema representation of a Chill entity or query type.
    /// Maps property names to frontend-friendly ChillDtoPropertyType values
    /// as provided by <see cref="ChillDtoPropertyMapper"/>.
    /// </summary>
    public class ChillDtoSchema
    {
        /// <summary>
        /// Short chill type identifier (same as used by DTO APIs).
        /// </summary>
        public string ChillType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the code used to identify or represent the ChillView configuration.
        /// </summary>
        public string ChillViewCode { get; set; } = string.Empty;

        /// <summary>
        /// Human-friendly label for UI display.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Map of property name -> mapped frontend property type.
        /// </summary>
        public List<ChillDtoPropertySchema> Properties { get; set; } = new();

        /// <summary>
        /// Create a ChillDtoPropertySchema representation for an IChillEntity instance.
        /// This method attempts to extract a display name from ChillEntityAttribute applied
        /// to the entity type, falling back to DisplayNameAttribute or the type name.
        /// </summary>
        /// <param name="chillEntity">The entity instance to inspect.</param>
        /// <param name="ChillViewCode">Optional view code.</param>
        /// <param name="shrinkTypePrefix">Optional shrink prefix (preserved for compatibility).</param>
        /// <returns>A ChillDtoPropertySchema with best-effort display metadata populated.</returns>
        public static ChillDtoSchema FromIChillEntity(IChillEntity chillEntity, string ChillViewCode = "default", string shrinkTypePrefix = "")
        {
            if (chillEntity == null)
                throw new ArgumentNullException(nameof(chillEntity));

            Type type = chillEntity.GetType();

            // Try to get the custom ChillEntityAttribute on the type.
            ChillEntityAttribute? chillAttr = type.GetCustomAttribute<ChillEntityAttribute>(inherit: true);

            // Resolve display name with fallbacks.
            string? displayName = !string.IsNullOrWhiteSpace(chillAttr?.PrimaryLanguageLabel)
                ? chillAttr.PrimaryLanguageLabel!
                : type.Name;

            // Create schema instance and set fields via reflection if present.
            var schema = new ChillDtoSchema();
            schema.DisplayName = displayName;
            // Shrink type prefix according to ChillContext settings GetChillTypePrefix()
            if (!string.IsNullOrEmpty(shrinkTypePrefix) && !shrinkTypePrefix.EndsWith("."))
                shrinkTypePrefix += ".";
            schema.ChillType = type.FullName!.Replace(shrinkTypePrefix, string.Empty);
            schema.ChillViewCode = ChillViewCode;

            // All chill properties matching the list
            // or all chill properties if list is null
            // No fields if list is empty.
            var ef_props = chillEntity.GetType().GetProperties().Where(prop =>
                prop.IsDefined(typeof(ChillPropertyAttribute), false));
            schema.Properties = ef_props.Select(p => ChillDtoPropertySchema.FromPropertyInfo(p, shrinkTypePrefix)).ToList();

            return schema;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ChillDtoSchema"/> based on the specified <see
        /// cref="IChillQuery{IChillEntity}"/> and its associated metadata.
        /// </summary>
        /// <remarks>The returned schema includes display name, type code, view code, and a list of
        /// properties extracted from the query's type metadata. Only properties marked with <see
        /// cref="ChillPropertyAttribute"/> are included in the schema.</remarks>
        /// <param name="chillQuery">The query object representing the Chill entity. Must not be <see langword="null"/>.</param>
        /// <param name="ChillViewCode">The code identifying the Chill view to associate with the schema. If not specified, defaults to "default".</param>
        /// <param name="shrinkTypePrefix">An optional prefix to apply to property type names when generating property schemas. If not specified, no
        /// prefix is applied.</param>
        /// <returns>A <see cref="ChillDtoSchema"/> instance populated with metadata and property schemas derived from the
        /// provided query.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="chillQuery"/> is <see langword="null"/>.</exception>
        public static ChillDtoSchema FromIChillQuery(IChillQuery<IChillEntity> chillQuery, string ChillViewCode = "default", string shrinkTypePrefix = "")
        {
            if (chillQuery == null)
                throw new ArgumentNullException(nameof(chillQuery));

            Type type = chillQuery.GetType();

            // Try to get the custom ChillEntityAttribute on the type.
            ChillEntityAttribute? chillAttr = type.GetCustomAttribute<ChillEntityAttribute>(inherit: true);

            // Resolve display name with fallbacks.
            string? displayName = !string.IsNullOrWhiteSpace(chillAttr?.PrimaryLanguageLabel)
                ? chillAttr.PrimaryLanguageLabel!
                : type.Name;

            // Create schema instance and set fields via reflection if present.
            var schema = new ChillDtoSchema();
            schema.DisplayName = displayName;
            // Shrink type prefix according to ChillContext settings GetChillTypePrefix()
            if (!string.IsNullOrEmpty(shrinkTypePrefix) && !shrinkTypePrefix.EndsWith("."))
                shrinkTypePrefix += ".";
            schema.ChillType = type.FullName!.Replace(shrinkTypePrefix, string.Empty);
            schema.ChillViewCode = ChillViewCode;

            // All chill properties matching the list
            // or all chill properties if list is null
            // No fields if list is empty.
            var ef_props = chillQuery.GetType().GetProperties().Where(prop =>
                prop.IsDefined(typeof(ChillPropertyAttribute), false));
            schema.Properties = ef_props.Select(p => ChillDtoPropertySchema.FromPropertyInfo(p, shrinkTypePrefix)).ToList();

            return schema;
        }
    }
}