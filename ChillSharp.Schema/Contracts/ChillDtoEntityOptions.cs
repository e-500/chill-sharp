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

namespace ChillSharp.Dto
{
    /// <summary>
    /// Runtime-configurable options persisted for a specific Chill entity type.
    /// </summary>
    public class ChillDtoEntityOptions
    {
        /// <summary>
        /// Logical Chill type identifier.
        /// </summary>
        public string ChillType { get; set; } = string.Empty;

        /// <summary>
        /// Enables or disables checksum calculation for the entity type.
        /// </summary>
        public bool ChecksumEnabled { get; set; } = true;

        /// <summary>
        /// Optional label format string reserved for future runtime label composition.
        /// </summary>
        public string? LabelFormatString { get; set; }

        /// <summary>
        /// Optional short-label format string reserved for future runtime short-label composition.
        /// </summary>
        public string? ShortLabelFormatString { get; set; }

        /// <summary>
        /// Optional full-text format string used for runtime full-text composition.
        /// </summary>
        public string? FullTextContentFormatString { get; set; }

        /// <summary>
        /// Enables or disables entity change log and history persistence.
        /// </summary>
        public bool ChangeLogEnabled { get; set; }
    }
}
