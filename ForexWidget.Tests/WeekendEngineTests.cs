namespace ForexWidget.Tests;

using ForexWidget.Core;
using System;
using Xunit;

public class WeekendEngineTests
{
    private readonly WeekendEngine _engine = new();

    [Fact]
    public void Case1_FridayBefore22_IsOpen()
    {
        var date = new DateTimeOffset(2026, 7, 3, 21, 59, 59, TimeSpan.Zero);
        Assert.False(_engine.IsWeekendClosed(date));
    }

    [Fact]
    public void Case2_FridayExactly22_IsClosed()
    {
        var date = new DateTimeOffset(2026, 7, 3, 22, 0, 0, TimeSpan.Zero);
        Assert.True(_engine.IsWeekendClosed(date));
    }

    [Fact]
    public void Case3_SaturdayAnyTime_IsClosed()
    {
        var date = new DateTimeOffset(2026, 7, 4, 14, 0, 0, TimeSpan.Zero);
        Assert.True(_engine.IsWeekendClosed(date));
    }

    [Fact]
    public void Case4_SundayBefore22_IsClosed()
    {
        var date = new DateTimeOffset(2026, 7, 5, 21, 59, 59, TimeSpan.Zero);
        Assert.True(_engine.IsWeekendClosed(date));
    }

    [Fact]
    public void Case5_SundayExactly22_IsOpen()
    {
        var date = new DateTimeOffset(2026, 7, 5, 22, 0, 0, TimeSpan.Zero);
        Assert.False(_engine.IsWeekendClosed(date));
    }

    [Fact]
    public void Case6_MondayAnyTime_IsOpen()
    {
        var date = new DateTimeOffset(2026, 7, 6, 9, 0, 0, TimeSpan.Zero);
        Assert.False(_engine.IsWeekendClosed(date));
    }

    [Fact]
    public void Case7_GetNextOpenFromSaturday_IsNextSunday22()
    {
        var date = new DateTimeOffset(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);
        var expected = new DateTimeOffset(2026, 7, 5, 22, 0, 0, TimeSpan.Zero);
        Assert.Equal(expected, _engine.GetNextOpen(date));
    }

    [Fact]
    public void Case8_GetNextCloseFromMonday_IsNextFriday22()
    {
        var date = new DateTimeOffset(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);
        var expected = new DateTimeOffset(2026, 7, 10, 22, 0, 0, TimeSpan.Zero);
        Assert.Equal(expected, _engine.GetNextClose(date));
    }

    [Fact]
    public void Case9_TimeUntilOpenFromSaturdayNoon_IsApprox34Hours()
    {
        var date = new DateTimeOffset(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);
        var expected = TimeSpan.FromHours(34);
        Assert.Equal(expected, _engine.TimeUntilOpen(date));
    }

    [Fact]
    public void Case10_TimeUntilCloseWhenClosed_ReturnsNull()
    {
        var date = new DateTimeOffset(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);
        Assert.Null(_engine.TimeUntilClose(date));
    }
}
