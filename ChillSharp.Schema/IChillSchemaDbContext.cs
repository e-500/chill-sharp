using ChillSharp.Schema.Model;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Schema;

/// <summary>
/// Defines the persistence contract required by the ChillSharp schema service.
/// </summary>
public interface IChillSchemaDbContext
{
    /// <summary>
    /// Gets the persisted schema rows.
    /// </summary>
    DbSet<ChillSchemaEntry> SchemaEntries { get; }

    /// <summary>
    /// Persists changes to the underlying store.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
