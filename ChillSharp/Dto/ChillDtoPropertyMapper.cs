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

namespace ChillSharp.Dto
{
    /// <summary>
    /// Frontend-oriented abstraction of CLR property types.
    /// Numeric values are stable and can be safely consumed by clients.
    /// </summary>
    public enum ChillDtoPropertyType
    {
        /// <summary>
        /// Fallback when a type cannot be mapped.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Any string representing a GUID/UUID.
        /// </summary>
        Guid = 1,

        /// <summary>
        /// Any integral numeric type (int, long, short, etc.).
        /// </summary>
        Integer = 10,

        /// <summary>
        /// Any floating-point or fixed-point numeric type.
        /// </summary>
        Decimal = 20,

        /// <summary>
        /// Date without time component.
        /// </summary>
        Date = 30,

        /// <summary>
        /// Time of day without date component.
        /// </summary>
        Time = 40,

        /// <summary>
        /// Date and time, possibly with offset.
        /// </summary>
        DateTime = 50,

        /// <summary>
        /// Duration or time interval.
        /// </summary>
        Duration = 60,

        /// <summary>
        /// Boolean value.
        /// </summary>
        Boolean = 70,

        /// <summary>
        /// Textual value.
        /// </summary>
        String = 80,

        /// <summary>
        /// Textual value.
        /// </summary>
        Text = 81,

        /// <summary>
        /// Represents an entity that is associated with a chill or cooling process.
        /// </summary>
        ChillEntity = 1000,

        /// <summary>
        /// Represents a collection of entities that are in a chilled or inactive state.
        /// </summary>
        ChillEntityCollection = 1010,

        /// <summary>
        /// Specifies a query that retrieves data in a relaxed or non-urgent manner.
        /// </summary>
        ChillQuery = 1100
    }

    /// <summary>
    /// Maps CLR types to frontend-friendly <see cref="ChillDtoPropertyType"/> values.
    /// </summary>
    public static class ChillDtoPropertyMapper
    {
        /// <summary>
        /// Maps a CLR <see cref="Type"/> to a corresponding <see cref="ChillDtoPropertyType"/>.
        /// Nullable types are unwrapped before evaluation.
        /// </summary>
        public static ChillDtoPropertyType Map(Type type)
        {
            // Null safety
            if (type == null)
                return ChillDtoPropertyType.Unknown;

            // Unwrap Nullable<T> to its underlying type
            type = Nullable.GetUnderlyingType(type) ?? type;

            // Guid types
            if (IsGuid(type))
                return ChillDtoPropertyType.Guid;

            // Integer numeric types
            if (IsInteger(type))
                return ChillDtoPropertyType.Integer;

            // Floating-point and fixed-point numeric types
            if (IsDecimal(type))
                return ChillDtoPropertyType.Decimal;

            // Date-only types
            if (IsDate(type))
                return ChillDtoPropertyType.Date;

            // Time-only types
            if (IsTime(type))
                return ChillDtoPropertyType.Time;

            // Date + time types
            if (IsDateTime(type))
                return ChillDtoPropertyType.DateTime;

            // Duration / interval types
            if (IsDuration(type))
                return ChillDtoPropertyType.Duration;

            // Chill entities
            if (IsChillEntity(type))
                return ChillDtoPropertyType.ChillEntity;

            // Collections of chill entities
            if (IsChillEntityCollection(type))
                return ChillDtoPropertyType.ChillEntityCollection;

            // Chill query types
            if (IsChillQuery(type))
                return ChillDtoPropertyType.ChillQuery; //ciao papi

            // Boolean type
            if (type == typeof(bool))
                return ChillDtoPropertyType.Boolean;

            // Textual types
            if (type == typeof(string) || type == typeof(char))
                return ChillDtoPropertyType.String;

            // Unknown or unsupported type
            return ChillDtoPropertyType.Unknown;
        }

        /// <summary>
        /// Determines whether the type is an GUID/UUID.
        /// </summary>
        private static bool IsGuid(Type type) =>
            type == typeof(Guid);

        /// <summary>
        /// Determines whether the type is an integral numeric type.
        /// </summary>
        private static bool IsInteger(Type type) =>
            type == typeof(byte) ||
            type == typeof(sbyte) ||
            type == typeof(short) ||
            type == typeof(ushort) ||
            type == typeof(int) ||
            type == typeof(uint) ||
            type == typeof(long) ||
            type == typeof(ulong);

        /// <summary>
        /// Determines whether the type is a floating-point or fixed-point numeric type.
        /// </summary>
        private static bool IsDecimal(Type type) =>
            type == typeof(float) ||
            type == typeof(double) ||
            type == typeof(decimal);

        /// <summary>
        /// Determines whether the type represents a date without time.
        /// </summary>
        private static bool IsDate(Type type) =>
            type == typeof(DateOnly);

        /// <summary>
        /// Determines whether the type represents a time of day without date.
        /// </summary>
        private static bool IsTime(Type type) =>
            type == typeof(TimeOnly);

        /// <summary>
        /// Determines whether the type represents a date and time.
        /// </summary>
        private static bool IsDateTime(Type type) =>
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset);

        /// <summary>
        /// Determines whether the type represents a duration or interval.
        /// </summary>
        private static bool IsDuration(Type type) =>
            type == typeof(TimeSpan);

        private static bool IsChillEntity(Type type) =>
            type != null && typeof(IChillEntity).IsAssignableFrom(type);

        private static bool IsChillEntityCollection(Type type)
        {
            if (type == null)
                return false;

            if (type.IsArray)
            {
                var elem = UnwrapProxy(type.GetElementType());
                return elem != null && typeof(IChillEntity).IsAssignableFrom(elem);
            }

            var typesToCheck = new[] { type }.Concat(type.GetInterfaces());

            foreach (var t in typesToCheck)
            {
                if (!t.IsGenericType)
                    continue;

                if (t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var arg = UnwrapProxy(t.GetGenericArguments()[0]);
                    if (typeof(IChillEntity).IsAssignableFrom(arg))
                        return true;
                }
            }

            return false;
        }

        private static Type UnwrapProxy(Type type)
        {
            if (type == null)
                return null;

            // crude but effective for EF Core proxies
            if (type.Namespace != null && type.Namespace.Contains("Castle.Proxies"))
                return type.BaseType ?? type;

            return type;
        }

        private static bool IsChillQuery(Type type)
        {
            if (type == null)
                return false;

            // direct closed generic: IChillQuery<SomeEntity>
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IChillQuery<>))
            {
                var arg = type.GetGenericArguments()[0];
                if (typeof(IChillEntity).IsAssignableFrom(arg))
                    return true;
            }

            // check all implemented interfaces for IChillQuery<T>
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                    continue;

                if (iface.GetGenericTypeDefinition() == typeof(IChillQuery<>))
                {
                    var arg = iface.GetGenericArguments()[0];
                    if (typeof(IChillEntity).IsAssignableFrom(arg))
                        return true;
                }
            }

            return false;
        }
    }
}
