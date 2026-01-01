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
    /// Represents a general exception type for the Chill Client.<br/>
    /// Used as a base exception for generic errors within the Chill Client library.
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or redistribution must comply with the GPLv3 license terms.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    public class ChillClientException : Exception
    {
        // Default constructor — allows the exception to be created with no message
        public ChillClientException() { }

        // Constructor that accepts a custom error message
        public ChillClientException(string message) : base(message) { }

        // Constructor that accepts a custom error message and an inner exception
        public ChillClientException(string message, Exception exception) : base(message, exception) { }
    }
}
