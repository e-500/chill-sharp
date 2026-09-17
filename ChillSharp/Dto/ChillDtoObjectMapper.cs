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
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace ChillSharp.Dto
{
    internal static class ChillDtoObjectMapper
    {
        public static Dictionary<string, object?> BuildProperties(
            IChillContext context,
            object source,
            string chillType,
            IEnumerable<PropertyInfo> properties,
            Func<string, List<ChillDtoProperty>>? resolveSubProperties = null,
            Action<string>? onInflate = null)
        {
            var defaultSchema = ResolveDefaultSchema(context, chillType);
            var dbx = (DbContext)context;

            return properties.ToDictionary(
                property => property.Name,
                property =>
                {
                    var attr = property.GetCustomAttribute<ChillPropertyAttribute>()!;
                    var propertyName = property.Name;

                    if (attr.CallOnInflate)
                    {
                        onInflate?.Invoke(propertyName);
                    }

                    if (typeof(IChillEntity).IsAssignableFrom(property.PropertyType))
                    {
                        if (dbx.Entry(source).Reference(propertyName).Exist(true))
                        {
                            var entity = (IChillEntity?)property.GetValue(source);
                            if (entity == null)
                                return null;

                            return new ChillDtoEntity(context, entity, resolveSubProperties?.Invoke(propertyName) ?? []);
                        }

                        return null;
                    }

                    if (typeof(IEnumerable<IChillEntity>).IsAssignableFrom(property.PropertyType))
                    {
                        // Check if property is mapped in EF model
                        var entityType = dbx.Model.FindEntityType(source.GetType());
                        var navigation = entityType?.FindNavigation(propertyName);

                        if (navigation != null)
                        {
                            dbx.Entry(source).Collection(propertyName).Load();
                        }

                        var collection = (IEnumerable<IChillEntity>?)property.GetValue(source);
                        if (collection == null)
                            return null;

                        return collection.Select(item => new ChillDtoEntity(context, item, resolveSubProperties?.Invoke(propertyName) ?? []));
                    }

                    var value = property.GetValue(source);
                    var propertyType = ResolvePropertyType(defaultSchema, propertyName, property.PropertyType);
                    return ConvertFromClrValue(value, propertyType);
                });
        }

        public static void ApplyProperties(
            IChillContext context,
            object target,
            string chillType,
            IReadOnlyDictionary<string, object?> sourceValues,
            IEnumerable<PropertyInfo> properties,
            string objectLabel,
            bool loadTrackedCollections,
            Action<string>? onInflate = null)
        {
            var dbx = (DbContext)context;
            var defaultSchema = ResolveDefaultSchema(context, chillType);

            foreach (var property in properties)
            {
                var attr = property.GetCustomAttribute<ChillPropertyAttribute>()!;
                var propertyName = property.Name;
                var value = sourceValues[propertyName];

                if (attr.CallOnInflate)
                {
                    onInflate?.Invoke(propertyName);
                    continue;
                }

                try
                {
                    var parsedValue = ConvertIncomingValue(
                        context,
                        dbx,
                        target,
                        property,
                        propertyName,
                        value,
                        defaultSchema,
                        loadTrackedCollections);

                    property.SetValue(target, parsedValue);
                }
                catch (Exception ex)
                {
                    throw new ChillException($"Error setting value to field {propertyName} on chillable {objectLabel}", ex);
                }
            }
        }

        private static object? ConvertIncomingValue(
            IChillContext context,
            DbContext dbx,
            object target,
            PropertyInfo property,
            string propertyName,
            object? value,
            ChillDtoSchema? defaultSchema,
            bool loadTrackedCollections)
        {
            if (value == null)
                return null;

            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Null)
                    return null;

                if (typeof(IChillEntity).IsAssignableFrom(property.PropertyType))
                {
                    var incomingEntity = JsonSerializer.Deserialize<ChillDtoEntity>(jsonElement.GetRawText());
                    return incomingEntity == null ? null : dbx.Find(targetType, incomingEntity.Guid);
                }

                if (typeof(IEnumerable<IChillEntity>).IsAssignableFrom(property.PropertyType))
                {
                    var incomingCollection = JsonSerializer.Deserialize<IEnumerable<ChillDtoEntity>>(jsonElement.GetRawText());
                    return ConvertEntityCollection(dbx, target, property, propertyName, incomingCollection, loadTrackedCollections);
                }

                var propertyType = ResolvePropertyType(defaultSchema, propertyName, property.PropertyType);
                return ConvertToClrValue(jsonElement, targetType, propertyType, Nullable.GetUnderlyingType(property.PropertyType) != null);
            }

            if (typeof(IChillEntity).IsAssignableFrom(property.PropertyType))
            {
                return value is ChillDtoEntity incomingEntity
                    ? dbx.Find(targetType, incomingEntity.Guid)
                    : value;
            }

            if (typeof(IEnumerable<IChillEntity>).IsAssignableFrom(property.PropertyType))
            {
                return ConvertEntityCollection(dbx, target, property, propertyName, value as IEnumerable<ChillDtoEntity>, loadTrackedCollections);
            }

            var resolvedPropertyType = ResolvePropertyType(defaultSchema, propertyName, property.PropertyType);
            return ConvertToClrValue(value, targetType, resolvedPropertyType, Nullable.GetUnderlyingType(property.PropertyType) != null);
        }

        private static object? ConvertEntityCollection(
            DbContext dbx,
            object target,
            PropertyInfo property,
            string propertyName,
            IEnumerable<ChillDtoEntity>? incomingCollection,
            bool loadTrackedCollections)
        {
            if (incomingCollection == null)
                return null;

            if (loadTrackedCollections)
                dbx.Entry(target).Collection(propertyName).Load();

            var collectionElementType = GetCollectionElementType(property.PropertyType);
            var listType = typeof(List<>).MakeGenericType(collectionElementType);
            var targetList = (IList?)Activator.CreateInstance(listType);

            foreach (var item in incomingCollection)
                targetList!.Add(dbx.Find(collectionElementType, item.Guid));

            return targetList;
        }

        private static Type GetCollectionElementType(Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType()!;

            if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return collectionType.GetGenericArguments()[0];

            var enumerableType = collectionType
                .GetInterfaces()
                .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableType != null)
                return enumerableType.GetGenericArguments()[0];

            throw new ChillException($"Unable to resolve collection element type for '{collectionType.FullName ?? collectionType.Name}'.");
        }

        private static ChillDtoSchema? ResolveDefaultSchema(IChillContext context, string chillType)
        {
            if (context is not DbContext dbContext)
                return null;

            try
            {
                var serviceProvider = ((IInfrastructure<IServiceProvider>)dbContext).Instance;
                var schemaService = serviceProvider.GetService(typeof(IChillSchemaService)) as IChillSchemaService;
                return schemaService?.GetSchemaAsync(chillType, "default").GetAwaiter().GetResult();
            }
            catch
            {
                return null;
            }
        }

        private static ChillDtoPropertyType ResolvePropertyType(ChillDtoSchema? schema, string propertyName, Type clrType)
        {
            var schemaPropertyType = schema?.Properties
                .FirstOrDefault(x => string.Equals(x.Name, propertyName, StringComparison.Ordinal))
                ?.PropertyType;

            return schemaPropertyType ?? ChillDtoPropertyMapper.Map(clrType);
        }

        private static object? ConvertFromClrValue(object? value, ChillDtoPropertyType propertyType)
        {
            if (value == null)
                return null;

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
                        DateOnly dateOnlyValue => dateOnlyValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        DateTime dateTimeValue => DateOnly.FromDateTime(dateTimeValue).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        _ => value
                    };

                case ChillDtoPropertyType.Time:
                    return value switch
                    {
                        TimeOnly timeOnlyValue => timeOnlyValue.ToString("HH':'mm':'ss'.'FFFFFFF", CultureInfo.InvariantCulture),
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
                    ChillDtoPropertyType.String or ChillDtoPropertyType.Text => ConvertString(jsonElement.GetString(), targetType),
                    _ => JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType)
                };
            }

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
                ChillDtoPropertyType.String or ChillDtoPropertyType.Text => ConvertString(value.ToString(), targetType),
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

            // Normalize: if has format "2024-01-01T00:00:00.000Z" remove "T00:00:00.000Z" to keep "2024-01-01"
            if (text.Contains("T"))
                text = text.Split("T", StringSplitOptions.None)[0];

            if (targetType == typeof(DateOnly))
                return DateOnly.Parse(text, CultureInfo.InvariantCulture);

            var date = DateOnly.Parse(text, CultureInfo.InvariantCulture);
            return date.ToDateTime(TimeOnly.MinValue);
        }

        private static object ConvertTime(object value, Type targetType)
        {
            var text = value is JsonElement json ? json.GetString() : Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
                return targetType == typeof(TimeOnly) ? default(TimeOnly) : default(DateTime);

            if (targetType == typeof(TimeOnly))
                return TimeOnly.Parse(text, CultureInfo.InvariantCulture);

            var time = TimeOnly.Parse(text, CultureInfo.InvariantCulture);
            return DateOnly.MinValue.ToDateTime(time);
        }

        private static object ConvertDateTime(object value, Type targetType)
        {
            var text = value is JsonElement json ? json.GetString() : Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
                return targetType == typeof(DateTimeOffset) ? default(DateTimeOffset) : default(DateTime);

            if (targetType == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

            return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
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
    }
}
