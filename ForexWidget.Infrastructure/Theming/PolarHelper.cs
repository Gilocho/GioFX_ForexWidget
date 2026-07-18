namespace ForexWidget.Infrastructure.Theming;

using System;
using System.Windows;

/// <summary>
/// Geometría polar pura para la vista de reloj (Sprint 12). Convierte las
/// mismas fracciones 0.0-1.0 del día que usan las barras (TimelineMath) a
/// grados y puntos sobre un círculo. Vive junto a TimeDisplayHelper para
/// poder testearse sin referenciar ForexWidget.App.
/// </summary>
public static class PolarHelper
{
    /// <summary>
    /// Converts a fraction of day (0.0-1.0) to degrees, where 0.0 = 12 o'clock
    /// position (top), rotating clockwise. Matches standard clock-face orientation.
    /// </summary>
    public static double FractionToDegrees(double fraction) => fraction * 360.0;

    /// <summary>
    /// Computes a point on a circle of given radius, centered at 'center',
    /// at the given angle in degrees (0 = top, clockwise). El signo negativo
    /// en Y es deliberado: en pantalla Y crece hacia abajo.
    /// </summary>
    public static Point PointOnCircle(Point center, double radius, double angleDegrees)
    {
        double angleRad = angleDegrees * Math.PI / 180.0;
        return new Point(
            center.X + radius * Math.Sin(angleRad),
            center.Y - radius * Math.Cos(angleRad)
        );
    }

    /// <summary>
    /// True if the arc from startDegrees to endDegrees (clockwise) sweeps more than 180°.
    /// Needed for WPF's ArcSegment.IsLargeArc flag.
    /// </summary>
    public static bool IsLargeArc(double startDegrees, double endDegrees)
    {
        double sweep = endDegrees - startDegrees;
        if (sweep < 0) sweep += 360;
        return sweep > 180;
    }
}
