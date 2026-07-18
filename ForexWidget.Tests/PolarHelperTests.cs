namespace ForexWidget.Tests;

using ForexWidget.Infrastructure.Theming;
using System.Windows;
using Xunit;

public class PolarHelperTests
{
    // Los casos de PointOnCircle verifican coordenadas REALES a propósito:
    // un error de signo en el eje Y (pantalla: Y crece hacia abajo) dibujaría
    // todo el reloj en espejo vertical sin fallar ningún otro test.

    [Fact]
    public void Case1_FractionZero_IsZeroDegrees()
        => Assert.Equal(0.0, PolarHelper.FractionToDegrees(0.0), precision: 10);

    [Fact]
    public void Case2_FractionHalf_Is180Degrees()
        => Assert.Equal(180.0, PolarHelper.FractionToDegrees(0.5), precision: 10);

    [Fact]
    public void Case3_FractionQuarter_Is90Degrees()
        => Assert.Equal(90.0, PolarHelper.FractionToDegrees(0.25), precision: 10);

    [Fact]
    public void Case4_PointAtZeroDegrees_IsAboveCenter()
    {
        var p = PolarHelper.PointOnCircle(new Point(100, 100), 50, 0);

        Assert.Equal(100.0, p.X, precision: 6);
        Assert.Equal(50.0, p.Y, precision: 6);
    }

    [Fact]
    public void Case5_PointAt90Degrees_IsRightOfCenter()
    {
        var p = PolarHelper.PointOnCircle(new Point(100, 100), 50, 90);

        Assert.Equal(150.0, p.X, precision: 6);
        Assert.Equal(100.0, p.Y, precision: 6);
    }

    [Fact]
    public void Case6_IsLargeArc_TrueOver180_FalseUnder180()
    {
        Assert.True(PolarHelper.IsLargeArc(0, 270));
        Assert.False(PolarHelper.IsLargeArc(0, 90));
    }
}
