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

using System.Runtime.CompilerServices; // Provides CallerMemberName, used to capture the name of the calling member automatically

namespace ChillSharp.Annotations
{
    /// <summary>
    /// An attribute used to mark a class or property as a "Chill Entity" field.
    /// <para>It can store metadata about the field's type and nullability.</para>
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or redistribution must comply with the GPLv3 license terms.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    public class ChillEntityAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the ChillEntityAttribute class.
        /// </summary>
        public ChillEntityAttribute() { }

        /// <summary>
        /// Initializes a new instance of the ChillEntityAttribute class with the specified unique entity key and
        /// language labels.
        /// </summary>
        /// <param name="UniquePropertyKeyString">A string representation of the unique entity key. Must be a valid GUID format.</param>
        /// <param name="PrimaryLanguageLabel">The label for the entity in the primary language.</param>
        /// <param name="SecondaryLanguageLabel">The label for the entity in the secondary language.</param>
        public ChillEntityAttribute(
            string UniquePropertyKeyString,
            string PrimaryLanguageLabel,
            string SecondaryLanguageLabel)
        {
            this.UniqueEntityKey = new Guid(UniquePropertyKeyString);
            this.PrimaryLanguageLabel = PrimaryLanguageLabel;
            this.SecondaryLanguageLabel = SecondaryLanguageLabel;
        }

        /// <summary>
        /// Unique key for the property to store the label for translation purposes
        /// </summary>
        public Guid? UniqueEntityKey { get; }

        /// <summary>
        /// Primary language label text (International english)
        /// </summary>
        public string? PrimaryLanguageLabel { get; }

        /// <summary>
        /// Secondary language label text (Software house / Developer language)
        /// </summary>
        public string? SecondaryLanguageLabel { get ; }

        /// <summary>
        /// Enables publication of the entity as an MCP resource.
        /// </summary>
        public bool EnableMCP { get; set; }

        /// <summary>
        /// Description exposed to MCP clients for the entity resource.
        /// </summary>
        public string? MCPDescription { get; set; }

        /// <summary>
        /// Optional metadata entries serialized as <c>key=value</c> pairs.
        /// </summary>
        public string[]? MetadataEntries { get; set; }

        /// <summary>
        /// Converts <see cref="MetadataEntries"/> into a dictionary suitable for DTO schema emission.
        /// </summary>
        public Dictionary<string, string> GetMetadata()
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (MetadataEntries == null)
                return metadata;

            foreach (var entry in MetadataEntries)
            {
                if (string.IsNullOrWhiteSpace(entry))
                    continue;

                var separatorIndex = entry.IndexOf('=');
                if (separatorIndex <= 0 || separatorIndex == entry.Length - 1)
                    continue;

                var key = entry[..separatorIndex].Trim();
                var value = entry[(separatorIndex + 1)..].Trim();
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                metadata[key] = value;
            }

            return metadata;
        }
    }
}
