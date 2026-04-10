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
/// DTO used by the schema module to persist and retrieve menu items.
/// </summary>
public class ChillDtoMenuItem
{
    public Guid Guid { get; set; }

    public int PositionNo { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ChillDtoMenuItem? Parent { get; set; }

    public string ComponentName { get; set; } = string.Empty;

    public string? ComponentConfigurationJson { get; set; }

    public string MenuHierarchy { get; set; } = string.Empty;
}
