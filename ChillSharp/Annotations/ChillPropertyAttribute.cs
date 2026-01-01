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
