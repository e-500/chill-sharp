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

        /// <summary>
        /// Gets the culture name associated with labels written as <c>PrimaryLanguageLabel</c>.
        /// </summary>
        /// <remarks>
        /// Different contexts can return different values, allowing multiple Chill contexts to coexist
        /// with their own language conventions inside the same host process.
        /// </remarks>
        string GetPrimaryCultureName()
        {
            return "en-GB";
        }

        /// <summary>
        /// Gets the culture name associated with labels written as <c>SecondaryLanguageLabel</c>.
        /// </summary>
        /// <remarks>
        /// Different contexts can return different values, allowing multiple Chill contexts to coexist
        /// with their own language conventions inside the same host process.
        /// </remarks>
        string GetSecondaryCultureName()
        {
            return "it-IT";
        }

        /// <summary>
        /// Gets the default user culture name used when callers do not explicitly request a schema culture.
        /// </summary>
        /// <remarks>
        /// Contexts can override this to align schema labels with tenant-specific or request-specific user preferences.
        /// </remarks>
        string GetDefaultUserCultureName()
        {
            return GetPrimaryCultureName();
        }

        /// <summary>
        /// Gets the user name associated with the current logical Chill operation.
        /// </summary>
        /// <remarks>
        /// Contexts can override this to provide request-specific or tenant-specific user identity data.
        /// </remarks>
        string GetCurrentUserName()
        {
            return Environment.UserName;
        }
        #endregion
    }
}
