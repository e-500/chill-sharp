/*
 * Author: Andrea Piovesan
 * Year: 2025
 * License: GNU Affero General Public License (AGPL) version 3
 *
 * Disclaimer:
 * You are free to use, modify, and distribute it under the terms of the AGPL v3 license.
 * This code comes with no warranty; use it at your own risk.
 * 
 * For further information, please refer to README and LICENSE files.
 */

namespace ChillSharp.EF
{
    /// <summary>
    /// Represents a validation error that occurs when validating a ChillEntity.
    /// This class can store information about which field failed validation
    /// and a message describing the issue.
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or redistribution must comply with the GPLv3 license terms.<br/>
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
