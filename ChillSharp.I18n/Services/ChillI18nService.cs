/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core 
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

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
        if (materializedRequests.Count == 0)
        {
            return Array.Empty<GetTextResponse?>();
        }

        var requestEntries = new List<(GetTextRequest Request, string CultureName, GetTextResponse? Response)>(materializedRequests.Count);
        var requestedLabelGuids = new HashSet<Guid>();
        var requestedCultureNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var request in materializedRequests)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(requests), "Requests cannot contain null items.");
            }

            var normalizedCultureName = NormalizeCultureName(request.CultureName);
            ValidateLabelGuid(request.LabelGuid);

            if (_cache.TryGet(request.LabelGuid, normalizedCultureName, out var cachedResponse))
            {
                requestEntries.Add((request, normalizedCultureName, cachedResponse));
                continue;
            }

            requestEntries.Add((request, normalizedCultureName, null));
            requestedLabelGuids.Add(request.LabelGuid);
            requestedCultureNames.Add(normalizedCultureName);
        }

        var dbResponses = new Dictionary<string, GetTextResponse>(StringComparer.OrdinalIgnoreCase);
        if (requestedLabelGuids.Count > 0)
        {
            var texts = await _context.Texts
                .AsNoTracking()
                .Where(x => requestedLabelGuids.Contains(x.LabelGuid) && requestedCultureNames.Contains(x.CultureCode))
                .ToListAsync(cancellationToken);

            foreach (var text in texts)
            {
                var response = MapResponse(text.LabelGuid, text.CultureCode, text.Value);
                dbResponses[BuildTextLookupKey(text.LabelGuid, text.CultureCode)] = _cache.SetText(response);
            }
        }

        var responses = new List<GetTextResponse?>(requestEntries.Count);
        foreach (var entry in requestEntries)
        {
            if (entry.Response is not null)
            {
                responses.Add(entry.Response);
                continue;
            }

            if (dbResponses.TryGetValue(BuildTextLookupKey(entry.Request.LabelGuid, entry.CultureName), out var response))
            {
                responses.Add(response);
                continue;
            }

            if (!CanPersistMissingTexts())
            {
                responses.Add(BuildDefaultResponse(entry.Request, entry.CultureName));
                continue;
            }

            if (!CanSeedDefaults(entry.Request.PrimaryCultureName, entry.Request.SecondaryCultureName))
            {
                responses.Add(null);
                continue;
            }

            responses.Add(await GetTextAsync(entry.Request, cancellationToken));
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

    private static string BuildTextLookupKey(Guid labelGuid, string cultureName)
    {
        return $"{labelGuid:N}|{cultureName.Trim()}";
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
