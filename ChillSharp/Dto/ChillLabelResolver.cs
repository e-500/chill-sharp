using System.Globalization;

namespace ChillSharp.Dto;

/// <summary>
/// Resolves whether Chill metadata should expose the primary or secondary label for the active UI culture.
/// </summary>
/// <remarks>
/// Resolution is context-aware: each <see cref="IChillContext"/> can define its own primary and secondary
/// culture names, so multiple Chill contexts can coexist with different language conventions.
/// </remarks>
internal static class ChillLabelResolver
{
    /// <summary>
    /// Chooses the best label for the current UI culture using the context's configured primary and secondary cultures.
    /// </summary>
    /// <param name="primaryLabel">The label authored as the primary language value.</param>
    /// <param name="secondaryLabel">The label authored as the secondary language value.</param>
    /// <param name="fallbackLabel">Fallback text used when no authored labels are available.</param>
    /// <param name="context">The active Chill context that defines which cultures map to primary and secondary labels.</param>
    /// <returns>The label that best matches the active UI culture.</returns>
    public static string Resolve(string? primaryLabel, string? secondaryLabel, string fallbackLabel, IChillContext? context)
    {
        if (context == null)
        {
            return FirstAvailable(primaryLabel, secondaryLabel, fallbackLabel);
        }

        var currentCultureName = CultureInfo.CurrentUICulture.Name;
        if (MatchesCulture(currentCultureName, context.GetSecondaryCultureName()))
        {
            return FirstAvailable(secondaryLabel, primaryLabel, fallbackLabel);
        }

        if (MatchesCulture(currentCultureName, context.GetPrimaryCultureName()))
        {
            return FirstAvailable(primaryLabel, secondaryLabel, fallbackLabel);
        }

        return FirstAvailable(primaryLabel, secondaryLabel, fallbackLabel);
    }

    /// <summary>
    /// Matches either the full culture name or just the neutral language portion.
    /// </summary>
    private static bool MatchesCulture(string currentCultureName, string configuredCultureName)
    {
        if (string.IsNullOrWhiteSpace(currentCultureName) || string.IsNullOrWhiteSpace(configuredCultureName))
        {
            return false;
        }

        if (string.Equals(currentCultureName, configuredCultureName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var currentLanguage = GetLanguageName(currentCultureName);
        var configuredLanguage = GetLanguageName(configuredCultureName);
        return string.Equals(currentLanguage, configuredLanguage, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts the neutral language part from a culture name.
    /// </summary>
    private static string GetLanguageName(string cultureName)
    {
        var separatorIndex = cultureName.IndexOf('-');
        return separatorIndex < 0 ? cultureName : cultureName.Substring(0, separatorIndex);
    }

    /// <summary>
    /// Returns the preferred label when present, otherwise the alternate or final fallback.
    /// </summary>
    private static string FirstAvailable(string? preferred, string? alternate, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return preferred!;
        }

        if (!string.IsNullOrWhiteSpace(alternate))
        {
            return alternate!;
        }

        return fallback;
    }
}
