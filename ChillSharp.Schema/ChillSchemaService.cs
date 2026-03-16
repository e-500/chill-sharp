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
    public async Task<ChillDtoSchema?> GetSchemaAsync(string chillType, string chillViewCode, CancellationToken cancellationToken = default)
    {
        if (_schemaCache.TryGet(chillType, chillViewCode, out ChillDtoSchema? cachedSchema))
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
                schema = BuildSchema(chillType, chillViewCode);
            }
            catch
            {
                schema = null;
            }
        }

        if (schema != null)
        {
            return _schemaCache.SetSchema(schema);
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
        return _schemaCache.SetSchema(schema);
    }

    private ChillDtoSchema BuildSchema(string chillType, string chillViewCode)
    {
        var fullChillType = PrepareFullChillType(chillType);
        var activatedType = _chillContext.GetType().Assembly.CreateInstance(fullChillType)
            ?? throw new ChillException($"Unable to activate entity for ChillType '{chillType}'");

        if (activatedType is IChillEntity chillEntity)
        {
            return ChillDtoSchema.FromIChillEntity(chillEntity, chillViewCode, _chillContext.GetChillTypePrefix(), _chillContext);
        }

        if (activatedType is IChillQuery<IChillEntity> chillQuery)
        {
            return ChillDtoSchema.FromIChillQuery(chillQuery, chillViewCode, _chillContext.GetChillTypePrefix(), _chillContext);
        }

        throw new ChillException($"Activated type '{fullChillType}' is not a Chill entity or query.");
    }

    private string PrepareFullChillType(string chillType)
    {
        var prefix = _chillContext.GetChillTypePrefix();
        if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("."))
            prefix += ".";

        var normalized = chillType?.Trim().Trim('.') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ChillException("ChillType is required to build a schema.");

        return normalized.StartsWith(prefix, StringComparison.Ordinal) ? normalized : $"{prefix}{normalized}";
    }

    private static string NormalizeKey(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "default" : value.Trim();
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

