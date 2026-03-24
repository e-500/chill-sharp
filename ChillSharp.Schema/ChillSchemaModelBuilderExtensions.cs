using ChillSharp.Schema.Model;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Schema;

/// <summary>
/// Provides shared EF Core model configuration for the ChillSharp schema persistence model.
/// </summary>
public static class ChillSchemaModelBuilderExtensions
{
    /// <summary>
    /// Applies the ChillSharp schema persistence model to an existing <see cref="ModelBuilder"/>.
    /// </summary>
    public static ModelBuilder AddChillSchemaModel(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChillSchemaEntry>(builder =>
        {
            builder.Property(x => x.ChillType).HasMaxLength(512);
            builder.Property(x => x.ChillViewCode).HasMaxLength(128);
            builder.Property(x => x.Json).HasMaxLength(int.MaxValue);
            builder.HasIndex(x => new { x.ChillType, x.ChillViewCode }).IsUnique();
        });

        modelBuilder.Entity<ChillEntityOptionsEntry>(builder =>
        {
            builder.Property(x => x.ChillType).HasMaxLength(512);
            builder.Property(x => x.LabelFormatString).HasMaxLength(2048);
            builder.Property(x => x.ShortLabelFormatString).HasMaxLength(2048);
            builder.Property(x => x.FullTextContentFormatString).HasMaxLength(4096);
            builder.HasIndex(x => x.ChillType).IsUnique();
        });

        return modelBuilder;
    }
}
