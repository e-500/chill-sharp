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

namespace ChillSharp.EF
{
    /// <summary>
    /// Represents a validation error that occurs when validating a ChillEntity.
    /// This class can store information about which field failed validation
    /// and a message describing the issue.
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the terms of the 
    /// GNU Affero General Public License as published by the Free Software Foundation, 
    /// either version 3 of the License, or (at your option) any later version.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    public class ChillValidationError
    {
        /// <summary>
        /// The name of the field that caused the validation error.
        /// Can be null if the error is not associated with a specific field.
        /// </summary>
        public string? FieldName { get; set; } = null;

        /// <summary>
        /// The validation error message describing what went wrong.
        /// For example, "Value cannot be null" or "Invalid format".
        /// </summary>
        public string? Message { get; set; } = null;
    }
}
