using ChillSharp.I18n.Contracts;
using System.Collections.Concurrent;

namespace ChillSharp.I18n.Services;

/// <summary>
/// Thread-safe in-memory cache for localized texts.
/// </summary>
public sealed class ChillI18nCache : IChillI18nCache
{
    private readonly ConcurrentDictionary<string, GetTextResponse> _cache = new();

    public bool TryGet(Guid labelGuid, string cultureName, out GetTextResponse? response)
    {
        return _cache.TryGetValue(MakeKey(labelGuid, cultureName), out response);
    }

    public GetTextResponse SetText(GetTextResponse response)
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        var key = MakeKey(response.LabelGuid, response.CultureName);
        _cache.AddOrUpdate(key, response, (_, _) => response);
        return response;
    }

    public void Invalidate(Guid labelGuid, string cultureName)
    {
        _cache.TryRemove(MakeKey(labelGuid, cultureName), out _);
    }

    public void InvalidateAll()
    {
        _cache.Clear();
    }

    private static string MakeKey(Guid labelGuid, string cultureName)
    {
        var normalizedCultureName = string.IsNullOrWhiteSpace(cultureName) ? "default" : cultureName.Trim();
        return $"{labelGuid:N}|{normalizedCultureName}";
    }
}
