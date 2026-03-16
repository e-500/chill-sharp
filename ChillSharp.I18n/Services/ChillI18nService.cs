using ChillSharp.I18n.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.I18n.Services;

/// <summary>
/// Provides localized text lookup against the host application's DbContext.
/// </summary>
public sealed class ChillI18nService : IChillI18nService
{
    private readonly IChillI18nDbContext _context;

    public ChillI18nService(IChillI18nDbContext context)
    {
        _context = context;
    }

    public async Task<GetTextResponse?> GetTextAsync(Guid labelGuid, string cultureName, CancellationToken cancellationToken)
    {
        var normalizedCultureName = cultureName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCultureName))
        {
            throw new ArgumentException("Culture name is required.", nameof(cultureName));
        }

        var text = await _context.Texts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.LabelGuid == labelGuid && x.CultureCode == normalizedCultureName,
                cancellationToken);

        if (text is null)
        {
            return null;
        }

        return new GetTextResponse
        {
            LabelGuid = text.LabelGuid,
            CultureName = text.CultureCode,
            Value = text.Value
        };
    }
}
