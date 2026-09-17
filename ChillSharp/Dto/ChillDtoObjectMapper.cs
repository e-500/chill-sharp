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

using ChillSharp.EF;
using ChillSharp.Schema;
using ChillSharp.Schema.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Globalization;
using System.Text.Json;

namespace ChillSharp.Dto
{
    internal static class ChillDtoObjectMapper
    {
        private static readonly JsonSerializerOptions IncomingJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Builds a DTO-friendly property bag from the selected CLR properties, resolving entity navigations
        /// into <see cref="ChillDtoEntity"/> wrappers and converting scalar values using the Chill schema type.
        /// </summary>
        public static Dictionary<string, object?> BuildProperties(
            IChillContext context,
            object source,
            string chillType,
            IEnumerable<ChillDtoPropertyAccessor> properties,
            Func<string, List<ChillDtoProperty>>? resolveSubProperties = null,
            Action<string>? onInflate = null)
        {
            var defaultSchema = ResolveDefaultSchema(context, chillType);
            var dbx = (DbContext)context;
            var sourceEntityType = dbx.Model.FindEntityType(source.GetType());
            var sourceIsMappedEntity = sourceEntityType != null;

            return properties.ToDictionary(
                property => property.Name,
                property =>
                {
                    var propertyName = property.Name;

                    if (property.Attribute.CallOnInflate)
                    {
                        onInflate?.Invoke(propertyName);
                    }

                    if (property.IsEntityReference)
                    {
                        if (!sourceIsMappedEntity || dbx.Entry(source).Reference(propertyName).Exist(true))
                        {
                            var entity = (IChillEntity?)property.Getter(source);
                            if (entity == null)
                                return null;

                            return new ChillDtoEntity(context, entity, resolveSubProperties?.Invoke(propertyName) ?? []);
                        }

                        return null;
                    }

                    if (property.IsEntityCollection)
                    {
                        // Check if property is mapped in EF model
                        var navigation = sourceEntityType?.FindNavigation(propertyName);

                        if (navigation != null)
                        {
                            // Load and serialize any kind of collection
                            dbx.Entry(source).Collection(propertyName).Load();

                            //if (dbx.Entry(source).Collection(propertyName).IsImplicitManyToMany())
                            //{
                            //    dbx.Entry(source).Collection(propertyName).Load();
                            //}
                            //else 
                            //{
                            //    return null; // Not loaded and not an implicit many-to-many, return null to avoid unintended loading
                            //}
                        }

                        var collection = (IEnumerable<IChillEntity>?)property.Getter(source);
                        if (collection == null)
                            return null;

                        return collection.Select(item => new ChillDtoEntity(context, item, resolveSubProperties?.Invoke(propertyName) ?? []));
                    }

                    var value = property.Getter(source);
                    var propertyType = ResolvePropertyType(defaultSchema, propertyName, property.DefaultDtoPropertyType);
                    return ConvertFromClrValue(value, property.PropertyType, propertyType);
                });
        }

        /// <summary>
        /// Applies incoming DTO values onto the target CLR object, inflating entity references from DTO payloads
        /// and converting scalar values back to the property type defined by the Chill schema.
        /// </summary>
        public static void ApplyProperties(
            IChillContext context,
            object target,
            string chillType,
            IReadOnlyDictionary<string, object?> sourceValues,
            IEnumerable<ChillDtoPropertyAccessor> properties,
            string objectLabel,
            bool loadTrackedCollections,
            Action<string>? onInflate = null)
        {
            var dbx = (DbContext)context;
            var defaultSchema = ResolveDefaultSchema(context, chillType);
            var targetIsMappedEntity = dbx.Model.FindEntityType(target.GetType()) != null;

            foreach (var property in properties)
            {
                var propertyName = property.Name;

                if (!TryGetSourceValue(sourceValues, propertyName, out var value))
                    continue;

                if (target is IChillEntity && property.IsServerManaged)
                    continue;

                if (property.Attribute.CallOnInflate)
                {
                    onInflate?.Invoke(propertyName);
                    continue;
                }

                // Can handle only implicit many-to-many relations, skip other type of collections.
                // Them should be managed separately
                if (targetIsMappedEntity &&
                    property.IsEntityCollection &&
                    !dbx.Entry(target).Collection(propertyName).IsImplicitManyToMany())
                {
                    continue;
                }

                try
                {
                    var parsedValue = ConvertIncomingValue(
                        dbx,
                        target,
                        property,
                        propertyName,
                        value,
                        defaultSchema,
                        loadTrackedCollections);

                    if (property.Setter == null)
                        throw new ChillException($"Property {propertyName} on chillable {objectLabel} is not writable");

                    property.Setter(target, parsedValue);

                    // Additional: To ensure to write null even if reference is not loaded
                    if (targetIsMappedEntity &&
                        value == null &&
                        property.IsEntityReference)
                    {
                        dbx.Entry(target).Reference(propertyName).ClearForeignKey(); 
                    }
                }
                catch (Exception ex)
                {
                    throw new ChillException($"Error setting value to field {propertyName} on chillable {objectLabel}", ex);
                }
            }
        }

        private static object? ConvertIncomingValue(
            DbContext dbx,
            object target,
            ChillDtoPropertyAccessor property,
            string propertyName,
            object? value,
            IChillDtoSchema? defaultSchema,
            bool loadTrackedCollections)
        {
            if (value == null)
                return null;

            var targetType = property.EffectiveType;

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Null)
                    return null;

                if (property.IsEntityReference)
                {
                    var incomingEntity = JsonSerializer.Deserialize<ChillDtoEntity>(jsonElement.GetRawText(), IncomingJsonOptions);
                    return incomingEntity == null ? null : dbx.Find(targetType, incomingEntity.Guid);
                }

                if (property.IsEntityCollection)
                {
                    var incomingCollection = JsonSerializer.Deserialize<IEnumerable<ChillDtoEntity>>(jsonElement.GetRawText(), IncomingJsonOptions);
                    return ConvertEntityCollection(dbx, target, property, propertyName, incomingCollection, loadTrackedCollections);
                }

                var propertyType = ResolvePropertyType(defaultSchema, propertyName, property.DefaultDtoPropertyType);
                return ConvertToClrValue(jsonElement, targetType, propertyType, property.IsNullable);
            }

            if (property.IsEntityReference)
            {
                return value is ChillDtoEntity incomingEntity
                    ? dbx.Find(targetType, incomingEntity.Guid)
                    : value;
            }

            if (property.IsEntityCollection)
            {
                return ConvertEntityCollection(dbx, target, property, propertyName, value as IEnumerable<ChillDtoEntity>, loadTrackedCollections);
            }

            var resolvedPropertyType = ResolvePropertyType(defaultSchema, propertyName, property.DefaultDtoPropertyType);
            return ConvertToClrValue(value, targetType, resolvedPropertyType, property.IsNullable);
        }

        private static object? ConvertEntityCollection(
            DbContext dbx,
            object target,
            ChillDtoPropertyAccessor property,
            string propertyName,
            IEnumerable<ChillDtoEntity>? incomingCollection,
            bool loadTrackedCollections)
        {
            if (incomingCollection == null)
                return null;

            if (loadTrackedCollections)
                dbx.Entry(target).Collection(propertyName).Load();

            if (property.CollectionElementType == null || property.CollectionFactory == null)
                throw new ChillException($"Unable to resolve collection element type for '{property.PropertyType.FullName ?? property.PropertyType.Name}'.");

            var collectionElementType = property.CollectionElementType;
            var targetList = property.CollectionFactory();

            foreach (var item in incomingCollection)
                targetList.Add(dbx.Find(collectionElementType, item.Guid));

            return targetList;
        }

        private static IChillDtoSchema? ResolveDefaultSchema(IChillContext context, string chillType)
        {
            try
            {
                return context.GetSchemaService().ResolveSchema(chillType, "default", context.GetDefaultUserCultureName());
            }
            catch
            {
                return null;
            }
        }

        private static ChillDtoPropertyType ResolvePropertyType(IChillDtoSchema? schema, string propertyName, ChillDtoPropertyType defaultPropertyType)
        {
            var schemaPropertyType = schema?.Properties
                .FirstOrDefault(x => string.Equals(x.Name, propertyName, StringComparison.Ordinal))
                ?.PropertyType;

            return schemaPropertyType ?? defaultPropertyType;
        }

        private static object? ConvertFromClrValue(object? value, Type clrType, ChillDtoPropertyType propertyType)
        {
            if (value == null)
                return null;

            var targetType = Nullable.GetUnderlyingType(clrType) ?? clrType;
            if (TryConvertTemporalToIsoString(value, targetType, out var temporalValue))
            {
                return temporalValue;
            }

            switch (propertyType)
            {
                case ChillDtoPropertyType.Guid:
                    return value switch
                    {
                        Guid guidValue => guidValue.ToString("D", CultureInfo.InvariantCulture),
                        _ => value.ToString()
                    };

                case ChillDtoPropertyType.Integer:
                    return value switch
                    {
                        byte byteValue => byteValue,
                        sbyte sbyteValue => sbyteValue,
                        short shortValue => shortValue,
                        ushort ushortValue => ushortValue,
                        int intValue => intValue,
                        uint uintValue => uintValue,
                        long longValue => longValue,
                        ulong ulongValue => ulongValue,
                        _ => Convert.ToInt64(value, CultureInfo.InvariantCulture)
                    };

                case ChillDtoPropertyType.Decimal:
                    return value switch
                    {
                        float floatValue => floatValue,
                        double doubleValue => doubleValue,
                        decimal decimalValue => decimalValue,
                        _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
                    };

                case ChillDtoPropertyType.Date:
                    return value switch
                    {
                        DateTime dateTimeValue => DateOnly.FromDateTime(dateTimeValue).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        _ => value
                    };

                case ChillDtoPropertyType.Time:
                    return value switch
                    {
                        DateTime dateTimeValue => TimeOnly.FromDateTime(dateTimeValue).ToString("HH':'mm':'ss'.'FFFFFFF", CultureInfo.InvariantCulture),
                        _ => value
                    };

                case ChillDtoPropertyType.DateTime:
                    return value switch
                    {
                        DateTimeOffset dateTimeOffsetValue => dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture),
                        DateTime dateTimeValue => dateTimeValue.ToString("O", CultureInfo.InvariantCulture),
                        _ => value
                    };

                case ChillDtoPropertyType.Duration:
                    return value switch
                    {
                        TimeSpan timeSpanValue => timeSpanValue.ToString("c", CultureInfo.InvariantCulture),
                        _ => value
                    };

                case ChillDtoPropertyType.Boolean:
                    return value is bool boolValue
                        ? boolValue
                        : Convert.ToBoolean(value, CultureInfo.InvariantCulture);

                case ChillDtoPropertyType.String:
                case ChillDtoPropertyType.Text:
                case ChillDtoPropertyType.Json:
                    return value switch
                    {
                        char charValue => charValue.ToString(),
                        _ => value.ToString()
                    };

                default:
                    return value;
            }
        }

        private static object? ConvertToClrValue(object value, Type targetType, ChillDtoPropertyType propertyType, bool isNullable)
        {
            if (value is JsonElement jsonElement)
            {
                return propertyType switch
                {
                    ChillDtoPropertyType.Guid => ConvertGuid(jsonElement, targetType),
                    ChillDtoPropertyType.Integer => ConvertInteger(jsonElement, targetType),
                    ChillDtoPropertyType.Decimal => ConvertDecimal(jsonElement, targetType),
                    ChillDtoPropertyType.Date => ConvertDate(jsonElement, targetType, isNullable),
                    ChillDtoPropertyType.Time => ConvertTime(jsonElement, targetType),
                    ChillDtoPropertyType.DateTime => ConvertDateTime(jsonElement, targetType),
                    ChillDtoPropertyType.Duration => ConvertDuration(jsonElement, targetType),
                    ChillDtoPropertyType.Boolean => jsonElement.GetBoolean(),
                    ChillDtoPropertyType.String or ChillDtoPropertyType.Text or ChillDtoPropertyType.Json => ConvertString(jsonElement.GetString(), targetType),
                    _ => JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType)
                };
            }

            if (targetType == typeof(DateTimeOffset) && value is DateTimeOffset dateTimeOffsetValue)
                return dateTimeOffsetValue.ToUniversalTime();

            if (targetType.IsInstanceOfType(value))
                return value;

            return propertyType switch
            {
                ChillDtoPropertyType.Guid => ConvertGuid(value, targetType),
                ChillDtoPropertyType.Integer => ConvertInteger(value, targetType),
                ChillDtoPropertyType.Decimal => ConvertDecimal(value, targetType),
                ChillDtoPropertyType.Date => ConvertDate(value, targetType, isNullable),
                ChillDtoPropertyType.Time => ConvertTime(value, targetType),
                ChillDtoPropertyType.DateTime => ConvertDateTime(value, targetType),
                ChillDtoPropertyType.Duration => ConvertDuration(value, targetType),
                ChillDtoPropertyType.Boolean => Convert.ToBoolean(value, CultureInfo.InvariantCulture),
                ChillDtoPropertyType.String or ChillDtoPropertyType.Text or ChillDtoPropertyType.Json => ConvertString(value.ToString(), targetType),
                _ => Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture)
            };
        }

        private static object ConvertGuid(object value, Type targetType)
        {
            var guid = value switch
            {
                Guid guidValue => guidValue,
                JsonElement json => Guid.Parse(json.GetString()!),
                _ => Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!)
            };

            return targetType == typeof(string) ? guid.ToString("D", CultureInfo.InvariantCulture) : guid;
        }

        private static object ConvertInteger(object value, Type targetType)
        {
            if (value is JsonElement json)
            {
                if (targetType == typeof(byte)) return json.GetByte();
                if (targetType == typeof(sbyte)) return json.GetSByte();
                if (targetType == typeof(short)) return json.GetInt16();
                if (targetType == typeof(ushort)) return json.GetUInt16();
                if (targetType == typeof(int)) return json.GetInt32();
                if (targetType == typeof(uint)) return json.GetUInt32();
                if (targetType == typeof(long)) return json.GetInt64();
                if (targetType == typeof(ulong)) return json.GetUInt64();
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private static object ConvertDecimal(object value, Type targetType)
        {
            if (value is JsonElement json)
            {
                if (targetType == typeof(float)) return json.GetSingle();
                if (targetType == typeof(double)) return json.GetDouble();
                if (targetType == typeof(decimal)) return json.GetDecimal();
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private static object? ConvertDate(object value, Type targetType, bool isNullable)
        {
            var text = value is JsonElement json ? json.GetString() : Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
            {
                if (isNullable)
                    return null;

                return targetType == typeof(DateOnly) ? default(DateOnly) : default(DateTime);
            }

            if (targetType == typeof(DateOnly))
                return ParseDateOnly(text);

            var date = ParseDateOnly(text);
            return date.ToDateTime(TimeOnly.MinValue);
        }

        private static object ConvertTime(object value, Type targetType)
        {
            var text = value is JsonElement json ? json.GetString() : Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
                return targetType == typeof(TimeOnly) ? default(TimeOnly) : default(DateTime);

            if (targetType == typeof(TimeOnly))
                return ParseTimeOnly(text);

            var time = ParseTimeOnly(text);
            return DateOnly.MinValue.ToDateTime(time);
        }

        private static object ConvertDateTime(object value, Type targetType)
        {
            var text = value is JsonElement json ? json.GetString() : Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
                return targetType == typeof(DateTimeOffset) ? default(DateTimeOffset) : default(DateTime);

            var systemTimeZone = ChillSharpInitOptions.GetSystemTimeZone();
            var hasUtcDesignator = HasUtcDesignator(text);
            var hasExplicitOffset = HasExplicitOffset(text);

            if (targetType == typeof(DateTimeOffset))
            {
                if (hasUtcDesignator || hasExplicitOffset)
                    return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();

                var localDateTime = ParseUnspecifiedDateTime(text);
                return new DateTimeOffset(localDateTime, systemTimeZone.GetUtcOffset(localDateTime)).ToUniversalTime();
            }

            if (hasUtcDesignator || hasExplicitOffset)
                return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).UtcDateTime;

            var unspecifiedDateTime = ParseUnspecifiedDateTime(text);
            return new DateTimeOffset(
                unspecifiedDateTime,
                systemTimeZone.GetUtcOffset(unspecifiedDateTime))
                .UtcDateTime;
        }

        private static object ConvertDuration(object value, Type targetType)
        {
            if (targetType == typeof(TimeSpan))
            {
                var text = value is JsonElement json ? json.GetString() : Convert.ToString(value, CultureInfo.InvariantCulture);
                return TimeSpan.Parse(text!, CultureInfo.InvariantCulture);
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private static object ConvertString(string? value, Type targetType)
        {
            if (targetType == typeof(char))
                return string.IsNullOrEmpty(value) ? default(char) : value[0];

            return value ?? string.Empty;
        }

        private static bool TryGetSourceValue(IReadOnlyDictionary<string, object?> sourceValues, string propertyName, out object? value)
        {
            if (sourceValues.TryGetValue(propertyName, out value))
                return true;

            foreach (var entry in sourceValues)
            {
                if (string.Equals(entry.Key, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = entry.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static bool TryConvertTemporalToIsoString(object value, Type targetType, out string? serializedValue)
        {
            var systemTimeZone = ChillSharpInitOptions.GetSystemTimeZone();

            if (targetType == typeof(DateTimeOffset) && value is DateTimeOffset dateTimeOffsetValue)
            {
                serializedValue = dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture);
                return true;
            }

            if (targetType == typeof(DateTime) && value is DateTime dateTimeValue)
            {
                serializedValue = ConvertDateTimeToSystemOffset(dateTimeValue, systemTimeZone)
                    .ToString("O", CultureInfo.InvariantCulture);
                return true;
            }

            serializedValue = null;
            return false;
        }

        private static DateOnly ParseDateOnly(string text)
        {
            if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnlyValue))
            {
                return dateOnlyValue;
            }

            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeOffsetValue))
            {
                return new DateOnly(dateTimeOffsetValue.Year, dateTimeOffsetValue.Month, dateTimeOffsetValue.Day);
            }

            var dateTimeValue = DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            return new DateOnly(dateTimeValue.Year, dateTimeValue.Month, dateTimeValue.Day);
        }

        private static TimeOnly ParseTimeOnly(string text)
        {
            if (TimeOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeOnlyValue))
            {
                return timeOnlyValue;
            }

            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeOffsetValue))
            {
                return TimeOnly.FromDateTime(dateTimeOffsetValue.DateTime);
            }

            var dateTimeValue = DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            return TimeOnly.FromDateTime(dateTimeValue);
        }

        private static DateTime ParseUnspecifiedDateTime(string text)
        {
            var parsedDateTime = DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            return DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Unspecified);
        }

        private static bool HasUtcDesignator(string text)
        {
            return text.EndsWith("Z", StringComparison.OrdinalIgnoreCase) ||
                   text.EndsWith("+00:00", StringComparison.OrdinalIgnoreCase) ||
                   text.EndsWith("-00:00", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasExplicitOffset(string text)
        {
            var timeSeparatorIndex = text.IndexOf('T');
            if (timeSeparatorIndex < 0)
            {
                return false;
            }

            var timePart = text[(timeSeparatorIndex + 1)..];
            return timePart.EndsWith("Z", StringComparison.OrdinalIgnoreCase) ||
                   timePart.Contains('+') ||
                   timePart.LastIndexOf('-') > 1;
        }

        private static DateTimeOffset ConvertDateTimeToSystemOffset(DateTime value, TimeZoneInfo systemTimeZone)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => ConvertToSystemTimeZone(new DateTimeOffset(value, TimeSpan.Zero), systemTimeZone),
                DateTimeKind.Local => ConvertToSystemTimeZone(new DateTimeOffset(value), systemTimeZone),
                _ => new DateTimeOffset(
                    DateTime.SpecifyKind(value, DateTimeKind.Unspecified),
                    systemTimeZone.GetUtcOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified)))
            };
        }

        private static DateTimeOffset ConvertToSystemTimeZone(DateTimeOffset value, TimeZoneInfo systemTimeZone)
        {
            return TimeZoneInfo.ConvertTime(value, systemTimeZone);
        }
    }
}
