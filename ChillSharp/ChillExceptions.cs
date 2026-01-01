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

namespace ChillSharp
{
    /// <summary>
    /// Represents a general exception type for the Chill module.<br/>
    /// Used as a base exception for generic errors within the Chill system.
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or redistribution must comply with the GPLv3 license terms.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    public class ChillException : Exception
    {
        // Default constructor — allows the exception to be created with no message
        public ChillException() { }

        // Constructor that accepts a custom error message
        public ChillException(string message) : base(message) { }

        // Constructor that accepts a custom error message and an inner exception
        public ChillException(string message, Exception exception) : base(message, exception) { }
    }

    /// <summary>
    /// Represents an exception thrown when validation fails.
    /// Typically used for input validation, data consistency checks, or configuration errors.
    /// </summary>
    public class ChillValidationException : Exception
    {
        // Default constructor — allows the exception to be created with no message
        public ChillValidationException() { }

        // Constructor that accepts a specific validation error message
        public ChillValidationException(string message) : base(message) { }

        // Constructor that accepts a custom error message and an inner exception
        public ChillValidationException(string message, Exception exception) : base(message, exception) { }
    }
}
