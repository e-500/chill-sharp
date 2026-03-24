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
using ChillSharp.EF;
using System.Reflection;

namespace ChillSharp.Dto
{
    /// <summary>
    /// Lightweight descriptor for a registered Chill entity or query type.
    /// </summary>
    public class ChillDtoSchemaListItem
    {
        /// <summary>
        /// Localized label resolved from the Chill entity attribute.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Normalized Chill type name exposed by the API.
        /// </summary>
        public string ChillType { get; set; } = string.Empty;

        /// <summary>
        /// Descriptor kind: <c>entity</c> or <c>query</c>.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Related normalized Chill entity type.
        /// For entities this matches <see cref="ChillType"/>, for queries it is the concrete target entity when available.
        /// </summary>
        public string? RelatedChillType { get; set; }

        public static ChillDtoSchemaListItem FromEntityType(Type entityType, string shrinkTypePrefix, IChillContext? context, string? cultureName)
        {
            var chillAttr = entityType.GetCustomAttribute<ChillEntityAttribute>(inherit: true);

            return new ChillDtoSchemaListItem
            {
                Name = ChillLabelResolver.Resolve(
                    chillAttr?.PrimaryLanguageLabel,
                    chillAttr?.SecondaryLanguageLabel,
                    entityType.Name,
                    context,
                    cultureName),
                ChillType = NormalizeChillType(entityType, shrinkTypePrefix),
                Type = "entity",
                RelatedChillType = NormalizeChillType(entityType, shrinkTypePrefix)
            };
        }

        public static ChillDtoSchemaListItem FromQueryType(Type queryType, string shrinkTypePrefix, IChillContext? context, string? cultureName)
        {
            var chillAttr = queryType.GetCustomAttribute<ChillEntityAttribute>(inherit: true);

            return new ChillDtoSchemaListItem
            {
                Name = ChillLabelResolver.Resolve(
                    chillAttr?.PrimaryLanguageLabel,
                    chillAttr?.SecondaryLanguageLabel,
                    queryType.Name,
                    context,
                    cultureName),
                ChillType = NormalizeChillType(queryType, shrinkTypePrefix),
                Type = "query",
                RelatedChillType = ResolveQueryRelatedChillType(queryType, shrinkTypePrefix)
            };
        }

        internal static string NormalizeChillType(Type type, string shrinkTypePrefix)
        {
            return ChillTypeResolver.NormalizeChillType(type, shrinkTypePrefix);
        }

        private static string? ResolveQueryRelatedChillType(Type queryType, string shrinkTypePrefix)
        {
            var entityType = ChillQueryTypeResolver.ResolveRelatedEntityType(queryType);

            if (entityType == null)
            {
                return null;
            }

            return NormalizeChillType(entityType, shrinkTypePrefix);
        }
    }
}
