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
        /// <summary>
        /// Initializes an empty property schema instance.
        /// </summary>
        public ChillDtoPropertySchema() { }

        /// <summary>
        /// Builds a DTO property schema from a reflected property definition.
        /// </summary>
        /// <param name="propInfo">The reflected property to inspect.</param>
        /// <param name="shrinkTypePrefix">Optional namespace prefix removed from reference Chill types.</param>
        /// <param name="context">Optional Chill context used to resolve localized labels.</param>
        /// <returns>A schema description suitable for client metadata.</returns>
        public static ChillDtoPropertySchema FromPropertyInfo(PropertyInfo propInfo, string shrinkTypePrefix = "", IChillContext? context = null)
        {
            var s = new ChillDtoPropertySchema();
            s.Name = propInfo.Name;
            var chillAttr = propInfo.GetCustomAttribute<ChillPropertyAttribute>();
            s.DisplayName = ChillLabelResolver.Resolve(
                chillAttr?.PrimaryLanguageLabel,
                chillAttr?.SecondaryLanguageLabel,
                propInfo.Name,
                context);
            s.Type = ChillDtoPropertyMapper.Map(propInfo.PropertyType);

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
        /// Detailed description of the property's logical Chill type.
        /// </summary>
        public ChillDtoPropertyType Type { get; set; } = new ChillDtoPropertyType();

        /// <summary>
        /// CLR property name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Human-friendly label chosen from Chill metadata or the property name fallback.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the value can be null when known.
        /// </summary>
        public bool? IsNullable { get; set; }

        /// <summary>
        /// Whether the property should be treated as read-only by clients.
        /// </summary>
        public bool? IsReadOnly { get; set; }

        /// <summary>
        /// Maximum string length when applicable.
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Preferred number of decimal places for decimal-like values.
        /// </summary>
        public int? DecimalPlaces { get; set; }

        /// <summary>
        /// Total precision for decimal-like values.
        /// </summary>
        public int? Precision { get; set; }

        /// <summary>
        /// Decimal scale for decimal-like values.
        /// </summary>
        public int? Scale { get; set; }

        /// <summary>
        /// Preferred date or time display format.
        /// </summary>
        public string DateFormat { get; set; } = string.Empty;

        /// <summary>
        /// Referenced Chill type for entity, query, or collection relationships.
        /// </summary>
        public string ReferenceChillType { get; set; } = string.Empty;

        /// <summary>
        /// Ordered values for enum-like properties.
        /// </summary>
        public List<string> EnumValues { get; set; } = new();

        /// <summary>
        /// Additional metadata for custom client renderers.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// Generic custom format hint for UI consumers.
        /// </summary>
        public string CustomFormat { get; set; } = string.Empty;

        /// <summary>
        /// Creates a schema preconfigured for decimal values.
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
        /// Creates a schema preconfigured for date or time values.
        /// </summary>
        public static ChillDtoPropertySchema ForDateTime(string? dateFormat = null)
        {
            return new ChillDtoPropertySchema
            {
                DateFormat = dateFormat ?? string.Empty
            };
        }

        /// <summary>
        /// Creates a schema preconfigured for string values.
        /// </summary>
        public static ChillDtoPropertySchema ForString(int? maxLength = null)
        {
            return new ChillDtoPropertySchema
            {
                MaxLength = maxLength
            };
        }

        /// <summary>
        /// Creates a schema preconfigured for enum values.
        /// </summary>
        public static ChillDtoPropertySchema ForEnum(IEnumerable<string> values)
        {
            return new ChillDtoPropertySchema
            {
                EnumValues = new List<string>(values)
            };
        }

        /// <summary>
        /// Creates a schema preconfigured for a single reference relationship.
        /// </summary>
        public static ChillDtoPropertySchema ForReference(string referenceType)
        {
            return new ChillDtoPropertySchema
            {
                ReferenceChillType = referenceType ?? string.Empty
            };
        }

        /// <summary>
        /// Creates a schema preconfigured for a collection relationship.
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
