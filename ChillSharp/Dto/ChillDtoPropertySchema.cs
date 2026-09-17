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
using System.Collections.Generic;
using System.Reflection;

namespace ChillSharp.Dto
{
    /// <summary>
    /// Describes how a DTO property should be represented at the front-end.
    /// Contains metadata and presentation hints for various data types.
    /// </summary>
    public class ChillDtoPropertySchema
    {
        public ChillDtoPropertySchema() { }

        /// <summary>
        /// Creates a new instance of <see cref="ChillDtoPropertySchema"/> based on the metadata of the specified
        /// property.
        /// </summary>
        /// <remarks>If the property represents a Chill entity, query, or entity collection, the <paramref
        /// name="shrinkTypePrefix"/> is used to remove a prefix from the reference type name. The display name is set
        /// from the <see cref="ChillPropertyAttribute.PrimaryLanguageLabel"/> if available; otherwise, it defaults to
        /// the property name.</remarks>
        /// <param name="propInfo">The <see cref="PropertyInfo"/> object representing the property to map. Must not be <see langword="null"/>.</param>
        /// <param name="shrinkTypePrefix">An optional type prefix to remove from the property's type name when setting reference type information. If
        /// not specified, no prefix is removed.</param>
        /// <returns>A <see cref="ChillDtoPropertySchema"/> populated with information derived from the provided property
        /// metadata.</returns>
        public static ChillDtoPropertySchema FromPropertyInfo(PropertyInfo propInfo, string shrinkTypePrefix = "")
        {
            var s = new ChillDtoPropertySchema();
            s.Name = propInfo.Name;
            var chillAttr = propInfo.GetCustomAttribute<ChillPropertyAttribute>();
            s.DisplayName = !string.IsNullOrWhiteSpace(chillAttr?.PrimaryLanguageLabel)
                ? chillAttr.PrimaryLanguageLabel!
                : propInfo.Name;
            s.Type = ChillDtoPropertyMapper.Map(propInfo.PropertyType);

            // Shrink type prefix according to ChillContext settings GetChillTypePrefix()
            if (!string.IsNullOrEmpty(shrinkTypePrefix) && !shrinkTypePrefix.EndsWith("."))
                shrinkTypePrefix += ".";

            if (s.Type == ChillDtoPropertyType.ChillEntity ||
                s.Type == ChillDtoPropertyType.ChillQuery)
            {
                string? propertyFullType = propInfo.PropertyType.FullName;
                if (!string.IsNullOrEmpty(propertyFullType))
                {
                    propertyFullType = propertyFullType.Replace(shrinkTypePrefix, string.Empty);
                    s.ReferenceChillType = propertyFullType;
                }
            }
            else if (s.Type == ChillDtoPropertyType.ChillEntityCollection)
            {
                var propertyType = propInfo.PropertyType;

                var collectionType = new[] { propertyType }
                    .Concat(propertyType.GetInterfaces())
                    .FirstOrDefault(t =>
                        t.IsGenericType &&
                        t.GetGenericTypeDefinition() == typeof(ICollection<>));

                if (collectionType != null)
                {
                    var itemType = collectionType.GetGenericArguments()[0];

                    if (typeof(IChillEntity).IsAssignableFrom(itemType))
                    {
                        var itemFullName = itemType.FullName;

                        if (!string.IsNullOrEmpty(itemFullName))
                        {
                            itemFullName = itemFullName.Replace(shrinkTypePrefix, string.Empty);
                            s.ReferenceChillType = itemFullName;
                        }
                    }
                }
            }
            return s;
        }

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
        public int? MaxLength { get; set; }

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
        /// Helper for decimal types.
        /// </summary>
        public static ChillDtoPropertySchema ForDecimal(int? decimalPlaces = null, int? precision = null, int? scale = null)
        {
            return new ChillDtoPropertySchema
            {
                DecimalPlaces = decimalPlaces,
                Precision = precision,
                Scale = scale
            };
        }

        /// <summary>
        /// Helper for date/time types with an optional format.
        /// </summary>
        public static ChillDtoPropertySchema ForDateTime(string? dateFormat = null)
        {
            return new ChillDtoPropertySchema
            {
                DateFormat = dateFormat ?? string.Empty
            };
        }

        /// <summary>
        /// Helper for string types.
        /// </summary>
        public static ChillDtoPropertySchema ForString(int? maxLength = null)
        {
            return new ChillDtoPropertySchema
            {
                MaxLength = maxLength
            };
        }

        /// <summary>
        /// Helper for enum types.
        /// </summary>
        public static ChillDtoPropertySchema ForEnum(IEnumerable<string> values)
        {
            return new ChillDtoPropertySchema
            {
                EnumValues = new List<string>(values)
            };
        }

        /// <summary>
        /// Helper for reference/navigation types.
        /// </summary>
        public static ChillDtoPropertySchema ForReference(string referenceType)
        {
            return new ChillDtoPropertySchema
            {
                ReferenceChillType = referenceType ?? string.Empty
            };
        }

        /// <summary>
        /// Helper for collection types.
        /// </summary>
        public static ChillDtoPropertySchema ForCollection(string referenceType)
        {
            return new ChillDtoPropertySchema
            {
                ReferenceChillType = referenceType ?? string.Empty
            };
        }
    }
}