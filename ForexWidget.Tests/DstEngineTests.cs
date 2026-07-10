namespace ForexWidget.Tests;

using ForexWidget.Core;
using System;
using System.Linq;
using Xunit;

public class DstEngineTests
{
    private readonly DstEngine _engine = new();

    [Fact]
    public void Case1_WinterUK_NoDst_Offset0()
    {
        var date = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var info = _engine.GetRegionDstInfo("GMT Standard Time", "London", date);
        Assert.False(info.IsDaylightSavingTime);
        Assert.Equal("UTC+0", info.CurrentOffsetDisplay);
    }

    [Fact]
    public void Case2_SummerUK_HasDst_Offset1()
    {
        var date = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var info = _engine.GetRegionDstInfo("GMT Standard Time", "London", date);
        Assert.True(info.IsDaylightSavingTime);
        Assert.Equal("UTC+1", info.CurrentOffsetDisplay);
    }

    [Fact]
    public void Case3_WinterUS_NoDst_OffsetMinus5()
    {
        var date = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var info = _engine.GetRegionDstInfo("Eastern Standard Time", "New York", date);
        Assert.False(info.IsDaylightSavingTime);
        Assert.Equal("UTC-5", info.CurrentOffsetDisplay);
    }

    [Fact]
    public void Case4_SummerUS_HasDst_OffsetMinus4()
    {
        var date = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var info = _engine.GetRegionDstInfo("Eastern Standard Time", "New York", date);
        Assert.True(info.IsDaylightSavingTime);
        Assert.Equal("UTC-4", info.CurrentOffsetDisplay);
    }

    [Fact]
    public void Case5_Tokyo_NeverHasDst()
    {
        var winter = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var summer = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        Assert.False(_engine.GetRegionDstInfo("Tokyo Standard Time", "Tokyo", winter).IsDaylightSavingTime);
        Assert.False(_engine.GetRegionDstInfo("Tokyo Standard Time", "Tokyo", summer).IsDaylightSavingTime);
    }

    [Fact]
    public void Case6_TransitionWarning_ActiveBeforeUKChange()
    {
        var date = new DateTimeOffset(2026, 3, 25, 12, 0, 0, TimeSpan.Zero);
        Assert.True(_engine.IsTransitionWarningActive(date));
    }

    [Fact]
    public void Case7_TransitionWarning_InactiveInJanuary()
    {
        var date = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        Assert.False(_engine.IsTransitionWarningActive(date));
    }

    [Fact]
    public void Case8_DstStatus_Returns4Regions()
    {
        var date = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var status = _engine.GetDstStatus(date);
        Assert.Equal(4, status.Regions.Count);
    }

    [Fact]
    public void Case9_WarningMessage_MentionsCorrectRegion()
    {
        var date = new DateTimeOffset(2026, 3, 25, 12, 0, 0, TimeSpan.Zero);
        var status = _engine.GetDstStatus(date);
        Assert.NotNull(status.WarningMessage);
        Assert.Contains("London", status.WarningMessage);
    }
}
