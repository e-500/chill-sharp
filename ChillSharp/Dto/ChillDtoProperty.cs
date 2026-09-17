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

﻿namespace ChillSharp.Dto
{
    /// <summary>
    /// Represents a property definition used to describe a projection tree
    /// in ChillSharp client requests.
    /// 
    /// A <see cref="ChillDtoProperty"/> can optionally contain nested
    /// <see cref="SubProperties"/>, allowing hierarchical property selection
    /// (e.g., navigation properties).
    /// </summary>
    public class ChillDtoProperty
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChillDtoProperty"/> class.
        /// 
        /// Required for serializers that rely on parameterless construction.
        /// </summary>
        public ChillDtoProperty() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChillDtoProperty"/> class
        /// with the specified property name.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property to include in the projection.
        /// </param>
        public ChillDtoProperty(string propertyName)
        {
            PropertyName = propertyName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChillDtoProperty"/> class
        /// with a property name and a collection of nested sub-properties.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property to include in the projection.
        /// </param>
        /// <param name="subProperties">
        /// The nested properties associated with this property.
        /// </param>
        public ChillDtoProperty(string propertyName, List<ChillDtoProperty> subProperties)
        {
            PropertyName = propertyName;
            SubProperties = subProperties;
        }

        /// <summary>
        /// Gets the name of the property represented by this instance.
        /// </summary>
        /// <remarks>
        /// This property is immutable after initialization.
        /// </remarks>
        public string PropertyName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the collection of nested properties associated with this property.
        /// </summary>
        /// <remarks>
        /// This collection represents hierarchical projections.
        /// The list itself is initialized by default to avoid null references.
        /// </remarks>
        public List<ChillDtoProperty> SubProperties { get; init; } = new();

        /// <summary>
        /// Builds a list of <see cref="ChillDtoProperty"/> instances from a
        /// heterogeneous collection of strings and <see cref="ChillDtoProperty"/> objects.
        /// </summary>
        /// <param name="properties">
        /// A set of property definitions, either as:
        /// <list type="bullet">
        /// <item><description><see cref="string"/> representing a property name</description></item>
        /// <item><description><see cref="ChillDtoProperty"/> for nested structures</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// A list of <see cref="ChillDtoProperty"/> instances.
        /// </returns>
        /// <exception cref="ChillClientException">
        /// Thrown when an unsupported type is provided.
        /// </exception>
        public static List<ChillDtoProperty> Build(params object[] properties)
        {
            return properties.Select(p => p switch
            {
                string s => new ChillDtoProperty(s),
                ChillDtoProperty dto => dto,
                _ => throw new ChillException(
                                            $"Invalid type '{p?.GetType().Name}'. Only string or ChillDtoProperty are allowed.")
            }).ToList();
        }

        /// <summary>
        /// Creates a <see cref="ChillDtoProperty"/> with nested sub-properties
        /// using a concise DSL-style syntax.
        /// </summary>
        /// <param name="name">
        /// The name of the parent property.
        /// </param>
        /// <param name="subProperties">
        /// The nested properties, defined as strings or <see cref="ChillDtoProperty"/> instances.
        /// </param>
        /// <returns>
        /// A new <see cref="ChillDtoProperty"/> with its sub-properties initialized.
        /// </returns>
        public static ChillDtoProperty With(string name, params object[] subProperties)
        {
            return new ChillDtoProperty(name, Build(subProperties));
        }
    }
}
