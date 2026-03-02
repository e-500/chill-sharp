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

namespace ChillSharp.Client.Dto
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
}
