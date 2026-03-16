using ChillSharp.I18n.Contracts;

namespace ChillSharp.I18n.Services;

/// <summary>
/// Exposes text lookup operations for localized labels.
/// </summary>
public interface IChillI18nService
{
    /// <summary>
    /// Gets the localized text for the specified label and culture.
    /// </summary>
    Task<GetTextResponse?> GetTextAsync(Guid labelGuid, string cultureName, CancellationToken cancellationToken);

    /// <summary>
    /// Creates or updates the localized text for the specified label and culture.
    /// </summary>
    Task<GetTextResponse> SetTextAsync(SetTextRequest request, CancellationToken cancellationToken);
}
