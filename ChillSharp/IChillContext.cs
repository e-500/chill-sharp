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

using ChillSharp.Dto;
using System.Collections;

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

        /// <summary>
        /// Resolves the runtime entity options for the specified Chill type.
        /// </summary>
        /// <remarks>
        /// When the current context also exposes an <c>EntityOptionsEntries</c> set, the default implementation
        /// reads the persisted row from that set; otherwise it falls back to the built-in defaults.
        /// </remarks>
        /// <param name="chillType">The logical Chill type identifier.</param>
        /// <returns>The resolved entity options.</returns>
        ChillDtoEntityOptions GetEntityOptions(string chillType)
        {
            var normalizedType = string.IsNullOrWhiteSpace(chillType) ? "default" : chillType.Trim();
            return GetCachedEntityOptions(this, normalizedType, () =>
            {
                var defaultOptions = new ChillDtoEntityOptions
                {
                    ChillType = normalizedType,
                    ChecksumEnabled = true,
                    ChangeLogEnabled = false
                };

                var entriesProperty = GetType().GetProperty("EntityOptionsEntries");
                var entries = entriesProperty?.GetValue(this);
                if (entries == null)
                    return defaultOptions;

                var localProperty = entries.GetType().GetProperty("Local");
                if (TryResolveEntityOptions(localProperty?.GetValue(entries) as IEnumerable, normalizedType, out var localOptions))
                    return localOptions;

                try
                {
                    if (TryResolveEntityOptions(entries as IEnumerable, normalizedType, out var persistedOptions))
                        return persistedOptions;
                }
                catch
                {
                    return defaultOptions;
                }

                return defaultOptions;
            });
        }

        /// <summary>
        /// Returns whether checksum calculation is enabled for the specified Chill type.
        /// </summary>
        /// <param name="chillType">The logical Chill type identifier.</param>
        /// <returns><see langword="true"/> when checksum calculation is enabled; otherwise <see langword="false"/>.</returns>
        bool IsEntityChecksumEnabled(string chillType)
        {
            return GetEntityOptions(chillType).ChecksumEnabled;
        }

        private static bool TryResolveEntityOptions(IEnumerable? entries, string chillType, out ChillDtoEntityOptions options)
        {
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry == null)
                        continue;

                    var entryType = entry.GetType();
                    var entryChillType = entryType.GetProperty("ChillType")?.GetValue(entry) as string;
                    if (!string.Equals(entryChillType, chillType, StringComparison.Ordinal))
                        continue;

                    options = new ChillDtoEntityOptions
                    {
                        ChillType = entryChillType ?? chillType,
                        ChecksumEnabled = entryType.GetProperty("ChecksumEnabled")?.GetValue(entry) as bool? ?? true,
                        LabelFormatString = entryType.GetProperty("LabelFormatString")?.GetValue(entry) as string,
                        ShortLabelFormatString = entryType.GetProperty("ShortLabelFormatString")?.GetValue(entry) as string,
                        FullTextContentFormatString = entryType.GetProperty("FullTextContentFormatString")?.GetValue(entry) as string,
                        ChangeLogEnabled = entryType.GetProperty("ChangeLogEnabled")?.GetValue(entry) as bool? ?? false
                    };
                    return true;
                }
            }

            options = null!;
            return false;
        }

        private static ChillDtoEntityOptions GetCachedEntityOptions(IChillContext context, string chillType, Func<ChillDtoEntityOptions> factory)
        {
            var runtimeCacheType = Type.GetType("ChillSharp.Schema.ChillEntityOptionsRuntimeCache, ChillSharp.Schema");
            var getOrAddMethod = runtimeCacheType?.GetMethod("GetOrAdd", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (getOrAddMethod == null)
                return factory();

            return (ChillDtoEntityOptions)getOrAddMethod.Invoke(null, [context, chillType, factory])!;
        }
        #endregion
    }
}
