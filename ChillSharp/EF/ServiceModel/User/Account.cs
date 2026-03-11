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

using ChillSharp.Annotations;
using System.ComponentModel.DataAnnotations;

namespace ChillSharp.EF.ServiceModel.User
{
    public class Account : ChillEntity
    {
        [Key]
        public override Guid Guid { get; set; }

        /// <summary>
        /// Gets or sets the username associated with the user account.
        /// </summary>
        [ChillProperty(
            UniquePropertyKeyString: "979D984C-63C8-4CCB-9413-5109EEE8A308",
            PrimaryLanguageLabel: "User name",
            SecondaryLanguageLabel: "Nome utente")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the culture code associated with the user, typically used to specify language or regional
        /// formatting.
        /// </summary>
        /// <remarks>The culture code should follow standard conventions such as combined language-region codes (e.g., "en-US" for U.S. English). This
        /// property can be used to localize content or control formatting based on cultural preferences.</remarks>
        [ChillProperty(
            UniquePropertyKeyString: "6578FE96-8144-4D82-AE90-C41B825DF514",
            PrimaryLanguageLabel: "Culture code",
            SecondaryLanguageLabel: "Codice cultura")]
        public string CultureCode { get; set; } = string.Empty;


        /// <summary>
        /// Gets or sets the identifier of the time zone, typically corresponding to a value recognized by the system's
        /// time zone database.
        /// </summary>
        /// <remarks>The value should match a valid time zone identifier, such as those returned by <see
        /// cref="TimeZoneInfo.GetSystemTimeZones"/>. Common examples include "Pacific Standard Time" or "UTC".
        /// Supplying an invalid identifier may result in errors when used with time zone conversion APIs.</remarks>
        [ChillProperty(
            UniquePropertyKeyString: "3273CF1E-EC28-406D-9CA2-DAFB89D7FD3B",
            PrimaryLanguageLabel: "Time zone id",
            SecondaryLanguageLabel: "Id zona oraria")]
        public string TimeZoneInfoId { get; set; } = string.Empty;
    }
}
