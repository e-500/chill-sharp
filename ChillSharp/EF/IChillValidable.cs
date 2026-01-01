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
    /// COMMENT: This interface allow form validation process
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or redistribution must comply with the GPLv3 license terms.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    public interface IChillValidable
    {
        /// <summary>
        /// Called by the <c>AUTOCOMPLETE()</c> method to fill or adjust fields 
        /// based on the current ("dirty") state of the entity.
        /// </summary>
        /// <param name="Context">The active database context.</param>
        void OnAutocomplete(IChillContext Context);

        /// <summary>
        /// Validates entity fields before an update operation.
        /// <para>Called by:</para>
        /// <list type="bullet">
        /// <item><description><c>VALIDATE()</c> — returns validation issues gracefully to the client.</description></item>
        /// <item><description><c>UPDATE()</c> — throws an exception if validation fails.</description></item>
        /// </list>
        /// </summary>
        /// <param name="Context">The active database context.</param>
        /// <returns>A collection of validation results.</returns>
        IEnumerable<ChillValidationError> OnValidation(IChillContext Context);
    }
}
