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
    public class ChillPropertyAttribute : Attribute
    {
        /// <summary>
        /// The constructor for ChillPropertyAttribute.
        /// The optional CallerMemberName attribute automatically supplies the name of the member
        /// (e.g., a property or method) to which this attribute is applied, unless explicitly provided.
        /// </summary>
        /// <param name="FieldName">
        /// The name of the field or property this attribute is applied to.
        /// Automatically filled in by the compiler when not manually provided.
        /// </param>
        /// <param name="CallOnInflate">
        /// If set ChillSharp call OnInflate() asking to load the collection or the property in general
        /// </param>
        public ChillPropertyAttribute([CallerMemberName] string? Name = null, bool CallOnInflate = false)
        {
            this.Name = Name;
            this._CallOnInflate = CallOnInflate;
        }

        /// <summary>
        /// Holds the name of the field or property associated with this attribute.
        /// This is kept private, but could be used internally if reflection is applied.
        /// </summary>
        private string? Name = null;

        private bool _CallOnInflate = false;
        public bool CallOnInflate { get { return _CallOnInflate; } }
    }
}
