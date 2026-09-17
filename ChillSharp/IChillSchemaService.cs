using ChillSharp.Dto;

namespace ChillSharp;

/// <summary>
/// Defines the contract for loading and persisting Chill DTO schemas independently from the core engine.
/// </summary>
public interface IChillSchemaService
{
    /// <summary>
    /// Loads or builds the schema for a Chill type and view code.
    /// </summary>
    /// <param name="chillType">The logical Chill type identifier.</param>
    /// <param name="chillViewCode">The logical Chill view code.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The resolved schema, or <see langword="null"/> when no schema can be resolved.</returns>
    Task<ChillDtoSchema?> GetSchemaAsync(string chillType, string chillViewCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a schema definition.
    /// </summary>
    /// <param name="schema">The schema to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted schema.</returns>
    Task<ChillDtoSchema> SetSchemaAsync(ChillDtoSchema schema, CancellationToken cancellationToken = default);
}
