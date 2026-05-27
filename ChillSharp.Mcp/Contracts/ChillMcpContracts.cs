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
using ChillSharp.EF;
using ChillSharp.Schema.Contracts;

namespace ChillSharp.Mcp.Contracts;

public sealed class ChillMcpSchemaListItem
{
    public string Name { get; set; } = string.Empty;
    public string ChillType { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? RelatedChillType { get; set; }
    public string Description { get; set; } = string.Empty;

    public static ChillMcpSchemaListItem FromDto(ChillDtoSchemaListItem dto, string? description)
    {
        return new ChillMcpSchemaListItem
        {
            Name = dto.Name,
            ChillType = dto.ChillType,
            Type = dto.Type,
            RelatedChillType = dto.RelatedChillType,
            Description = description ?? string.Empty
        };
    }
}

public sealed class ChillMcpSchema
{
    public string ChillType { get; set; } = string.Empty;
    public string ChillViewCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool HandleAttachments { get; set; }
    public bool EnableMCP { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = [];
    public string? QueryRelatedChillType { get; set; }
    public List<ChillMcpPropertySchema> Properties { get; set; } = [];
    public List<ChillMcpSchemaRelation> Relations { get; set; } = [];

    public static ChillMcpSchema FromDto(ChillDtoSchema dto)
    {
        return new ChillMcpSchema
        {
            ChillType = dto.ChillType,
            ChillViewCode = dto.ChillViewCode,
            DisplayName = dto.DisplayName,
            HandleAttachments = dto.HandleAttachments,
            EnableMCP = dto.EnableMCP,
            Description = dto.MCPDescription,
            Metadata = dto.Metadata.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal),
            QueryRelatedChillType = dto.QueryRelatedChillType,
            Properties = dto.Properties.Select(ChillMcpPropertySchema.FromDto).ToList(),
            Relations = dto.Relations.Select(ChillMcpSchemaRelation.FromDto).ToList()
        };
    }
}

public sealed class ChillMcpPropertySchema
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int PropertyType { get; set; }
    public string SimplePropertyType { get; set; } = string.Empty;
    public string ReferenceChillType { get; set; } = string.Empty;
    public string ReferenceChillTypeQuery { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool? IsNullable { get; set; }
    public bool? IsReadOnly { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public long? IntegerMinValue { get; set; }
    public long? IntegerMaxValue { get; set; }
    public decimal? DecimalMinValue { get; set; }
    public decimal? DecimalMaxValue { get; set; }
    public int? DecimalPlaces { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public string DateFormat { get; set; } = string.Empty;
    public string CustomFormat { get; set; } = string.Empty;
    public string RegexPattern { get; set; } = string.Empty;
    public List<string> EnumValues { get; set; } = [];
    public string? LookupQueryValues { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = [];

    public static ChillMcpPropertySchema FromDto(ChillDtoPropertySchema dto)
    {
        return new ChillMcpPropertySchema
        {
            Name = dto.Name,
            DisplayName = dto.DisplayName,
            PropertyType = (int)dto.PropertyType,
            SimplePropertyType = dto.SimplePropertyType,
            ReferenceChillType = dto.ReferenceChillType,
            ReferenceChillTypeQuery = dto.ReferenceChillTypeQuery,
            Description = dto.MCPDescription,
            IsNullable = dto.IsNullable,
            IsReadOnly = dto.IsReadOnly,
            MinLength = dto.MinLength,
            MaxLength = dto.MaxLength,
            IntegerMinValue = dto.IntegerMinValue,
            IntegerMaxValue = dto.IntegerMaxValue,
            DecimalMinValue = dto.DecimalMinValue,
            DecimalMaxValue = dto.DecimalMaxValue,
            DecimalPlaces = dto.DecimalPlaces,
            Precision = dto.Precision,
            Scale = dto.Scale,
            DateFormat = dto.DateFormat,
            CustomFormat = dto.CustomFormat,
            RegexPattern = dto.RegexPattern,
            EnumValues = dto.EnumValues.ToList(),
            LookupQueryValues = dto.LookupQueryValues,
            Metadata = dto.Metadata.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal)
        };
    }
}

public sealed class ChillMcpSchemaRelation
{
    public string ChillType { get; set; } = string.Empty;
    public string ChillQuery { get; set; } = string.Empty;
    public Dictionary<string, string> FixedValues { get; set; } = [];
    public Dictionary<string, string> FixedQueryValues { get; set; } = [];
    public ChillMcpSchemaRelationLabel RelationLabel { get; set; } = new();

    public static ChillMcpSchemaRelation FromDto(ChillDtoSchemaRelation dto)
    {
        return new ChillMcpSchemaRelation
        {
            ChillType = dto.ChillType,
            ChillQuery = dto.ChillQuery,
            FixedValues = dto.FixedValues.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal),
            FixedQueryValues = dto.FixedQueryValues.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal),
            RelationLabel = ChillMcpSchemaRelationLabel.FromDto(dto.RelationLabel)
        };
    }
}

public sealed class ChillMcpSchemaRelationLabel
{
    public Guid? LabelGuid { get; set; }
    public string PrimaryDefaultText { get; set; } = string.Empty;
    public string SecondaryDefaultText { get; set; } = string.Empty;

    public static ChillMcpSchemaRelationLabel FromDto(ChillDtoSchemaRelationLabel dto)
    {
        return new ChillMcpSchemaRelationLabel
        {
            LabelGuid = dto.LabelGuid,
            PrimaryDefaultText = dto.PrimaryDefaultText,
            SecondaryDefaultText = dto.SecondaryDefaultText
        };
    }
}

/// <summary>
/// MCP-specific entity DTO with only the fields useful to tool callers.
/// </summary>
public sealed class ChillMcpEntity
{
    public Guid? Guid { get; set; }
    public int? Position { get; set; }
    public string ChillType { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string? ShortLabel { get; set; }
    public Dictionary<string, object?> Properties { get; set; } = [];

    public ChillDtoEntity ToDto()
    {
        return new ChillDtoEntity
        {
            Guid = Guid ?? System.Guid.Empty,
            Position = Position ?? 0,
            ChillType = ChillType,
            Label = Label,
            ShortLabel = ShortLabel,
            Properties = Properties.ToDictionary(
                item => item.Key,
                item => ToDtoValue(item.Value),
                StringComparer.Ordinal)
        };
    }

    public static ChillMcpEntity FromDto(ChillDtoEntity dto)
    {
        return new ChillMcpEntity
        {
            Guid = dto.Guid == System.Guid.Empty ? null : dto.Guid,
            Position = dto.Position,
            ChillType = dto.ChillType,
            Label = dto.Label,
            ShortLabel = dto.ShortLabel,
            Properties = dto.Properties.ToDictionary(
                item => item.Key,
                item => FromDtoValue(item.Value),
                StringComparer.Ordinal)
        };
    }

    internal static object? ToDtoValue(object? value)
    {
        return value switch
        {
            ChillMcpEntity entity => entity.ToDto(),
            ChillMcpQuery query => query.ToDto(),
            IEnumerable<ChillMcpEntity> entities => entities.Select(entity => entity.ToDto()).ToList(),
            IEnumerable<ChillMcpQuery> queries => queries.Select(query => query.ToDto()).ToList(),
            _ => value
        };
    }

    internal static object? FromDtoValue(object? value)
    {
        return value switch
        {
            ChillDtoEntity entity => FromDto(entity),
            ChillDtoQuery query => ChillMcpQuery.FromDto(query),
            IEnumerable<ChillDtoEntity> entities => entities.Select(FromDto).ToList(),
            IEnumerable<ChillDtoQuery> queries => queries.Select(ChillMcpQuery.FromDto).ToList(),
            _ => value
        };
    }
}

/// <summary>
/// MCP-specific query DTO with a compact schema for tool request and response payloads.
/// </summary>
public sealed class ChillMcpQuery
{
    public string ChillType { get; set; } = string.Empty;
    public Dictionary<string, object?> Properties { get; set; } = [];
    public List<ChillMcpProperty>? ResultProperties { get; set; }
    public ChillMcpPagination? Pagination { get; set; }
    public ChillMcpOrdering? Ordering { get; set; }
    public bool? LightweightRequired { get; set; }
    public List<ChillMcpEntity> Results { get; set; } = [];

    public ChillDtoQuery ToDto()
    {
        return new ChillDtoQuery
        {
            ChillType = ChillType,
            Properties = Properties.ToDictionary(
                item => item.Key,
                item => ChillMcpEntity.ToDtoValue(item.Value),
                StringComparer.Ordinal),
            ResultProperties = ResultProperties?.Select(property => property.ToDto()).ToList(),
            Pagination = Pagination?.ToDto(),
            Ordering = Ordering?.ToDto(),
            LightweightRequired = LightweightRequired,
            Results = Results.Select(entity => entity.ToDto()).ToList()
        };
    }

    public static ChillMcpQuery FromDto(ChillDtoQuery dto)
    {
        return new ChillMcpQuery
        {
            ChillType = dto.ChillType,
            Properties = dto.Properties.ToDictionary(
                item => item.Key,
                item => ChillMcpEntity.FromDtoValue(item.Value),
                StringComparer.Ordinal),
            ResultProperties = dto.ResultProperties?.Select(ChillMcpProperty.FromDto).ToList(),
            Pagination = dto.Pagination == null ? null : ChillMcpPagination.FromDto(dto.Pagination),
            Ordering = dto.Ordering == null ? null : ChillMcpOrdering.FromDto(dto.Ordering),
            LightweightRequired = dto.LightweightRequired,
            Results = dto.Results.Select(ChillMcpEntity.FromDto).ToList()
        };
    }
}

public sealed class ChillMcpProperty
{
    public string PropertyName { get; set; } = string.Empty;
    public List<ChillMcpProperty> SubProperties { get; set; } = [];

    public ChillDtoProperty ToDto()
    {
        return new ChillDtoProperty(PropertyName, SubProperties.Select(property => property.ToDto()).ToList());
    }

    public static ChillMcpProperty FromDto(ChillDtoProperty dto)
    {
        return new ChillMcpProperty
        {
            PropertyName = dto.PropertyName,
            SubProperties = dto.SubProperties.Select(FromDto).ToList()
        };
    }
}

public sealed class ChillMcpPagination
{
    public int Page { get; set; }
    public int PageResults { get; set; }

    public ChillPagination ToDto()
    {
        return new ChillPagination
        {
            Page = Page,
            PageResults = PageResults
        };
    }

    public static ChillMcpPagination FromDto(ChillPagination dto)
    {
        return new ChillMcpPagination
        {
            Page = dto.Page,
            PageResults = dto.PageResults
        };
    }
}

public sealed class ChillMcpOrdering
{
    public string PropertyName { get; set; } = nameof(IChillEntity.Position);
    public string Direction { get; set; } = ChillOrdering.AscendingDirection;

    public ChillOrdering ToDto()
    {
        return new ChillOrdering
        {
            PropertyName = PropertyName,
            Direction = Direction
        };
    }

    public static ChillMcpOrdering FromDto(ChillOrdering dto)
    {
        return new ChillMcpOrdering
        {
            PropertyName = dto.PropertyName,
            Direction = dto.Direction
        };
    }
}

public sealed class ChillMcpValidationError
{
    public string? FieldName { get; set; }
    public string? Message { get; set; }

    public static ChillMcpValidationError FromDto(ChillValidationError dto)
    {
        return new ChillMcpValidationError
        {
            FieldName = dto.FieldName,
            Message = dto.Message
        };
    }
}

public sealed class ChillMcpEmptyResult
{
    public bool Success { get; set; } = true;
}

public sealed class ChillMcpOperation
{
    public int Index { get; set; }
    public string? Verb { get; set; }
    public ChillMcpQuery? Query { get; set; }
    public ChillMcpEntity? Entity { get; set; }
    public List<ChillMcpValidationError>? ValidationErrors { get; set; }

    public ChillOperation ToDto()
    {
        return new ChillOperation
        {
            Index = Index,
            Verb = Verb,
            Query = Query?.ToDto(),
            Entity = Entity?.ToDto()
        };
    }

    public static ChillMcpOperation FromDto(ChillOperation dto)
    {
        return new ChillMcpOperation
        {
            Index = dto.Index,
            Verb = dto.Verb,
            Query = dto.Query == null ? null : ChillMcpQuery.FromDto(dto.Query),
            Entity = dto.Entity == null ? null : ChillMcpEntity.FromDto(dto.Entity),
            ValidationErrors = dto.ValidationErrors?.Select(ChillMcpValidationError.FromDto).ToList()
        };
    }
}
