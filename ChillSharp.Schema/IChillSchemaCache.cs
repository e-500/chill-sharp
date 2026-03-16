using ChillSharp.Dto;

namespace ChillSharp.Schema;

/// <summary>
/// Defines the cache contract for Chill schemas.
/// </summary>
public interface IChillSchemaCache
{
    bool TryGet(string chillType, string chillViewCode, out ChillDtoSchema? schema);

    ChillDtoSchema SetSchema(ChillDtoSchema schema);

    void Invalidate(string chillType, string chillViewCode);

    void InvalidateAll();
}
