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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
        /// <param name="cultureName">Optional explicit culture used to choose between primary and secondary labels.</param>
        /// <returns>A schema description suitable for client metadata.</returns>
        public static ChillDtoPropertySchema FromPropertyInfo(
            PropertyInfo propInfo,
            string shrinkTypePrefix = "",
            IChillContext? context = null,
            string? cultureName = null)
        {
            var chillAttr = propInfo.GetCustomAttribute<ChillPropertyAttribute>();
            var propertyType = propInfo.PropertyType;
            var schema = new ChillDtoPropertySchema
            {
                Name = propInfo.Name,
                DisplayName = ChillLabelResolver.Resolve(
                    chillAttr?.PrimaryLanguageLabel,
                    chillAttr?.SecondaryLanguageLabel,
                    propInfo.Name,
                    context,
                    cultureName),
                PropertyType = ChillDtoPropertyMapper.Map(propertyType),
                IsNullable = chillAttr?.IsNullable ?? ResolveNullable(propInfo),
                IsReadOnly = chillAttr?.IsReadOnly ?? ResolveReadOnly(propInfo),
                MinLength = chillAttr?.MinLength ?? ResolveMinLength(propInfo),
                MaxLength = chillAttr?.MaxLength ?? ResolveMaxLength(propInfo),
                IntegerMinValue = chillAttr?.IntegerMinValue,
                IntegerMaxValue = chillAttr?.IntegerMaxValue,
                DecimalMinValue = chillAttr?.GetDecimalMinValue(),
                DecimalMaxValue = chillAttr?.GetDecimalMaxValue(),
                DecimalPlaces = chillAttr?.DecimalPlaces,
                Precision = chillAttr?.Precision,
                Scale = chillAttr?.Scale,
                DateFormat = chillAttr?.DateFormat ?? ResolveDateFormat(propInfo),
                CustomFormat = chillAttr?.CustomFormat ?? ResolveCustomFormat(propInfo),
                RegexPattern = chillAttr?.RegexPattern ?? ResolveRegexPattern(propInfo),
                EnumValues = ResolveEnumValues(propertyType, chillAttr),
                Metadata = chillAttr?.GetMetadata() ?? new Dictionary<string, string>()
            };

            ApplyPrecisionFallbacks(propInfo, schema);

            if (!string.IsNullOrEmpty(shrinkTypePrefix) && !shrinkTypePrefix.EndsWith("."))
                shrinkTypePrefix += ".";

            if (schema.PropertyType == ChillDtoPropertyType.ChillEntity ||
                schema.PropertyType == ChillDtoPropertyType.ChillQuery)
            {
                string? propertyFullType = propertyType.FullName;
                if (!string.IsNullOrEmpty(propertyFullType))
                {
                    propertyFullType = propertyFullType.Replace(shrinkTypePrefix, string.Empty);
                    schema.ReferenceChillType = propertyFullType;
                }
            }
            else if (schema.PropertyType == ChillDtoPropertyType.ChillEntityCollection)
            {
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
                            schema.ReferenceChillType = itemFullName;
                        }
                    }
                }
            }

            return schema;
        }

        /// <summary>
        /// Detailed description of the property's logical Chill type.
        /// </summary>
        public ChillDtoPropertyType PropertyType { get; set; } = new ChillDtoPropertyType();

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
        /// Minimum string length when applicable.
        /// </summary>
        public int? MinLength { get; set; }

        /// <summary>
        /// Maximum string length when applicable.
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
        /// Regular-expression hint for string validation.
        /// </summary>
        public string RegexPattern { get; set; } = string.Empty;

        /// <summary>
        /// Creates a schema preconfigured for decimal values.
        /// </summary>
        public static ChillDtoPropertySchema ForDecimal(
            int? decimalPlaces = null,
            int? precision = null,
            int? scale = null,
            decimal? minValue = null,
            decimal? maxValue = null)
        {
            return new ChillDtoPropertySchema
            {
                DecimalPlaces = decimalPlaces,
                Precision = precision,
                Scale = scale,
                DecimalMinValue = minValue,
                DecimalMaxValue = maxValue
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
        public static ChillDtoPropertySchema ForString(int? minLength = null, int? maxLength = null, string? regexPattern = null)
        {
            return new ChillDtoPropertySchema
            {
                MinLength = minLength,
                MaxLength = maxLength,
                RegexPattern = regexPattern ?? string.Empty
            };
        }

        /// <summary>
        /// Creates a schema preconfigured for integer values.
        /// </summary>
        public static ChillDtoPropertySchema ForInteger(long? minValue = null, long? maxValue = null)
        {
            return new ChillDtoPropertySchema
            {
                IntegerMinValue = minValue,
                IntegerMaxValue = maxValue
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

        private static void ApplyPrecisionFallbacks(PropertyInfo propInfo, ChillDtoPropertySchema schema)
        {
            var precisionAttribute = propInfo.GetCustomAttribute<PrecisionAttribute>();
            if (precisionAttribute != null)
            {
                schema.Precision ??= precisionAttribute.Precision;
                schema.Scale ??= precisionAttribute.Scale;
                schema.DecimalPlaces ??= precisionAttribute.Scale;
            }

            if (schema.DecimalPlaces == null && schema.Scale != null)
            {
                schema.DecimalPlaces = schema.Scale;
            }
        }

        private static bool? ResolveNullable(PropertyInfo propInfo)
        {
            var propertyType = propInfo.PropertyType;
            if (Nullable.GetUnderlyingType(propertyType) != null)
            {
                return true;
            }

            if (propertyType.IsValueType)
            {
                return false;
            }

            var nullability = new NullabilityInfoContext().Create(propInfo);
            return nullability.ReadState switch
            {
                NullabilityState.Nullable => true,
                NullabilityState.NotNull => false,
                _ => null
            };
        }

        private static bool? ResolveReadOnly(PropertyInfo propInfo)
        {
            if (!propInfo.CanWrite || propInfo.SetMethod == null || !propInfo.SetMethod.IsPublic)
            {
                return true;
            }

            var readOnlyAttribute = propInfo.GetCustomAttribute<ReadOnlyAttribute>();
            if (readOnlyAttribute != null)
            {
                return readOnlyAttribute.IsReadOnly;
            }

            var editableAttribute = propInfo.GetCustomAttribute<EditableAttribute>();
            if (editableAttribute != null)
            {
                return !editableAttribute.AllowEdit;
            }

            return null;
        }

        private static int? ResolveMinLength(PropertyInfo propInfo)
        {
            var stringLengthAttribute = propInfo.GetCustomAttribute<StringLengthAttribute>();
            return stringLengthAttribute?.MinimumLength;
        }

        private static int? ResolveMaxLength(PropertyInfo propInfo)
        {
            var maxLengthAttribute = propInfo.GetCustomAttribute<MaxLengthAttribute>();
            if (maxLengthAttribute != null)
            {
                return maxLengthAttribute.Length;
            }

            var stringLengthAttribute = propInfo.GetCustomAttribute<StringLengthAttribute>();
            return stringLengthAttribute?.MaximumLength;
        }

        private static string ResolveDateFormat(PropertyInfo propInfo)
        {
            var displayFormatAttribute = propInfo.GetCustomAttribute<DisplayFormatAttribute>();
            return displayFormatAttribute?.DataFormatString ?? string.Empty;
        }

        private static string ResolveCustomFormat(PropertyInfo propInfo)
        {
            var displayFormatAttribute = propInfo.GetCustomAttribute<DisplayFormatAttribute>();
            return displayFormatAttribute?.DataFormatString ?? string.Empty;
        }

        private static string ResolveRegexPattern(PropertyInfo propInfo)
        {
            var regexAttribute = propInfo.GetCustomAttribute<RegularExpressionAttribute>();
            return regexAttribute?.Pattern ?? string.Empty;
        }

        private static List<string> ResolveEnumValues(Type propertyType, ChillPropertyAttribute? chillAttr)
        {
            if (chillAttr?.EnumValues != null && chillAttr.EnumValues.Length > 0)
            {
                return chillAttr.EnumValues.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }

            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            if (!underlyingType.IsEnum)
            {
                return new List<string>();
            }

            return Enum.GetNames(underlyingType).ToList();
        }
    }
}
