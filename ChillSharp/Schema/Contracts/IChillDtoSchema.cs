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

namespace ChillSharp.Schema.Contracts;

/// <summary>
/// Read-only contract for DTO schema metadata shared with ChillSharp core components.
/// </summary>
public interface IChillDtoSchema
{
    string ChillType { get; }
    string ChillViewCode { get; }
    string DisplayName { get; }
    bool EnableMCP { get; }
    string MCPDescription { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
    string? QueryRelatedChillType { get; }
    IReadOnlyList<IChillDtoPropertySchema> Properties { get; }
}
