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
