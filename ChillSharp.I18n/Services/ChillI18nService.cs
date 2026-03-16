using ChillSharp.EF.ServiceModel.I18n;
using ChillSharp.I18n.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.I18n.Services;

/// <summary>
/// Provides localized text lookup against the host application's DbContext.
/// </summary>
public sealed class ChillI18nService : IChillI18nService
{
    private readonly IChillI18nDbContext _context;
    private readonly IChillI18nCache _cache;

    public ChillI18nService(IChillI18nDbContext context, IChillI18nCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<GetTextResponse?> GetTextAsync(Guid labelGuid, string cultureName, CancellationToken cancellationToken)
    {
        var normalizedCultureName = NormalizeCultureName(cultureName);
        ValidateLabelGuid(labelGuid);

        if (_cache.TryGet(labelGuid, normalizedCultureName, out var cachedResponse))
        {
            return cachedResponse;
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

        var response = MapResponse(text.LabelGuid, text.CultureCode, text.Value);
        return _cache.SetText(response);
    }

    public async Task<GetTextResponse> SetTextAsync(SetTextRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var normalizedCultureName = NormalizeCultureName(request.CultureName);
        ValidateLabelGuid(request.LabelGuid);

        var text = await _context.Texts
            .FirstOrDefaultAsync(
                x => x.LabelGuid == request.LabelGuid && x.CultureCode == normalizedCultureName,
                cancellationToken);

        if (text is null)
        {
            text = new Text
            {
                Guid = Guid.NewGuid(),
                LabelGuid = request.LabelGuid,
                CultureCode = normalizedCultureName
            };
            _context.Texts.Add(text);
        }

        text.Value = request.Value ?? string.Empty;
        await _context.SaveChangesAsync(cancellationToken);

        return _cache.SetText(MapResponse(text.LabelGuid, text.CultureCode, text.Value));
    }

    private static GetTextResponse MapResponse(Guid labelGuid, string cultureName, string value)
    {
        return new GetTextResponse
        {
            LabelGuid = labelGuid,
            CultureName = cultureName,
            Value = value
        };
    }

    private static string NormalizeCultureName(string cultureName)
    {
        var normalizedCultureName = cultureName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedCultureName))
        {
            throw new ArgumentException("Culture name is required.", nameof(cultureName));
        }

        return normalizedCultureName;
    }

    private static void ValidateLabelGuid(Guid labelGuid)
    {
        if (labelGuid == Guid.Empty)
        {
            throw new ArgumentException("Label guid is required.", nameof(labelGuid));
        }
    }
}
