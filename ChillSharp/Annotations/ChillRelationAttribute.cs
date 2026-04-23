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

using System.Runtime.CompilerServices;

namespace ChillSharp.Annotations
{
    /// <summary>
    /// Marks an entity collection as a schema relation exposed through <c>ChillDtoSchema.Relations</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ChillRelationAttribute : Attribute
    {
        /// <summary>
        /// Creates a relation annotation without localized labels.
        /// </summary>
        public ChillRelationAttribute([CallerMemberName] string? PropertyName = null)
        {
            this.PropertyName = PropertyName;
        }

        /// <summary>
        /// Creates a relation annotation with localized labels.
        /// </summary>
        public ChillRelationAttribute(
            string UniquePropertyKeyString,
            string PrimaryLanguageLabel,
            string SecondaryLanguageLabel,
            [CallerMemberName] string? PropertyName = null)
        {
            this.PropertyName = PropertyName;
            this.UniquePropertyKey = new Guid(UniquePropertyKeyString);
            this.PrimaryLanguageLabel = PrimaryLanguageLabel;
            this.SecondaryLanguageLabel = SecondaryLanguageLabel;
        }

        /// <summary>
        /// Holds the name of the property associated with this relation.
        /// </summary>
        public string? PropertyName { get; set; }

        /// <summary>
        /// Stable key used for translated relation labels.
        /// </summary>
        public Guid? UniquePropertyKey { get; set; }

        /// <summary>
        /// Primary language label text.
        /// </summary>
        public string? PrimaryLanguageLabel { get; set; }

        /// <summary>
        /// Secondary language label text.
        /// </summary>
        public string? SecondaryLanguageLabel { get; set; }

        /// <summary>
        /// Optional description exposed to schema consumers.
        /// </summary>
        public string? MCPDescription { get; set; }

        /// <summary>
        /// Optional Chill query type used by clients to browse the relation collection.
        /// </summary>
        public string? ReferenceChillTypeQuery { get; set; }
    }
}
