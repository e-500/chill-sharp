using System.Collections.Concurrent;

namespace ChillSharp.Auth.Services;

/// <summary>
/// Stores immutable display-preference snapshots by external identity identifier.
/// </summary>
public interface IChillAuthUserPreferencesCache
{
    bool TryGet(string externalId, out ChillUserPreferences? preferences);
    void Set(string externalId, ChillUserPreferences preferences);
    void Invalidate(string externalId);
    void InvalidateAll();
}

/// <summary>
/// In-memory cache for the display preferences used by authenticated Chill operations.
/// </summary>
public sealed class ChillAuthUserPreferencesCache : IChillAuthUserPreferencesCache
{
    private readonly ConcurrentDictionary<string, ChillUserPreferences> _cache = new(StringComparer.Ordinal);

    public bool TryGet(string externalId, out ChillUserPreferences? preferences)
    {
        return _cache.TryGetValue(Normalize(externalId), out preferences);
    }

    public void Set(string externalId, ChillUserPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        _cache[Normalize(externalId)] = preferences;
    }

    public void Invalidate(string externalId)
    {
        _cache.TryRemove(Normalize(externalId), out _);
    }

    public void InvalidateAll()
    {
        _cache.Clear();
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}

/// <summary>
/// Resolves the cached display preferences for the principal on the current request.
/// </summary>
public interface IChillAuthUserPreferencesAccessor
{
    ChillUserPreferences Current { get; }
}
