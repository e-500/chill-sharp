using System.Globalization;
using System.Threading;

namespace ChillSharp;

/// <summary>
/// Applies culture and time-zone settings to the current asynchronous execution flow.
/// </summary>
public static class ChillSharpThreadContext
{
    private static readonly AsyncLocal<TimeZoneInfo?> AmbientTimeZone = new();

    internal static TimeZoneInfo? CurrentTimeZone => AmbientTimeZone.Value;

    /// <summary>
    /// Applies the supplied culture and time zone until the returned scope is disposed.
    /// </summary>
    public static IDisposable Begin(CultureInfo culture, TimeZoneInfo timeZone)
    {
        return Begin(culture, culture, timeZone);
    }

    /// <summary>
    /// Applies the supplied culture, UI culture, and time zone until the returned scope is disposed.
    /// </summary>
    public static IDisposable Begin(CultureInfo culture, CultureInfo uiCulture, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(uiCulture);
        ArgumentNullException.ThrowIfNull(timeZone);

        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        var previousTimeZone = AmbientTimeZone.Value;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture;
        AmbientTimeZone.Value = timeZone;

        return new Scope(previousCulture, previousUiCulture, previousTimeZone);
    }

    private sealed class Scope(
        CultureInfo previousCulture,
        CultureInfo previousUiCulture,
        TimeZoneInfo? previousTimeZone) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
            AmbientTimeZone.Value = previousTimeZone;
            _disposed = true;
        }
    }
}