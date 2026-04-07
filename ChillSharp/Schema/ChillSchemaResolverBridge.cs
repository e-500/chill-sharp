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
using ChillSharp.Schema.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ChillSharp.Schema;

public static class ChillSchemaResolverBridge
{
    public static IChillDtoEntityOptions GetEntityOptions(IChillContext context, string chillType)
    {
        var normalizedType = string.IsNullOrWhiteSpace(chillType) ? "default" : chillType.Trim();

        if (context is DbContext dbContext)
        {
            try
            {
                var serviceProvider = ((IInfrastructure<IServiceProvider>)dbContext).Instance;
                var schemaResolver = serviceProvider.GetService(typeof(IChillSchemaResolverService)) as IChillSchemaResolverService;
                if (schemaResolver != null)
                    return schemaResolver.GetEntityOptions(normalizedType);
            }
            catch
            {
            }
        }

        var defaults = ResolveEntityAttributeDefaults(context, normalizedType);
        return new DefaultChillDtoEntityOptions
        {
            ChillType = normalizedType,
            ChecksumEnabled = true,
            LabelFormatString = defaults.LabelFormatString,
            ShortLabelFormatString = defaults.ShortLabelFormatString,
            FullTextContentFormatString = defaults.FullTextContentFormatString,
            EnableMCP = defaults.EnableMCP,
            MCPDescription = defaults.MCPDescription,
            ChangeLogEnabled = false
        };
    }

    private static (string? LabelFormatString, string? ShortLabelFormatString, string? FullTextContentFormatString, bool EnableMCP, string? MCPDescription) ResolveEntityAttributeDefaults(IChillContext context, string chillType)
    {
        try
        {
            var resolvedType = ChillTypeResolver.ResolveType(
                context.GetType().Assembly,
                chillType,
                context.GetChillTypePrefix());

            var chillAttribute = resolvedType.GetCustomAttributes(typeof(ChillEntityAttribute), inherit: true)
                .OfType<ChillEntityAttribute>()
                .FirstOrDefault();

            if (chillAttribute == null)
                return default;

            return (
                NormalizeOptionalText(chillAttribute.LabelFormatString),
                NormalizeOptionalText(chillAttribute.ShortLabelFormatString),
                NormalizeOptionalText(chillAttribute.FullTextContentFormatString),
                chillAttribute.EnableMCP,
                NormalizeOptionalText(chillAttribute.MCPDescription));
        }
        catch
        {
            return default;
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed class DefaultChillDtoEntityOptions : IChillDtoEntityOptions
    {
        public string ChillType { get; init; } = string.Empty;
        public bool ChecksumEnabled { get; init; }
        public string? LabelFormatString { get; init; }
        public string? ShortLabelFormatString { get; init; }
        public string? FullTextContentFormatString { get; init; }
        public bool EnableMCP { get; init; }
        public string? MCPDescription { get; init; }
        public bool ChangeLogEnabled { get; init; }
    }
}
