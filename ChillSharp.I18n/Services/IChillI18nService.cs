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
    Task<GetTextResponse?> GetTextAsync(GetTextRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Gets multiple localized texts.
    /// </summary>
    Task<IReadOnlyList<GetTextResponse?>> GetTextsAsync(IEnumerable<GetTextRequest> requests, CancellationToken cancellationToken);

    /// <summary>
    /// Creates or updates the localized text for the specified label and culture.
    /// </summary>
    Task<GetTextResponse> SetTextAsync(SetTextRequest request, CancellationToken cancellationToken);
}
