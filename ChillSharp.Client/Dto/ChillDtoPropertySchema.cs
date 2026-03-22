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
using System.Reflection;

namespace ChillSharp.Client.Dto
{
    /// <summary>
    /// Describes how a DTO property should be represented at the front-end.
    /// Contains metadata and presentation hints for various data types.
    /// </summary>
    public class ChillDtoPropertySchema
    {
        public ChillDtoPropertySchema() { }

        /// <summary>
        /// Detailed description of the property type (replaces the prior enum-based Kind).
        /// </summary>
        public ChillDtoPropertyType Type { get; set; } = new ChillDtoPropertyType();

        /// <summary>
        /// Property name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Human-friendly label for UI display.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the property can be null (null = unknown).
        /// </summary>
        public bool? IsNullable { get; set; }

        /// <summary>
        /// Whether the property is read-only from the API perspective.
        /// </summary>
        public bool? IsReadOnly { get; set; }

        /// <summary>
        /// Maximum length for string-like properties (where applicable).
        /// </summary>
        public int? MinLength { get; set; }

        /// <summary>
        /// Maximum length for string-like properties (where applicable).
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Minimum allowed value for integer-like properties.
        /// </summary>
        public long? IntegerMinValue { get; set; }

        /// <summary>
        /// Maximum allowed value for integer-like properties.
        /// </summary>
        public long? IntegerMaxValue { get; set; }

        /// <summary>
        /// Minimum allowed value for decimal-like properties.
        /// </summary>
        public decimal? DecimalMinValue { get; set; }

        /// <summary>
        /// Maximum allowed value for decimal-like properties.
        /// </summary>
        public decimal? DecimalMaxValue { get; set; }

        /// <summary>
        /// Number of decimal places to display for decimal numbers.
        /// </summary>
        public int? DecimalPlaces { get; set; }

        /// <summary>
        /// Precision (total digits) for decimal numbers where relevant.
        /// </summary>
        public int? Precision { get; set; }

        /// <summary>
        /// Scale for decimals (digits to the right of the decimal point).
        /// </summary>
        public int? Scale { get; set; }

        /// <summary>
        /// Preferred date/time format string (e.g. "yyyy-MM-dd", "o", or a culture-specific pattern).
        /// </summary>
        public string DateFormat { get; set; } = string.Empty;

        /// <summary>
        /// For collection types, the description of the item type.
        /// </summary>
        public ChillDtoPropertyType? ItemType { get; set; }

        /// <summary>
        /// For reference/navigation properties, the referenced entity type name.
        /// </summary>
        public string ReferenceChillType { get; set; } = string.Empty;

        /// <summary>
        /// Values for enum types (ordered).
        /// </summary>
        public List<string> EnumValues { get; set; } = new();

        /// <summary>
        /// Arbitrary string metadata useful for front-end renderers.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// Generic custom format string for display (when more specific fields are not enough).
        /// </summary>
        public string CustomFormat { get; set; } = string.Empty;

        /// <summary>
        /// Regular-expression hint for string validation.
        /// </summary>
        public string RegexPattern { get; set; } = string.Empty;
    }
}
