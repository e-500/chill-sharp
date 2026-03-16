using ChillSharp.EF.ServiceModel.I18n;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.I18n;

/// <summary>
/// Provides shared EF Core model configuration for the ChillSharp i18n persistence model.
/// </summary>
public static class ChillI18nModelBuilderExtensions
{
    /// <summary>
    /// Applies the ChillSharp i18n persistence model to an existing <see cref="ModelBuilder"/>.
    /// </summary>
    public static ModelBuilder AddChillI18nModel(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Text>(builder =>
        {
            builder.Property(x => x.CultureCode).HasMaxLength(16);
            builder.Property(x => x.Value).HasMaxLength(int.MaxValue);
            builder.HasIndex(x => new { x.LabelGuid, x.CultureCode }).IsUnique();
        });

        return modelBuilder;
    }
}
