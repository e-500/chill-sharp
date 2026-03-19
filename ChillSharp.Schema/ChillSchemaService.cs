using ChillSharp.Dto;
using ChillSharp.EF;
using ChillSharp.Schema.Model;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;

namespace ChillSharp.Schema;

/// <summary>
/// Default implementation of <see cref="IChillSchemaService"/> backed by EF Core persistence.
/// </summary>
public class ChillSchemaService : IChillSchemaService
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
        return _schemaCache.SetSchema(schema, NormalizeCultureName(null));
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



