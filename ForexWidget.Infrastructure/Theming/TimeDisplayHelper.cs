namespace ForexWidget.Infrastructure.Theming;

using System;

/// <summary>
/// Conversión visual UTC → hora local para la capa de presentación.
/// El Core sigue calculando todo en UTC; esto solo desplaza lo que se muestra.
/// Sin dependencias de WPF para poder testearse.
/// </summary>
public static class TimeDisplayHelper
{
    /// <summary>
    /// Converts a UTC TimeOnly into a "display fraction of day" (0.0-1.0),
    /// shifted by the given offset. Wraps correctly across midnight in both directions.
    /// Example: utcTime=23:00, offset=-5h → shifted to 18:00 → fraction = 0.75
    /// Example: utcTime=01:00, offset=-5h → shifted to 20:00 (previous day) → fraction ≈ 0.833
    /// </summary>
    public static double ToDisplayFraction(TimeOnly utcTime, TimeSpan offset)
    {
        double utcFraction = (utcTime.Hour * 3600 + utcTime.Minute * 60 + utcTime.Second) / 86400.0;
        double shifted = utcFraction + (offset.TotalHours / 24.0);

        shifted %= 1.0;
        if (shifted < 0) shifted += 1.0;

        return shifted;
    }

    /// <summary>
    /// Returns the offset to apply for display purposes.
    /// TimeSpan.Zero if displayMode is "UTC" (or anything other than "Local").
    /// The machine's current local UTC offset (DST-aware) if displayMode is "Local".
    /// </summary>
    public static TimeSpan GetDisplayOffset(string? displayMode, DateTimeOffset utcNow)
    {
        if (!string.Equals(displayMode, "Local", StringComparison.OrdinalIgnoreCase))
            return TimeSpan.Zero;

        return TimeZoneInfo.Local.GetUtcOffset(utcNow);
    }

    /// <summary>
    /// Human-readable label for the current display mode, e.g. "UTC" or "Local (UTC-5)".
    /// </summary>
    public static string GetDisplayLabel(string? displayMode, DateTimeOffset utcNow)
    {
        if (!string.Equals(displayMode, "Local", StringComparison.OrdinalIgnoreCase))
            return "UTC";

        var offset = TimeZoneInfo.Local.GetUtcOffset(utcNow);
        string sign = offset.TotalHours >= 0 ? "+" : "-";
        return $"Local (UTC{sign}{Math.Abs(offset.Hours)})";
    }
}
