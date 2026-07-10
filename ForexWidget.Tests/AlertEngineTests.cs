namespace ForexWidget.Tests;

using ForexWidget.Core;
using ForexWidget.Domain.Enums;
using ForexWidget.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class AlertEngineTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 6, 50, 0, TimeSpan.Zero);
    private static readonly DstStatus NoDst = new(new List<DstInfo>(), false, null);

    private static SessionState Session(
        SessionName name, string displayName, double? minutesUntilOpen = null, double? minutesUntilClose = null)
        => new(name, displayName,
               minutesUntilOpen is null ? SessionStatus.Open : SessionStatus.Closed,
               new TimeOnly(7, 0), new TimeOnly(16, 0),
               minutesUntilOpen is null ? null : TimeSpan.FromMinutes(minutesUntilOpen.Value),
               minutesUntilClose is null ? null : TimeSpan.FromMinutes(minutesUntilClose.Value));

    private static MarketState State(
        IReadOnlyList<SessionState>? sessions = null,
        string? nextMilestoneName = null,
        double? minutesUntilMilestone = null)
        => new(Now, false, sessions ?? new List<SessionState>(),
               LiquidityLevel.Medium, MarketPhaseType.AsianSession, "Asian Session", "MEDIUM",
               minutesUntilMilestone is null ? null : TimeSpan.FromMinutes(minutesUntilMilestone.Value),
               nextMilestoneName);

    private static KillzoneState Killzone(string name, double minutesUntilStart)
        => new(name, false, new TimeOnly(6, 0), new TimeOnly(9, 0), "#00AA00", "MMM",
               TimeSpan.FromMinutes(minutesUntilStart), null);

    private static AlertDefinition Alert(string ev, int minutesBefore, bool enabled = true)
        => new(ev, minutesBefore, enabled);

    private readonly AlertEngine _engine = new();

    [Fact]
    public void Case1_LondonOpenIn9_5Min_AlertAt10_Fires()
    {
        var state = State(new[] { Session(SessionName.London, "London", minutesUntilOpen: 9.5) });

        var triggers = _engine.Evaluate(state, [], NoDst, [Alert("LondonOpen", 10)], Now);

        Assert.Single(triggers);
        Assert.Equal("LondonOpen", triggers[0].EventName);
        Assert.Contains("10 minutes", triggers[0].Message);
    }

    [Fact]
    public void Case2_LondonOpenIn10_5Min_AlertAt10_DoesNotFire()
    {
        var state = State(new[] { Session(SessionName.London, "London", minutesUntilOpen: 10.5) });

        var triggers = _engine.Evaluate(state, [], NoDst, [Alert("LondonOpen", 10)], Now);

        Assert.Empty(triggers);
    }

    [Fact]
    public void Case3_LondonOpenIn8_9Min_AlertAt10_DoesNotFire()
    {
        var state = State(new[] { Session(SessionName.London, "London", minutesUntilOpen: 8.9) });

        var triggers = _engine.Evaluate(state, [], NoDst, [Alert("LondonOpen", 10)], Now);

        Assert.Empty(triggers);
    }

    [Fact]
    public void Case4_DisabledAlert_NeverFiresEvenInWindow()
    {
        var state = State(new[] { Session(SessionName.London, "London", minutesUntilOpen: 9.5) });

        var triggers = _engine.Evaluate(state, [], NoDst, [Alert("LondonOpen", 10, enabled: false)], Now);

        Assert.Empty(triggers);
    }

    [Fact]
    public void Case5_KillzoneStartIn4_5Min_AlertAt5_FiresWithKillzoneName()
    {
        var killzones = new[] { Killzone("London Open", 4.5) };

        var triggers = _engine.Evaluate(State(), killzones, NoDst, [Alert("KillzoneStart", 5)], Now);

        Assert.Single(triggers);
        Assert.Equal("KillzoneStart:London Open", triggers[0].EventName);
        Assert.Contains("London Open", triggers[0].Title);
    }

    [Fact]
    public void Case6_TwoKillzonesInWindow_FiresTwoDistinctTriggers()
    {
        var killzones = new[] { Killzone("New York Open", 4.5), Killzone("London-NY Overlap", 4.2) };

        var triggers = _engine.Evaluate(State(), killzones, NoDst, [Alert("KillzoneStart", 5)], Now);

        Assert.Equal(2, triggers.Count);
        Assert.Equal(2, triggers.Select(t => t.EventName).Distinct().Count());
    }

    [Fact]
    public void Case7_WeekendCloseIn29_5Min_AlertAt30_Fires()
    {
        var state = State(nextMilestoneName: "Weekend Close", minutesUntilMilestone: 29.5);

        var triggers = _engine.Evaluate(state, [], NoDst, [Alert("WeekendClose", 30)], Now);

        Assert.Single(triggers);
        Assert.Equal("WeekendClose", triggers[0].EventName);
    }

    [Fact]
    public void Case8_NothingInWindow_ReturnsEmptyListNotNull()
    {
        var state = State(new[] { Session(SessionName.London, "London", minutesUntilOpen: 120) });
        var killzones = new[] { Killzone("London Open", 90) };

        var triggers = _engine.Evaluate(state, killzones, NoDst,
            [Alert("LondonOpen", 10), Alert("KillzoneStart", 5), Alert("WeekendClose", 30)], Now);

        Assert.NotNull(triggers);
        Assert.Empty(triggers);
    }
}
