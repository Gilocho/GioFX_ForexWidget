namespace ForexWidget.Tests;

using ForexWidget.Core;
using ForexWidget.Domain.Enums;
using ForexWidget.Domain.Models;
using System;
using System.Linq;
using Xunit;

public class SessionEngineTests
{
    private readonly SessionEngine _engine = new(new WeekendEngine(), new DstEngine());

    [Fact]
    public void Case1_Monday0800UTC_LondonTokyoOpen()
    {
        var date = new DateTimeOffset(2026, 1, 5, 8, 0, 0, TimeSpan.Zero);
        var state = _engine.GetMarketState(date);
        
        Assert.Equal(SessionStatus.Open, state.Sessions.First(s => s.Name == SessionName.London).Status);
        Assert.Equal(SessionStatus.Open, state.Sessions.First(s => s.Name == SessionName.Tokyo).Status);
        Assert.Equal(SessionStatus.Closed, state.Sessions.First(s => s.Name == SessionName.NewYork).Status);
        Assert.Equal(SessionStatus.Closed, state.Sessions.First(s => s.Name == SessionName.Sydney).Status);
    }

    [Fact]
    public void Case2_Monday1300UTC_LondonNYOverlap()
    {
        var date = new DateTimeOffset(2026, 1, 5, 13, 0, 0, TimeSpan.Zero);
        var state = _engine.GetMarketState(date);
        
        Assert.Equal(SessionStatus.Open, state.Sessions.First(s => s.Name == SessionName.London).Status);
        Assert.Equal(SessionStatus.Open, state.Sessions.First(s => s.Name == SessionName.NewYork).Status);
        Assert.Equal(LiquidityLevel.VeryHigh, state.LiquidityLevel);
    }

    [Fact]
    public void Case3_Monday2200UTC_OnlySydneyOpen()
    {
        var date = new DateTimeOffset(2026, 1, 5, 22, 0, 0, TimeSpan.Zero);
        var state = _engine.GetMarketState(date);
        
        Assert.Equal(SessionStatus.Open, state.Sessions.First(s => s.Name == SessionName.Sydney).Status);
        Assert.Equal(SessionStatus.Closed, state.Sessions.First(s => s.Name == SessionName.Tokyo).Status);
        Assert.Equal(LiquidityLevel.Low, state.LiquidityLevel);
    }

    [Fact]
    public void Case4_Saturday1200UTC_AllClosed_IsWeekend()
    {
        var date = new DateTimeOffset(2026, 1, 3, 12, 0, 0, TimeSpan.Zero);
        var state = _engine.GetMarketState(date);
        
        Assert.True(state.IsWeekendClosed);
        Assert.All(state.Sessions, s => Assert.Equal(SessionStatus.Closed, s.Status));
    }

    [Fact]
    public void Case5_Sunday2230UTC_MarketOpened_SydneyActive()
    {
        var date = new DateTimeOffset(2026, 1, 4, 22, 30, 0, TimeSpan.Zero);
        var state = _engine.GetMarketState(date);
        
        Assert.False(state.IsWeekendClosed);
        Assert.Equal(SessionStatus.Open, state.Sessions.First(s => s.Name == SessionName.Sydney).Status);
    }

    [Fact]
    public void Case6_Sydney2300UTC_CrossMidnight_Active()
    {
        var date = new DateTimeOffset(2026, 1, 5, 23, 0, 0, TimeSpan.Zero);
        var state = _engine.GetMarketState(date);
        Assert.Equal(SessionStatus.Open, state.Sessions.First(s => s.Name == SessionName.Sydney).Status);
    }

    [Fact]
    public void Case7_Sydney0500UTC_CrossMidnight_Active()
    {
        var date = new DateTimeOffset(2026, 1, 5, 5, 0, 0, TimeSpan.Zero);
        var state = _engine.GetMarketState(date);
        Assert.Equal(SessionStatus.Open, state.Sessions.First(s => s.Name == SessionName.Sydney).Status);
    }

    [Fact]
    public void Case8_Sydney0700UTC_AlreadyClosed()
    {
        var date = new DateTimeOffset(2026, 1, 5, 7, 0, 0, TimeSpan.Zero);
        var state = _engine.GetMarketState(date);
        Assert.Equal(SessionStatus.Closed, state.Sessions.First(s => s.Name == SessionName.Sydney).Status);
    }

    [Fact]
    public void Case9_DstSummerUK_LondonOpensAt0600()
    {
        var date = new DateTimeOffset(2026, 6, 15, 6, 30, 0, TimeSpan.Zero);
        var state = _engine.GetMarketState(date);
        Assert.Equal(SessionStatus.Open, state.Sessions.First(s => s.Name == SessionName.London).Status);
    }

    [Fact]
    public void Case10_DstSummerUS_NYOpensAt1100()
    {
        var date = new DateTimeOffset(2026, 6, 15, 11, 30, 0, TimeSpan.Zero);
        var state = _engine.GetMarketState(date);
        Assert.Equal(SessionStatus.Open, state.Sessions.First(s => s.Name == SessionName.NewYork).Status);
    }

    [Fact]
    public void Case11_MarketPhase_LondonNYOverlap()
    {
        var date = new DateTimeOffset(2026, 1, 5, 13, 0, 0, TimeSpan.Zero);
        var state = _engine.GetMarketState(date);
        Assert.Equal(MarketPhaseType.LondonNewYorkOverlap, state.CurrentPhase);
    }

    [Fact]
    public void Case12_MarketPhase_WeekendClosed()
    {
        var date = new DateTimeOffset(2026, 1, 3, 12, 0, 0, TimeSpan.Zero);
        var state = _engine.GetMarketState(date);
        Assert.Equal(MarketPhaseType.WeekendClosed, state.CurrentPhase);
    }

    [Fact]
    public void Case13_NextMilestone_ReturnsValidEvent()
    {
        var date = new DateTimeOffset(2026, 1, 5, 6, 30, 0, TimeSpan.Zero);
        var state = _engine.GetMarketState(date);
        Assert.NotNull(state.NextMilestoneName);
        Assert.True(state.TimeUntilNextMilestone.HasValue);
    }
}
