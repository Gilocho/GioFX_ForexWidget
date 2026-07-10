namespace ForexWidget.Infrastructure.Theming;

using System;

/// <summary>
/// Geometría pura del timeline (fracciones 0.0-1.0 del día).
/// Sin dependencias de WPF para poder testearse.
/// </summary>
public static class TimelineMath
{
    /// <summary>
    /// Intersección de dos rangos [start, start+width). Retorna null si no se
    /// solapan — el contacto exacto en el borde (aEnd == bStart) NO cuenta.
    /// </summary>
    public static (double Start, double Width)? IntersectRanges(
        double aStart, double aWidth, double bStart, double bWidth)
    {
        double aEnd = aStart + aWidth;
        double bEnd = bStart + bWidth;

        double start = Math.Max(aStart, bStart);
        double end = Math.Min(aEnd, bEnd);

        if (end <= start) return null;

        return (start, end - start);
    }
}
