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
