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
using ChillSharp.Schema.Model;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ChillSharp.Schema.Contracts;
using ChillSharp.Dto;

namespace ChillSharp.Schema;

/// <summary>
/// Default implementation of <see cref="IChillSchemaManagementService"/> backed by EF Core persistence.
/// </summary>
public class ChillSchemaService : IChillSchemaService, IChillSchemaResolverService
{
    private readonly IChillSchemaDbContext _schemaContext;
    private readonly IChillContext _chillContext;
    private readonly IChillSchemaCache _schemaCache;

    /// <summary>
    /// Initializes the schema service.
    /// </summary>
    public ChillSchemaService(IChillSchemaDbContext schemaContext, IChillContext chillContext, IChillSchemaCache schemaCache)
    {
        _schemaContext = schemaContext;
        _chillContext = chillContext;
        _schemaCache = schemaCache;
    }

    /// <inheritdoc />
    public IChillDtoSchema? ResolveSchema(string chillType, string chillViewCode, string? cultureName = null)
    {
        return GetSchemaAsync(chillType, chillViewCode, cultureName).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task<ChillDtoSchema?> GetSchemaAsync(string chillType, string chillViewCode, string? cultureName = null, CancellationToken cancellationToken = default)
    {
        var effectiveCultureName = NormalizeCultureName(cultureName);

        if (_schemaCache.TryGet(chillType, chillViewCode, effectiveCultureName, out ChillDtoSchema? cachedSchema))
        {
            return cachedSchema;
        }

        var normalizedType = NormalizeKey(chillType);
        var normalizedView = NormalizeKey(chillViewCode);

        var row = await _schemaContext.SchemaEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ChillType == normalizedType && x.ChillViewCode == normalizedView, cancellationToken);

        ChillDtoSchema? schema = null;
        if (row != null)
        {
            schema = JsonSerializer.Deserialize<ChillDtoSchema>(row.Json, CreateSerializerOptions());
        }
        else
        {
            try
            {
                schema = BuildSchema(chillType, chillViewCode, effectiveCultureName);
            }
            catch
            {
                schema = null;
            }
        }

        if (schema != null)
        {
            return _schemaCache.SetSchema(schema, effectiveCultureName);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<ChillDtoSchema> SetSchemaAsync(ChillDtoSchema schema, CancellationToken cancellationToken = default)
    {
        if (schema == null)
            throw new ArgumentNullException(nameof(schema));

        var chillType = NormalizeKey(schema.ChillType);
        var chillViewCode = NormalizeKey(schema.ChillViewCode);

        var row = await _schemaContext.SchemaEntries
            .FirstOrDefaultAsync(x => x.ChillType == chillType && x.ChillViewCode == chillViewCode, cancellationToken);

        if (row == null)
        {
            row = new ChillSchemaEntry
            {
                Guid = Guid.NewGuid(),
                ChillType = chillType,
                ChillViewCode = chillViewCode
            };
            _schemaContext.SchemaEntries.Add(row);
        }

        row.Json = JsonSerializer.Serialize(schema, CreateSerializerOptions());
        row.UpdatedUtc = DateTime.UtcNow;

        await _schemaContext.SaveChangesAsync(cancellationToken);
        _schemaCache.InvalidateAll();
        ChillEntityOptionsRuntimeCache.InvalidateAll();
        return _schemaCache.SetSchema(schema, NormalizeCultureName(null));
    }

    /// <inheritdoc />
    public async Task<ChillDtoEntityOptions> GetEntityOptionsAsync(string chillType, CancellationToken cancellationToken = default)
    {
        var normalizedType = NormalizeKey(chillType);

        if (_schemaCache.TryGetEntityOptions(normalizedType, out var cachedEntityOptions) && cachedEntityOptions != null)
            return cachedEntityOptions;

        var row = await _schemaContext.EntityOptionsEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ChillType == normalizedType, cancellationToken);

        if (row == null)
        {
            return _schemaCache.SetEntityOptions(CreateDefaultEntityOptions(normalizedType));
        }

        return _schemaCache.SetEntityOptions(new ChillDtoEntityOptions
        {
            ChillType = row.ChillType,
            ChecksumEnabled = row.ChecksumEnabled,
            LabelFormatString = row.LabelFormatString,
            ShortLabelFormatString = row.ShortLabelFormatString,
            FullTextContentFormatString = row.FullTextContentFormatString,
            EnableMCP = row.EnableMCP,
            MCPDescription = row.MCPDescription,
            ChangeLogEnabled = row.ChangeLogEnabled
        });
    }

    /// <inheritdoc />
    public async Task<ChillDtoEntityOptions> SetEntityOptionsAsync(ChillDtoEntityOptions entityOptions, CancellationToken cancellationToken = default)
    {
        if (entityOptions == null)
            throw new ArgumentNullException(nameof(entityOptions));

        var chillType = NormalizeKey(entityOptions.ChillType);

        var row = await _schemaContext.EntityOptionsEntries
            .FirstOrDefaultAsync(x => x.ChillType == chillType, cancellationToken);

        if (row == null)
        {
            row = new ChillEntityOptionsEntry
            {
                Guid = Guid.NewGuid(),
                ChillType = chillType
            };
            _schemaContext.EntityOptionsEntries.Add(row);
        }

        row.ChecksumEnabled = entityOptions.ChecksumEnabled;
        row.LabelFormatString = NormalizeOptionalText(entityOptions.LabelFormatString);
        row.ShortLabelFormatString = NormalizeOptionalText(entityOptions.ShortLabelFormatString);
        row.FullTextContentFormatString = NormalizeOptionalText(entityOptions.FullTextContentFormatString);
        row.EnableMCP = entityOptions.EnableMCP;
        row.MCPDescription = NormalizeOptionalText(entityOptions.MCPDescription);
        row.ChangeLogEnabled = entityOptions.ChangeLogEnabled;
        row.UpdatedUtc = DateTime.UtcNow;

        await _schemaContext.SaveChangesAsync(cancellationToken);
        _schemaCache.InvalidateEntityOptions(chillType);
        ChillEntityOptionsRuntimeCache.Invalidate(_chillContext, chillType);

        return _schemaCache.SetEntityOptions(new ChillDtoEntityOptions
        {
            ChillType = chillType,
            ChecksumEnabled = row.ChecksumEnabled,
            LabelFormatString = row.LabelFormatString,
            ShortLabelFormatString = row.ShortLabelFormatString,
            FullTextContentFormatString = row.FullTextContentFormatString,
            EnableMCP = row.EnableMCP,
            MCPDescription = row.MCPDescription,
            ChangeLogEnabled = row.ChangeLogEnabled
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChillDtoMenuItem>> GetMenuAsync(Guid? parentGuid = null, CancellationToken cancellationToken = default)
    {
        var rows = await _schemaContext.MenuItems
            .AsNoTracking()
            .Include(x => x.Parent)
            .Where(x => x.ParentGuid == parentGuid)
            .OrderBy(x => x.Title)
            .ThenBy(x => x.Guid)
            .ToListAsync(cancellationToken);

        return rows.Select(MapMenuItem).ToList();
    }

    /// <inheritdoc />
    public async Task<ChillDtoMenuItem> SetMenuAsync(ChillDtoMenuItem menuItem, CancellationToken cancellationToken = default)
    {
        if (menuItem == null)
            throw new ArgumentNullException(nameof(menuItem));

        var parentGuid = menuItem.Parent?.Guid;
        ChillMenuItemEntry? parent = null;
        if (parentGuid.HasValue && parentGuid.Value != Guid.Empty)
        {
            parent = await _schemaContext.MenuItems
                .FirstOrDefaultAsync(x => x.Guid == parentGuid.Value, cancellationToken)
                ?? throw new ArgumentException("The referenced parent menu item does not exist.");
        }

        ChillMenuItemEntry row;

        bool exists = false;
        if (menuItem.Guid != Guid.Empty)
            exists = await _schemaContext.MenuItems.AnyAsync(x => x.Guid == menuItem.Guid, cancellationToken);

        if (exists)
        {
            row = await _schemaContext.MenuItems
                .Include(x => x.Parent)
                .FirstOrDefaultAsync(x => x.Guid == menuItem.Guid, cancellationToken)
                ?? throw new ArgumentException("The referenced menu item does not exist.");
        }
        else
        {
            row = new ChillMenuItemEntry
            {
                Guid = (menuItem.Guid == Guid.Empty) ? Guid.NewGuid() : menuItem.Guid
            };
            _schemaContext.MenuItems.Add(row);
        }

        if (parent != null && parent.Guid == row.Guid)
            throw new ArgumentException("A menu item cannot be its own parent.");

        row.Title = NormalizeRequiredText(menuItem.Title, nameof(menuItem.Title), 255);
        row.Description = NormalizeOptionalText(menuItem.Description);
        row.ParentGuid = parent?.Guid;
        row.Parent = parent;
        row.ComponentName = NormalizeRequiredText(menuItem.ComponentName, nameof(menuItem.ComponentName), 255);
        row.ComponentConfigurationJson = NormalizeOptionalText(menuItem.ComponentConfigurationJson);
        row.MenuHierarchy = NormalizeRequiredText(menuItem.MenuHierarchy, nameof(menuItem.MenuHierarchy), 512);
        row.UpdatedUtc = DateTime.UtcNow;

        await _schemaContext.SaveChangesAsync(cancellationToken);

        row = await _schemaContext.MenuItems
            .AsNoTracking()
            .Include(x => x.Parent)
            .FirstAsync(x => x.Guid == row.Guid, cancellationToken);

        return MapMenuItem(row);
    }


    /// <inheritdoc />
    public async Task DeleteMenuAsync(Guid menuItemGuid, CancellationToken cancellationToken = default)
    {
        if (menuItemGuid == Guid.Empty)
            throw new ArgumentException("'menuItemGuid' is required.", nameof(menuItemGuid));

        var menuRows = await _schemaContext.MenuItems
            .ToListAsync(cancellationToken);

        var rowsByGuid = menuRows.ToDictionary(x => x.Guid);
        if (!rowsByGuid.ContainsKey(menuItemGuid))
            throw new ArgumentException("The referenced menu item does not exist.", nameof(menuItemGuid));

        var descendantGuids = new HashSet<Guid>();
        var pending = new Stack<Guid>();
        pending.Push(menuItemGuid);

        while (pending.Count > 0)
        {
            var currentGuid = pending.Pop();
            if (!descendantGuids.Add(currentGuid))
                continue;

            foreach (var childGuid in menuRows.Where(x => x.ParentGuid == currentGuid).Select(x => x.Guid))
            {
                pending.Push(childGuid);
            }
        }

        var rowsToDelete = menuRows
            .Where(x => descendantGuids.Contains(x.Guid))
            .ToList();

        _schemaContext.MenuItems.RemoveRange(rowsToDelete);
        await _schemaContext.SaveChangesAsync(cancellationToken);
    }
    private ChillDtoSchema BuildSchema(string chillType, string chillViewCode, string cultureName)
    {
        var activatedType = ChillTypeResolver.ActivateType(_chillContext.GetType().Assembly, chillType, _chillContext.GetChillTypePrefix());
        var fullChillType = PrepareFullChillType(chillType);

        if (activatedType is IChillEntity chillEntity)
        {
            return ChillDtoSchema.FromIChillEntity(chillEntity, chillViewCode, _chillContext.GetChillTypePrefix(), _chillContext, cultureName);
        }

        if (activatedType is IChillQuery<IChillEntity> chillQuery)
        {
            return ChillDtoSchema.FromIChillQuery(chillQuery, chillViewCode, _chillContext.GetChillTypePrefix(), _chillContext, cultureName);
        }

        throw new ChillException($"Activated type '{fullChillType}' is not a Chill entity or query.");
    }

    private string PrepareFullChillType(string chillType)
    {
        return ChillTypeResolver.PrepareFullChillType(chillType, _chillContext.GetChillTypePrefix());
    }

    private static string NormalizeKey(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "default" : value.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeRequiredText(string? value, string parameterName, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException($"'{parameterName}' is required.", parameterName);

        if (normalized.Length > maxLength)
            throw new ArgumentException($"'{parameterName}' cannot exceed {maxLength} characters.", parameterName);

        return normalized;
    }

    private static ChillDtoMenuItem MapMenuItem(ChillMenuItemEntry row)
    {
        return new ChillDtoMenuItem
        {
            Guid = row.Guid,
            Title = row.Title,
            Description = row.Description,
            Parent = row.Parent == null
                ? null
                : new ChillDtoMenuItem
                {
                    Guid = row.Parent.Guid,
                    Title = row.Parent.Title,
                    Description = row.Parent.Description,
                    ComponentName = row.Parent.ComponentName,
                    ComponentConfigurationJson = row.Parent.ComponentConfigurationJson,
                    MenuHierarchy = row.Parent.MenuHierarchy
                },
            ComponentName = row.ComponentName,
            ComponentConfigurationJson = row.ComponentConfigurationJson,
            MenuHierarchy = row.MenuHierarchy
        };
    }

    private ChillDtoEntityOptions CreateDefaultEntityOptions(string chillType)
    {
        var defaults = ResolveEntityAttributeDefaults(chillType);

        return new ChillDtoEntityOptions
        {
            ChillType = chillType,
            ChecksumEnabled = true,
            LabelFormatString = defaults.LabelFormatString,
            ShortLabelFormatString = defaults.ShortLabelFormatString,
            FullTextContentFormatString = defaults.FullTextContentFormatString,
            EnableMCP = defaults.EnableMCP,
            MCPDescription = defaults.MCPDescription,
            ChangeLogEnabled = false
        };
    }

    private (string? LabelFormatString, string? ShortLabelFormatString, string? FullTextContentFormatString, bool EnableMCP, string? MCPDescription) ResolveEntityAttributeDefaults(string chillType)
    {
        try
        {
            var resolvedType = ChillTypeResolver.ResolveType(_chillContext.GetType().Assembly, chillType, _chillContext.GetChillTypePrefix());
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

    private string NormalizeCultureName(string? cultureName)
    {
        return string.IsNullOrWhiteSpace(cultureName)
            ? NormalizeKey(_chillContext.GetDefaultUserCultureName())
            : NormalizeKey(cultureName);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }
}




