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
    /// Defines the interface that your <see cref="DbContext"/> must implement 
    /// to interact with the ChillSharp engine.
    /// 
    /// <para>
    /// Implementing <see cref="IChillContext"/> ensures that the ChillEngine and ChillDtoEngine 
    /// can activate, query, and persist entities correctly within your EF Core context.
    /// </para>
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or redistribution must comply with the GPLv3 license terms.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    public interface IChillContext
    {
        #region HELPERS
        /// <summary>
        /// Returns the base namespace prefix used by ChillSharp entity type identifiers.
        /// <para>
        /// This string is used by <see cref="ChillEngine"/> to construct the full type name 
        /// when activating entities dynamically.
        /// </para>
        /// <para><b>Example:</b></para>
        /// <code>
        /// FullType: "My.ComplexFramework.Module1.Db.User.Account"
        /// TypeId: "User.Account"
        /// BaseNamespace: "My.ComplexFramework.Module1.Db"
        /// </code>
        /// <para>
        /// When implementing, return the namespace portion (BaseNamespace) of your entities.
        /// </para>
        /// </summary>
        /// <returns>The namespace prefix for ChillSharp entity type identifiers.</returns>
        string GetChillTypePrefix();
        #endregion
    }
}
