/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

namespace ChillSharp;

/// <summary>
/// Holds process-wide initialization settings used by ChillSharp runtime helpers.
/// </summary>
public sealed class ChillSharpInitOptions
{
    /// <summary>
    /// Environment variable used to override the system time zone used by DTO date/time conversions.
    /// </summary>
    public const string SystemTimeZoneEnvironmentVariableName = "CHILL_SHARP_SYSTEM_TIMEZONE";

    /// <summary>
    /// Default IANA time-zone identifier used when no override is configured.
    /// </summary>
    public const string DefaultSystemTimeZoneId = "Europe/Rome";

    /// <summary>
    /// Gets the current process-wide ChillSharp initialization settings.
    /// </summary>
    public static ChillSharpInitOptions Current { get; private set; } = FromEnvironment();

    /// <summary>
    /// Gets the IANA time-zone identifier used by ChillSharp for DTO date/time conversions.
    /// </summary>
    public string SystemTimeZoneId { get; init; } = DefaultSystemTimeZoneId;

    /// <summary>
    /// Replaces the current settings or reloads them from environment variables when no explicit value is provided.
    /// </summary>
    public static void Initialize(ChillSharpInitOptions? options = null)
    {
        Current = options ?? FromEnvironment();
    }

    /// <summary>
    /// Resolves the configured ChillSharp system time zone into a runtime <see cref="TimeZoneInfo"/>.
    /// </summary>
    public static TimeZoneInfo GetSystemTimeZone()
    {
        return ResolveTimeZone(Current.SystemTimeZoneId);
    }

    /// <summary>
    /// Creates a new options snapshot using the current environment variables.
    /// </summary>
    public static ChillSharpInitOptions FromEnvironment()
    {
        var configuredTimeZoneId = Environment.GetEnvironmentVariable(SystemTimeZoneEnvironmentVariableName)?.Trim();
        return new ChillSharpInitOptions
        {
            SystemTimeZoneId = string.IsNullOrWhiteSpace(configuredTimeZoneId)
                ? DefaultSystemTimeZoneId
                : configuredTimeZoneId
        };
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (TryResolveTimeZone(timeZoneId, out var resolvedTimeZone))
        {
            return resolvedTimeZone;
        }

        if (TryResolveTimeZone(DefaultSystemTimeZoneId, out resolvedTimeZone))
        {
            return resolvedTimeZone;
        }

        return TimeZoneInfo.Local;
    }

    private static bool TryResolveTimeZone(string? timeZoneId, out TimeZoneInfo timeZone)
    {
        var normalizedTimeZoneId = timeZoneId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedTimeZoneId))
        {
            timeZone = null!;
            return false;
        }

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(normalizedTimeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(normalizedTimeZoneId, out var windowsId))
            {
                try
                {
                    timeZone = TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                    return true;
                }
                catch
                {
                }
            }
        }
        catch (InvalidTimeZoneException)
        {
        }

        timeZone = null!;
        return false;
    }
}
