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

namespace ChillSharp.Schema;

/// <summary>
/// Defines the cache contract for Chill schemas.
/// </summary>
public interface IChillSchemaCache
{
    bool TryGet(string chillType, string chillViewCode, string? cultureName, out ChillDtoSchema? schema);

    ChillDtoSchema SetSchema(ChillDtoSchema schema, string? cultureName);

    bool TryGetEntityOptions(string chillType, out ChillDtoEntityOptions? entityOptions);

    ChillDtoEntityOptions SetEntityOptions(ChillDtoEntityOptions entityOptions);

    void Invalidate(string chillType, string chillViewCode, string? cultureName);

    void InvalidateEntityOptions(string chillType);

    void InvalidateAll();
}
