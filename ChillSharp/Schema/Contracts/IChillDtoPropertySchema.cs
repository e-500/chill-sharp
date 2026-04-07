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

namespace ChillSharp.Schema.Contracts;

/// <summary>
/// Read-only contract for DTO property schema metadata shared with ChillSharp core components.
/// </summary>
public interface IChillDtoPropertySchema
{
    ChillDtoPropertyType PropertyType { get; }
    string Name { get; }
    string DisplayName { get; }
    string MCPDescription { get; }
    bool? IsNullable { get; }
    bool? IsReadOnly { get; }
    int? MinLength { get; }
    int? MaxLength { get; }
    long? IntegerMinValue { get; }
    long? IntegerMaxValue { get; }
    decimal? DecimalMinValue { get; }
    decimal? DecimalMaxValue { get; }
    int? DecimalPlaces { get; }
    int? Precision { get; }
    int? Scale { get; }
    string DateFormat { get; }
    string ReferenceChillType { get; }
    string ReferenceChillTypeQuery { get; }
    IReadOnlyList<string> EnumValues { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
    string CustomFormat { get; }
    string RegexPattern { get; }
}
