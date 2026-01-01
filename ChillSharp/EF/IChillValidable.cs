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
