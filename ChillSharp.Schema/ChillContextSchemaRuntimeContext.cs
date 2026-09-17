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

using System.Reflection;
using ChillSharp.EF;
using ChillSharp.Schema.Contracts;

namespace ChillSharp.Schema;

/// <summary>
/// Adapts an <see cref="IChillContext"/> to the narrow runtime contract required by schema services.
/// </summary>
public sealed class ChillContextSchemaRuntimeContext : IChillSchemaRuntimeContext
{
    private readonly IChillContext _context;

    public ChillContextSchemaRuntimeContext(IChillContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Assembly ModelAssembly => _context.GetType().Assembly;

    public string ChillTypePrefix => _context.GetChillTypePrefix();

    public string DefaultUserCultureName => _context.GetDefaultUserCultureName();

    public string RuntimeContextKey => _context.GetType().FullName ?? _context.GetType().Name;

    public IChillDtoSchema BuildSchema(object activatedType, string chillViewCode, string cultureName)
    {
        if (activatedType is IChillEntity chillEntity)
        {
            return ChillDtoSchema.FromIChillEntity(
                chillEntity,
                chillViewCode,
                ChillTypePrefix,
                _context,
                cultureName);
        }

        if (activatedType is IChillQuery<IChillEntity> chillQuery)
        {
            return ChillDtoSchema.FromIChillQuery(
                chillQuery,
                chillViewCode,
                ChillTypePrefix,
                _context,
                cultureName);
        }

        throw new ChillException(
            $"Activated type '{activatedType.GetType().FullName ?? activatedType.GetType().Name}' is not a Chill entity or query.");
    }
}
