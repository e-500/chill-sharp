namespace ChillSharp.I18n.Contracts;

/// <summary>
/// Represents a request to create or update a localized text value.
/// </summary>
public sealed class SetTextRequest
{
    public Guid LabelGuid { get; set; }

    public string CultureName { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
