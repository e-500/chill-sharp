using ChillSharp.I18n.Contracts;

namespace ChillSharp.I18n.Services;

/// <summary>
/// Defines the cache contract for localized texts.
/// </summary>
public interface IChillI18nCache
{
    bool TryGet(Guid labelGuid, string cultureName, out GetTextResponse? response);

    GetTextResponse SetText(GetTextResponse response);

    void Invalidate(Guid labelGuid, string cultureName);

    void InvalidateAll();
}
