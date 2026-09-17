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
using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace ChillSharp.Dto
{
    internal static class ChillDtoTypeMetadataCache
    {
        private static readonly ConcurrentDictionary<Type, ChillDtoTypeMetadata> Cache = new();

        public static ChillDtoTypeMetadata Get(Type clrType)
        {
            return Cache.GetOrAdd(clrType, static type => new ChillDtoTypeMetadata(type));
        }
    }

    internal sealed class ChillDtoTypeMetadata
    {
        public ChillDtoTypeMetadata(Type clrType)
        {
            ClrType = clrType;

            var chillProperties = clrType.GetProperties()
                .Select(ChillDtoPropertyAccessor.TryCreate)
                .Where(accessor => accessor != null)
                .Cast<ChillDtoPropertyAccessor>()
                .ToArray();

            ChillProperties = chillProperties;
            ChillPropertiesByName = chillProperties.ToDictionary(x => x.Name, StringComparer.Ordinal);
            ChillPropertiesByNameIgnoreCase = chillProperties.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        }

        public Type ClrType { get; }

        public IReadOnlyList<ChillDtoPropertyAccessor> ChillProperties { get; }

        public IReadOnlyDictionary<string, ChillDtoPropertyAccessor> ChillPropertiesByName { get; }

        public IReadOnlyDictionary<string, ChillDtoPropertyAccessor> ChillPropertiesByNameIgnoreCase { get; }
    }

    internal sealed class ChillDtoPropertyAccessor
    {
        private ChillDtoPropertyAccessor(
            string name,
            Type propertyType,
            Type effectiveType,
            bool isNullable,
            bool isEntityReference,
            bool isEntityCollection,
            bool isServerManaged,
            ChillPropertyAttribute attribute,
            ChillDtoPropertyType defaultDtoPropertyType,
            Type? collectionElementType,
            Func<IList>? collectionFactory,
            Func<object, object?> getter,
            Action<object, object?>? setter)
        {
            Name = name;
            PropertyType = propertyType;
            EffectiveType = effectiveType;
            IsNullable = isNullable;
            IsEntityReference = isEntityReference;
            IsEntityCollection = isEntityCollection;
            IsServerManaged = isServerManaged;
            Attribute = attribute;
            DefaultDtoPropertyType = defaultDtoPropertyType;
            CollectionElementType = collectionElementType;
            CollectionFactory = collectionFactory;
            Getter = getter;
            Setter = setter;
        }

        public string Name { get; }

        public Type PropertyType { get; }

        public Type EffectiveType { get; }

        public bool IsNullable { get; }

        public bool IsEntityReference { get; }

        public bool IsEntityCollection { get; }

        public bool IsServerManaged { get; }

        public ChillPropertyAttribute Attribute { get; }

        public ChillDtoPropertyType DefaultDtoPropertyType { get; }

        public Type? CollectionElementType { get; }

        public Func<IList>? CollectionFactory { get; }

        public Func<object, object?> Getter { get; }

        public Action<object, object?>? Setter { get; }

        public static ChillDtoPropertyAccessor? TryCreate(PropertyInfo property)
        {
            var attribute = property.GetCustomAttribute<ChillPropertyAttribute>(inherit: false);
            if (attribute == null)
                return null;

            var propertyType = property.PropertyType;
            var effectiveType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            var isEntityReference = typeof(IChillEntity).IsAssignableFrom(propertyType);
            var isEntityCollection = typeof(IEnumerable<IChillEntity>).IsAssignableFrom(propertyType);
            var collectionElementType = isEntityCollection ? ResolveCollectionElementType(propertyType) : null;

            return new ChillDtoPropertyAccessor(
                property.Name,
                propertyType,
                effectiveType,
                Nullable.GetUnderlyingType(propertyType) != null,
                isEntityReference,
                isEntityCollection,
                IsServerManagedProperty(property.Name),
                attribute,
                ChillDtoPropertyMapper.Map(propertyType),
                collectionElementType,
                collectionElementType == null ? null : BuildCollectionFactory(collectionElementType),
                BuildGetter(property),
                BuildSetter(property));
        }

        private static bool IsServerManagedProperty(string propertyName)
        {
            return propertyName == nameof(IChillEntity.Checksum) ||
                   propertyName == nameof(IChillEntity.LastUpdateUser) ||
                   propertyName == nameof(IChillEntity.LastUpdate) ||
                   propertyName == nameof(IChillEntity.LastUpdateUtcOffset);
        }

        private static Type ResolveCollectionElementType(Type collectionType)
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

        private static Func<object, object?> BuildGetter(PropertyInfo property)
        {
            var target = Expression.Parameter(typeof(object), "target");
            var convertedTarget = Expression.Convert(target, property.DeclaringType!);
            var propertyAccess = Expression.Property(convertedTarget, property);
            var boxValue = Expression.Convert(propertyAccess, typeof(object));
            return Expression.Lambda<Func<object, object?>>(boxValue, target).Compile();
        }

        private static Action<object, object?>? BuildSetter(PropertyInfo property)
        {
            if (!property.CanWrite || property.SetMethod == null)
                return null;

            var target = Expression.Parameter(typeof(object), "target");
            var value = Expression.Parameter(typeof(object), "value");
            var convertedTarget = Expression.Convert(target, property.DeclaringType!);
            var convertedValue = Expression.Convert(value, property.PropertyType);
            var assign = Expression.Assign(Expression.Property(convertedTarget, property), convertedValue);
            return Expression.Lambda<Action<object, object?>>(assign, target, value).Compile();
        }

        private static Func<IList> BuildCollectionFactory(Type collectionElementType)
        {
            var listType = typeof(List<>).MakeGenericType(collectionElementType);
            var newList = Expression.New(listType);
            var castList = Expression.Convert(newList, typeof(IList));
            return Expression.Lambda<Func<IList>>(castList).Compile();
        }
    }
}
