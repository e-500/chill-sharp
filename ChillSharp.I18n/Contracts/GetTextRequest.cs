namespace ChillSharp.I18n.Contracts;

/// <summary>
/// Represents a localized text lookup request, optionally including seed values for the
/// configured primary and secondary cultures when the server does not know the label yet.
/// </summary>
public sealed class GetTextRequest
{
    public Guid LabelGuid { get; set; }

    public string CultureName { get; set; } = string.Empty;

    public string PrimaryCultureName { get; set; } = string.Empty;

    public string PrimaryDefaultText { get; set; } = string.Empty;

    public string SecondaryCultureName { get; set; } = string.Empty;

    public string SecondaryDefaultText { get; set; } = string.Empty;
}
