using ChillSharp.Schema.Model;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Schema;

/// <summary>
/// EF Core context containing the persisted Chill DTO schemas.
/// </summary>
public class ChillSchemaDbContext : DbContext, IChillSchemaDbContext
{
    /// <summary>
    /// Initializes a new schema context instance.
    /// </summary>
    public ChillSchemaDbContext(DbContextOptions<ChillSchemaDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets the set of persisted schema rows.
    /// </summary>
    public DbSet<ChillSchemaEntry> SchemaEntries => Set<ChillSchemaEntry>();

    /// <summary>
    /// Configures indexes and constraints for the schema persistence model.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddChillSchemaModel();
    }
}
