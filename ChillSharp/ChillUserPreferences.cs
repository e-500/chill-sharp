namespace ChillSharp;

/// <summary>
/// Immutable display preferences for the user performing the current logical operation.
/// </summary>
/// <remarks>
/// A context returns <see cref="Empty"/> when the operation is unauthenticated or the host
/// does not supply user preferences.
/// </remarks>
public sealed record ChillUserPreferences(
    string DisplayCultureName,
    string DisplayTimeZone,
    string DisplayDateFormat,
    string DisplayNumberFormat,
    string PreferredTheme)
{
    /// <summary>
    /// The preference set used when no authenticated user preference set is available.
    /// </summary>
    public static readonly ChillUserPreferences Empty = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
}
