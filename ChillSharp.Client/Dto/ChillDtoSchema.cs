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

using System.Collections.Generic;

namespace ChillSharp.Client.Dto
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
        /// Human-friendly label for the entity or query type.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Chill type of the entity targeted by a query schema.
        /// Empty for entity schemas.
        /// </summary>
        public string? QueryRelatedChillType { get; set; }

        /// <summary>
        /// Map of property name -> mapped frontend property type.
        /// </summary>
        public List<ChillDtoPropertySchema> Properties { get; set; } = new();
    }
}
