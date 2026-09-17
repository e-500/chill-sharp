using ChillSharp.EF.ServiceModel.I18n;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.I18n;

/// <summary>
/// Defines the persistence contract required by the ChillSharp i18n service.
/// </summary>
public interface IChillI18nDbContext : IChillContext
{
    /// <summary>
    /// Gets the persisted localized texts.
    /// </summary>
    DbSet<Text> Texts { get; }

    /// <summary>
    /// Persists changes to the underlying store.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
