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
using System.ComponentModel.DataAnnotations;

namespace ChillSharp.EF.ServiceModel.I18n
{
    /// <summary>
    /// Represents a localized text entity that stores a text value and its associated culture code for language or
    /// regional formatting purposes.
    /// </summary>
    /// <remarks>Use this class to manage text content that may vary by language or region, such as for
    /// internationalization or localization scenarios. The culture code property enables differentiation of text values
    /// based on cultural context, supporting applications that require multi-language or region-specific
    /// content.</remarks>
    [ChillEntity(
        UniquePropertyKeyString: "DB9456E0-12F0-4DBF-ACCA-EF4C6A94B8B7",
        PrimaryLanguageLabel: "Culture dependant text",
        SecondaryLanguageLabel: "Testo dipendente dalla cultura")]
    public class Text : ChillEntity
    {
        [Key]
        public override Guid Guid { get; set; }

        /// <summary>
        /// Gets or sets the culture code associated with the item, typically used to specify language or regional
        /// formatting.
        /// </summary>
        /// <remarks>The culture code should follow standard conventions such as combined language-region codes (e.g., "en-US" for U.S. English). This
        /// property can be used to localize content or control formatting based on cultural preferences.</remarks>
        [ChillProperty(
            UniquePropertyKeyString: "AF8190BF-57D5-4E5A-AADA-4BA41BDFB322",
            PrimaryLanguageLabel: "Culture code",
            SecondaryLanguageLabel: "Codice cultura")]
        public string CultureCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the text value associated with this property.
        /// </summary>
        [ChillProperty(
            UniquePropertyKeyString: "43F71E38-4E15-4E94-895A-A023617D20D0", 
            PrimaryLanguageLabel: "Text value", 
            SecondaryLanguageLabel: "Valore del testo")]
        public string Value { get; set; } = string.Empty;

    }
}
