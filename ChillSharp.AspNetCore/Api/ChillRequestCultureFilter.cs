using ChillSharp.Auth.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Text;

namespace ChillSharp.Api;

/// <summary>
/// Applies current-user display preferences before MVC model binding.
/// </summary>
public sealed class ChillRequestCultureFilter(IConfiguration configuration) : IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var preferences = context.HttpContext.RequestServices
            .GetService<IChillAuthUserPreferencesAccessor>()?.Current ?? ChillUserPreferences.Empty;

        var configuredCultureName = ReadSetting("DisplayCultureName", "CHILLSHARP_DISPLAY_CULTURE_NAME")
            ?? configuration[ChillApiOptions.CultureNameConfigurationKey];
        var configuredUiCultureName = ReadSetting("DisplayUiCultureName", "CHILLSHARP_DISPLAY_UI_CULTURE_NAME");
        var defaultCulture = CultureInfo.DefaultThreadCurrentCulture ?? CultureInfo.CurrentCulture;
        var defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture ?? CultureInfo.CurrentUICulture;
        var culture = ResolveCulture(preferences.DisplayCultureName, configuredCultureName, defaultCulture);
        var uiCulture = ResolveUiCulture(
            preferences.DisplayCultureName,
            configuredUiCultureName,
            configuredCultureName,
            defaultUiCulture);
        ApplyDateFormat(
            culture,
            preferences.DisplayDateFormat,
            ReadSetting("DisplayDateFormat", "CHILLSHARP_DISPLAY_DATE_FORMAT")
                ?? ReadSetting("DisplayTimeFormat", "CHILLSHARP_DISPLAY_TIME_FORMAT"));
        ApplyNumberFormat(
            culture,
            preferences.DisplayNumberFormat,
            ReadSetting("DisplayNumberFormat", "CHILLSHARP_DISPLAY_NUMBER_FORMAT"),
            ReadSeparator("ThousandSeparator", "CHILLSHARP_THOUSAND_SEPARATOR"),
            ReadSeparator("DecimalSeparator", "CHILLSHARP_DECIMAL_SEPARATOR"));
        var timeZone = ResolveTimeZone(
            preferences.DisplayTimeZone,
            ReadSetting("DisplayTimeZone", "CHILLSHARP_DISPLAY_TIME_ZONE")
                ?? configuration[ChillApiOptions.TimeZoneIdConfigurationKey]
                ?? Environment.GetEnvironmentVariable(ChillSharpInitOptions.SystemTimeZoneEnvironmentVariableName));

        using (ChillSharpThreadContext.Begin(culture, uiCulture, timeZone))
        {
            await next();
        }
    }

    private string? ReadSetting(string name, string environmentVariableName)
    {
        return Environment.GetEnvironmentVariable(environmentVariableName)?.Trim()
            ?? configuration[$"ChillSharp:{name}"]?.Trim()
            ?? configuration[name]?.Trim();
    }

    private string? ReadSeparator(string name, string environmentVariableName)
    {
        return Environment.GetEnvironmentVariable(environmentVariableName)
            ?? configuration[$"ChillSharp:{name}"]
            ?? configuration[name];
    }

    private static CultureInfo ResolveCulture(string? userCultureName, string? configuredCultureName, CultureInfo defaultCulture)
    {
        if (TryGetCulture(userCultureName, out var culture)
            || TryGetCulture(configuredCultureName, out culture))
        {
            return (CultureInfo)culture.Clone();
        }

        return (CultureInfo)defaultCulture.Clone();
    }

    private static CultureInfo ResolveUiCulture(
        string? userCultureName,
        string? configuredUiCultureName,
        string? configuredCultureName,
        CultureInfo defaultUiCulture)
    {
        if (TryGetCulture(userCultureName, out var culture)
            || TryGetCulture(configuredUiCultureName, out culture)
            || TryGetCulture(configuredCultureName, out culture))
        {
            return (CultureInfo)culture.Clone();
        }

        return (CultureInfo)defaultUiCulture.Clone();
    }

    private static bool TryGetCulture(string? cultureName, out CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            culture = null!;
            return false;
        }

        try
        {
            culture = CultureInfo.GetCultureInfo(cultureName.Trim());
            return true;
        }
        catch (CultureNotFoundException)
        {
            culture = null!;
            return false;
        }
    }

    private static TimeZoneInfo ResolveTimeZone(string? userTimeZoneId, string? configuredTimeZoneId)
    {
        if (ChillSharpInitOptions.TryGetTimeZone(userTimeZoneId, out var timeZone)
            || ChillSharpInitOptions.TryGetTimeZone(configuredTimeZoneId, out timeZone))
        {
            return timeZone;
        }

        return ChillSharpInitOptions.GetSystemTimeZone();
    }

    private static void ApplyDateFormat(CultureInfo culture, string? userDateFormat, string? configuredDateFormat)
    {
        foreach (var format in new[] { userDateFormat, configuredDateFormat })
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                continue;
            }

            try
            {
                culture.DateTimeFormat.ShortDatePattern = NormalizeDatePattern(format.Trim());
                return;
            }
            catch (ArgumentException)
            {
            }
        }
    }

    private static string NormalizeDatePattern(string pattern)
    {
        var result = new StringBuilder(pattern.Length);
        var quote = '\0';
        var escaped = false;

        foreach (var character in pattern)
        {
            if (escaped)
            {
                result.Append(character);
                escaped = false;
                continue;
            }

            if (character == '\\' && quote == '\0')
            {
                result.Append(character);
                escaped = true;
                continue;
            }

            if (quote != '\0')
            {
                if (character == quote)
                {
                    quote = '\0';
                }

                result.Append(character);
                continue;
            }

            if (character is '\'' or '"')
            {
                quote = character;
                result.Append(character);
                continue;
            }

            result.Append(character switch
            {
                'D' => 'd',
                'Y' => 'y',
                _ => character
            });
        }

        return result.ToString();
    }

    private static void ApplyNumberFormat(
        CultureInfo culture,
        string? userNumberFormat,
        string? configuredNumberFormat,
        string? configuredThousandSeparator,
        string? configuredDecimalSeparator)
    {
        var userSeparators = ParseNumberSeparators(userNumberFormat);
        var configuredSeparators = ParseNumberSeparators(configuredNumberFormat);
        ApplyGroupSeparator(culture, userSeparators.Group, configuredThousandSeparator, configuredSeparators.Group);
        ApplyDecimalSeparator(culture, userSeparators.Decimal, configuredDecimalSeparator, configuredSeparators.Decimal);
    }

    private static void ApplyGroupSeparator(CultureInfo culture, params string?[] candidates)
    {
        foreach (var separator in candidates)
        {
            if (separator == null || separator.Contains('\0') || separator.Any(char.IsDigit))
            {
                continue;
            }

            var numberFormat = (NumberFormatInfo)culture.NumberFormat.Clone();
            try
            {
                numberFormat.NumberGroupSeparator = separator;
                numberFormat.CurrencyGroupSeparator = separator;
                numberFormat.PercentGroupSeparator = separator;
                culture.NumberFormat = numberFormat;
                return;
            }
            catch (ArgumentException)
            {
            }
        }
    }

    private static void ApplyDecimalSeparator(CultureInfo culture, params string?[] candidates)
    {
        foreach (var separator in candidates)
        {
            if (string.IsNullOrEmpty(separator) || separator.Contains('\0') || separator.Any(char.IsDigit))
            {
                continue;
            }

            var numberFormat = (NumberFormatInfo)culture.NumberFormat.Clone();
            try
            {
                numberFormat.NumberDecimalSeparator = separator;
                numberFormat.CurrencyDecimalSeparator = separator;
                numberFormat.PercentDecimalSeparator = separator;
                culture.NumberFormat = numberFormat;
                return;
            }
            catch (ArgumentException)
            {
            }
        }
    }

    private static (string? Group, string? Decimal) ParseNumberSeparators(string? sample)
    {
        if (string.IsNullOrWhiteSpace(sample))
        {
            return (null, null);
        }

        var digitRuns = new List<string>();
        var separators = new List<string>();
        var runStart = -1;
        var previousRunEnd = -1;

        for (var index = 0; index <= sample.Length; index++)
        {
            var isDigit = index < sample.Length && char.IsDigit(sample[index]);
            if (isDigit && runStart < 0)
            {
                if (previousRunEnd >= 0)
                {
                    separators.Add(sample[previousRunEnd..index]);
                }

                runStart = index;
            }
            else if (!isDigit && runStart >= 0)
            {
                digitRuns.Add(sample[runStart..index]);
                previousRunEnd = index;
                runStart = -1;
            }
        }

        if (digitRuns.Count < 2 || separators.Count != digitRuns.Count - 1)
        {
            return (null, null);
        }

        var allGroups = digitRuns.Skip(1).All(run => run.Length == 3)
            && separators.All(separator => separator == separators[0]);
        if (allGroups)
        {
            return (separators[0], null);
        }

        return (separators.Count > 1 ? separators[0] : null, separators[^1]);
    }
}