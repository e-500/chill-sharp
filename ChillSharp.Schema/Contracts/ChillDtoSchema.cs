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
using ChillSharp.Dto;
using ChillSharp.EF;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace ChillSharp.Schema.Contracts
{
    /// <summary>
    /// Schema representation of a Chill entity or query type.
    /// Maps property names to frontend-friendly ChillDtoPropertyType values
    /// as provided by <see cref="ChillDtoPropertyMapper"/>.
    /// </summary>
    public class ChillDtoSchema : IChillDtoSchema
    {
        /// <summary>
        /// Short Chill type identifier exposed to clients.
        /// </summary>
        public string ChillType { get; set; } = string.Empty;

        /// <summary>
        /// View code identifying the schema variant.
        /// </summary>
        public string ChillViewCode { get; set; } = string.Empty;

        /// <summary>
        /// Human-friendly label for the entity or query type.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Enables publication of the schema as an MCP resource.
        /// </summary>
        public bool EnableMCP { get; set; }

        /// <summary>
        /// Description exposed to MCP clients for the schema resource.
        /// </summary>
        public string MCPDescription { get; set; } = string.Empty;

        /// <summary>
        /// Additional metadata for custom client renderers.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// Chill type of the entity targeted by a query schema.
        /// Empty for entity schemas.
        /// </summary>
        public string? QueryRelatedChillType { get; set; }

        /// <summary>
        /// Property schemas exposed for the type.
        /// </summary>
        public List<ChillDtoPropertySchema> Properties { get; set; } = new();

        IReadOnlyDictionary<string, string> IChillDtoSchema.Metadata => Metadata;

        IReadOnlyList<IChillDtoPropertySchema> IChillDtoSchema.Properties => Properties;

        /// <summary>
        /// Builds schema metadata from an entity instance.
        /// </summary>
        /// <param name="chillEntity">The entity instance to inspect.</param>
        /// <param name="ChillViewCode">The view code attached to the generated schema.</param>
        /// <param name="shrinkTypePrefix">Optional namespace prefix removed from generated Chill type names.</param>
        /// <param name="context">Optional Chill context used to resolve localized labels.</param>
        /// <param name="cultureName">Optional explicit culture used to choose between primary and secondary labels.</param>
        /// <returns>A schema representation of the entity.</returns>
        public static ChillDtoSchema FromIChillEntity(
            IChillEntity chillEntity,
            string ChillViewCode = "default",
            string shrinkTypePrefix = "",
            IChillContext? context = null,
            string? cultureName = null)
        {
            if (chillEntity == null)
                throw new ArgumentNullException(nameof(chillEntity));

            Type type = chillEntity.GetType();
            ChillEntityAttribute? chillAttr = type.GetCustomAttribute<ChillEntityAttribute>(inherit: true);

            string displayName = ChillLabelResolver.Resolve(
                chillAttr?.PrimaryLanguageLabel,
                chillAttr?.SecondaryLanguageLabel,
                type.Name,
                context,
                cultureName);

            var schema = new ChillDtoSchema();
            schema.DisplayName = displayName;
            schema.ChillType = ChillTypeResolver.NormalizeChillType(type, shrinkTypePrefix);
            schema.ChillViewCode = ChillViewCode;
            schema.EnableMCP = chillAttr?.EnableMCP ?? false;
            schema.MCPDescription = chillAttr?.MCPDescription ?? string.Empty;
            schema.Metadata = chillAttr?.GetMetadata() ?? new Dictionary<string, string>();

            var ef_props = chillEntity.GetType().GetProperties().Where(prop =>
                prop.IsDefined(typeof(ChillPropertyAttribute), false));
            schema.Properties = ef_props.Select(p => ChillDtoPropertySchema.FromPropertyInfo(p, shrinkTypePrefix, context, cultureName)).ToList();

            return schema;
        }

        /// <summary>
        /// Builds schema metadata from a query instance.
        /// </summary>
        /// <param name="chillQuery">The query instance to inspect.</param>
        /// <param name="ChillViewCode">The view code attached to the generated schema.</param>
        /// <param name="shrinkTypePrefix">Optional namespace prefix removed from generated Chill type names.</param>
        /// <param name="context">Optional Chill context used to resolve localized labels.</param>
        /// <param name="cultureName">Optional explicit culture used to choose between primary and secondary labels.</param>
        /// <returns>A schema representation of the query.</returns>
        public static ChillDtoSchema FromIChillQuery(
            IChillQuery<IChillEntity> chillQuery,
            string ChillViewCode = "default",
            string shrinkTypePrefix = "",
            IChillContext? context = null,
            string? cultureName = null)
        {
            if (chillQuery == null)
                throw new ArgumentNullException(nameof(chillQuery));

            Type type = chillQuery.GetType();
            ChillEntityAttribute? chillAttr = type.GetCustomAttribute<ChillEntityAttribute>(inherit: true);

            string displayName = ChillLabelResolver.Resolve(
                chillAttr?.PrimaryLanguageLabel,
                chillAttr?.SecondaryLanguageLabel,
                type.Name,
                context,
                cultureName);

            var schema = new ChillDtoSchema();
            schema.DisplayName = displayName;
            schema.ChillType = ChillTypeResolver.NormalizeChillType(type, shrinkTypePrefix);
            schema.ChillViewCode = ChillViewCode;
            schema.EnableMCP = chillAttr?.EnableMCP ?? false;
            schema.MCPDescription = chillAttr?.MCPDescription ?? string.Empty;
            schema.Metadata = chillAttr?.GetMetadata() ?? new Dictionary<string, string>();
            schema.QueryRelatedChillType = ResolveQueryRelatedChillType(type, shrinkTypePrefix);

            var ef_props = chillQuery.GetType().GetProperties().Where(prop =>
                prop.IsDefined(typeof(ChillPropertyAttribute), false));
            schema.Properties = ef_props.Select(p => ChillDtoPropertySchema.FromPropertyInfo(p, shrinkTypePrefix, context, cultureName)).ToList();

            return schema;
        }

        private static string? ResolveQueryRelatedChillType(Type queryType, string shrinkTypePrefix)
        {
            var entityType = ChillQueryTypeResolver.ResolveRelatedEntityType(queryType);

            if (entityType == null)
            {
                return null;
            }

            return ChillTypeResolver.NormalizeChillType(entityType, shrinkTypePrefix);
        }
    }
}

