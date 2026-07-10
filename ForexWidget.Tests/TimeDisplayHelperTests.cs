namespace ForexWidget.Tests;

using ForexWidget.Infrastructure.Theming;
using System;
using Xunit;

public class TimeDisplayHelperTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
    private const double Tolerance = 0.0001;

    [Fact]
    public void Case1_NoonUtcNoOffset_ReturnsHalf()
    {
        double result = TimeDisplayHelper.ToDisplayFraction(new TimeOnly(12, 0), TimeSpan.Zero);

        Assert.Equal(0.5, result, Tolerance);
    }

    [Fact]
    public void Case2_23UtcMinus5_ShiftsTo18()
    {
        double result = TimeDisplayHelper.ToDisplayFraction(new TimeOnly(23, 0), TimeSpan.FromHours(-5));

        Assert.Equal(18.0 / 24.0, result, Tolerance);
    }

    [Fact]
    public void Case3_01UtcMinus5_WrapsTo20PreviousDay()
    {
        double result = TimeDisplayHelper.ToDisplayFraction(new TimeOnly(1, 0), TimeSpan.FromHours(-5));

        Assert.Equal(20.0 / 24.0, result, Tolerance);
    }

    [Fact]
    public void Case4_03UtcPlus9_ShiftsToNoon()
    {
        double result = TimeDisplayHelper.ToDisplayFraction(new TimeOnly(3, 0), TimeSpan.FromHours(9));

        Assert.Equal(0.5, result, Tolerance);
    }

    [Fact]
    public void Case5_GetDisplayOffsetUtc_ReturnsZero()
    {
        Assert.Equal(TimeSpan.Zero, TimeDisplayHelper.GetDisplayOffset("UTC", Now));
    }

    [Fact]
    public void Case6_GetDisplayOffsetLocal_MatchesMachineTimeZone()
    {
        var expected = TimeZoneInfo.Local.GetUtcOffset(Now);

        Assert.Equal(expected, TimeDisplayHelper.GetDisplayOffset("Local", Now));
    }

    [Fact]
    public void Case7_GetDisplayLabelUtc_ReturnsUtc()
    {
        Assert.Equal("UTC", TimeDisplayHelper.GetDisplayLabel("UTC", Now));
    }

    [Fact]
    public void Case8_GetDisplayLabelLocal_FormatsMachineOffset()
    {
        var offset = TimeZoneInfo.Local.GetUtcOffset(Now);
        string sign = offset.TotalHours >= 0 ? "+" : "-";
        string expected = $"Local (UTC{sign}{Math.Abs(offset.Hours)})";

        Assert.Equal(expected, TimeDisplayHelper.GetDisplayLabel("Local", Now));
    }
}
