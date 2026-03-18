using ChillSharp.EF.ServiceModel.I18n;
using ChillSharp.I18n.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.I18n.Services;

/// <summary>
/// Provides localized text lookup against the host application's DbContext.
/// </summary>
public sealed class ChillI18nService : IChillI18nService
{
    private readonly IChillI18nDbContext _context;
    private readonly IChillI18nCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChillI18nService(IChillI18nDbContext context, IChillI18nCache cache, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<GetTextResponse?> GetTextAsync(GetTextRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var normalizedCultureName = NormalizeCultureName(request.CultureName);
        ValidateLabelGuid(request.LabelGuid);

        if (_cache.TryGet(request.LabelGuid, normalizedCultureName, out var cachedResponse))
        {
            return cachedResponse;
        }

        var text = await _context.Texts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.LabelGuid == request.LabelGuid && x.CultureCode == normalizedCultureName,
                cancellationToken);

        if (text is null)
        {
            if (!CanPersistMissingTexts())
            {
                return BuildDefaultResponse(request, normalizedCultureName);
            }

            if (!CanSeedDefaults(request.PrimaryCultureName, request.SecondaryCultureName))
            {
                return null;
            }

            await SeedMissingTextsAsync(
                request.LabelGuid,
                request.PrimaryDefaultText,
                request.SecondaryDefaultText,
                cancellationToken);

            text = await _context.Texts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.LabelGuid == request.LabelGuid && x.CultureCode == normalizedCultureName,
                    cancellationToken);

            if (text is null)
            {
                return null;
            }
        }

        var response = MapResponse(text.LabelGuid, text.CultureCode, text.Value);
        return _cache.SetText(response);
    }

    public async Task<IReadOnlyList<GetTextResponse?>> GetTextsAsync(IEnumerable<GetTextRequest> requests, CancellationToken cancellationToken)
    {
        if (requests is null)
        {
            throw new ArgumentNullException(nameof(requests));
        }

        var materializedRequests = requests as IList<GetTextRequest> ?? requests.ToList();
        var responses = new List<GetTextResponse?>(materializedRequests.Count);

        foreach (var request in materializedRequests)
        {
            responses.Add(await GetTextAsync(request, cancellationToken));
        }

        return responses;
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

    private bool CanPersistMissingTexts()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext is null || httpContext.User?.Identity?.IsAuthenticated == true;
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

    private GetTextResponse? BuildDefaultResponse(GetTextRequest request, string requestedCultureName)
    {
        var value = ResolveDefaultText(request, requestedCultureName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return MapResponse(request.LabelGuid, requestedCultureName, value);
    }

    private static string? ResolveDefaultText(GetTextRequest request, string requestedCultureName)
    {
        if (MatchesCulture(requestedCultureName, request.SecondaryCultureName))
        {
            return FirstAvailable(request.SecondaryDefaultText, request.PrimaryDefaultText);
        }

        if (MatchesCulture(requestedCultureName, request.PrimaryCultureName))
        {
            return FirstAvailable(request.PrimaryDefaultText, request.SecondaryDefaultText);
        }

        return FirstAvailable(request.PrimaryDefaultText, request.SecondaryDefaultText);
    }

    private static string? FirstAvailable(string? preferred, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return preferred;
        }

        return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }

    private bool CanSeedDefaults(string primaryCultureName, string secondaryCultureName)
    {
        return MatchesCulture(primaryCultureName, _context.GetPrimaryCultureName())
            && MatchesCulture(secondaryCultureName, _context.GetSecondaryCultureName());
    }

    private async Task SeedMissingTextsAsync(
        Guid labelGuid,
        string primaryDefaultText,
        string secondaryDefaultText,
        CancellationToken cancellationToken)
    {
        var configuredPrimaryCultureName = NormalizeCultureName(_context.GetPrimaryCultureName());
        var configuredSecondaryCultureName = NormalizeCultureName(_context.GetSecondaryCultureName());

        var existingTexts = await _context.Texts
            .Where(x =>
                x.LabelGuid == labelGuid
                && (x.CultureCode == configuredPrimaryCultureName || x.CultureCode == configuredSecondaryCultureName))
            .ToListAsync(cancellationToken);

        EnsureText(existingTexts, labelGuid, configuredPrimaryCultureName, primaryDefaultText);
        EnsureText(existingTexts, labelGuid, configuredSecondaryCultureName, secondaryDefaultText);

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var storedText in existingTexts)
        {
            _cache.SetText(MapResponse(storedText.LabelGuid, storedText.CultureCode, storedText.Value));
        }
    }

    private void EnsureText(List<Text> existingTexts, Guid labelGuid, string cultureName, string value)
    {
        if (existingTexts.Any(x => x.CultureCode == cultureName))
        {
            return;
        }

        var text = new Text
        {
            Guid = Guid.NewGuid(),
            LabelGuid = labelGuid,
            CultureCode = cultureName,
            Value = value ?? string.Empty
        };

        existingTexts.Add(text);
        _context.Texts.Add(text);
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

    private static bool MatchesCulture(string requestedCultureName, string configuredCultureName)
    {
        if (string.IsNullOrWhiteSpace(requestedCultureName) || string.IsNullOrWhiteSpace(configuredCultureName))
        {
            return false;
        }

        if (string.Equals(requestedCultureName.Trim(), configuredCultureName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(
            GetLanguageName(requestedCultureName),
            GetLanguageName(configuredCultureName),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string GetLanguageName(string cultureName)
    {
        var trimmedCultureName = cultureName.Trim();
        var separatorIndex = trimmedCultureName.IndexOf('-');
        return separatorIndex < 0 ? trimmedCultureName : trimmedCultureName.Substring(0, separatorIndex);
    }

    private static void ValidateLabelGuid(Guid labelGuid)
    {
        if (labelGuid == Guid.Empty)
        {
            throw new ArgumentException("Label guid is required.", nameof(labelGuid));
        }
    }
}
