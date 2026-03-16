namespace ChillSharp.I18n.Contracts;

/// <summary>
/// Represents the localized text returned by the i18n API.
/// </summary>
public sealed class GetTextResponse
{
    public Guid LabelGuid { get; set; }

    public string CultureName { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
