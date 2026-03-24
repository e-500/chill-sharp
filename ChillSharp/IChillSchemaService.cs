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
    /// <param name="cultureName">Optional explicit culture used to localize schema labels.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The resolved schema, or <see langword="null"/> when no schema can be resolved.</returns>
    Task<ChillDtoSchema?> GetSchemaAsync(string chillType, string chillViewCode, string? cultureName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a schema definition.
    /// </summary>
    /// <param name="schema">The schema to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted schema.</returns>
    Task<ChillDtoSchema> SetSchemaAsync(ChillDtoSchema schema, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the persisted runtime options for a Chill entity type.
    /// </summary>
    /// <param name="chillType">The logical Chill type identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The resolved entity options, falling back to defaults when no persisted row exists.</returns>
    Task<ChillDtoEntityOptions> GetEntityOptionsAsync(string chillType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists runtime options for a Chill entity type.
    /// </summary>
    /// <param name="entityOptions">The entity options to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted entity options.</returns>
    Task<ChillDtoEntityOptions> SetEntityOptionsAsync(ChillDtoEntityOptions entityOptions, CancellationToken cancellationToken = default);
}
