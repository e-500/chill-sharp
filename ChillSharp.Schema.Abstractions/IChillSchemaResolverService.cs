using ChillSharp.Schema.Contracts;

namespace ChillSharp.Schema;

/// <summary>
/// Minimal schema lookup contract shared with the ChillSharp core mapper.
/// </summary>
public interface IChillSchemaResolverService
{
    /// <summary>
    /// Loads or builds the schema for a Chill type and view code.
    /// </summary>
    IChillDtoSchema? ResolveSchema(string chillType, string chillViewCode, string? cultureName = null);

    /// <summary>
    /// Loads the runtime options for a Chill entity type.
    /// </summary>
    IChillDtoEntityOptions GetEntityOptions(string chillType);
}
