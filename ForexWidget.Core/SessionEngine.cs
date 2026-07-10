namespace ForexWidget.Core;

using ForexWidget.Domain.Enums;
using ForexWidget.Domain.Interfaces;
using ForexWidget.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

public class SessionEngine : ISessionEngine
{
    private readonly IWeekendEngine _weekendEngine;
    private readonly IDstEngine _dstEngine;

    private static readonly IReadOnlyList<SessionDefinition> BaseDefinitions =
    [
        new(SessionName.Sydney,  "Sydney",   new TimeOnly(21, 0), new TimeOnly( 6, 0), "AUS Eastern Standard Time"),
        new(SessionName.Tokyo,   "Tokyo",    new TimeOnly( 0, 0), new TimeOnly( 9, 0), "Tokyo Standard Time"),
        new(SessionName.London,  "London",   new TimeOnly( 7, 0), new TimeOnly(16, 0), "GMT Standard Time"),
        new(SessionName.NewYork, "New York", new TimeOnly(12, 0), new TimeOnly(21, 0), "Eastern Standard Time"),
    ];

    public SessionEngine(IWeekendEngine weekendEngine, IDstEngine dstEngine)
    {
        _weekendEngine = weekendEngine;
        _dstEngine = dstEngine;
    }

    public IReadOnlyList<SessionDefinition> GetSessionDefinitions() => BaseDefinitions;

    public MarketState GetMarketState(DateTimeOffset utcNow)
    {
        bool isWeekend = _weekendEngine.IsWeekendClosed(utcNow);
        var sessionStates = new List<SessionState>();

        foreach (var def in BaseDefinitions)
        {
            var (adjOpen, adjClose) = GetDstAdjustedTimes(def, utcNow);
            var status = isWeekend ? SessionStatus.Closed : (IsSessionActive(adjOpen, adjClose, TimeOnly.FromTimeSpan(utcNow.TimeOfDay)) ? SessionStatus.Open : SessionStatus.Closed);
            
            TimeSpan? untilOpen = null;
            TimeSpan? untilClose = null;

            if (!isWeekend)
            {
                var nowTime = TimeOnly.FromTimeSpan(utcNow.TimeOfDay);
                if (status == SessionStatus.Open)
                {
                    untilClose = adjClose >= nowTime ? adjClose - nowTime : new TimeOnly(23, 59, 59) - nowTime + (adjClose - TimeOnly.MinValue) + TimeSpan.FromSeconds(1);
                }
                else
                {
                    untilOpen = adjOpen >= nowTime ? adjOpen - nowTime : new TimeOnly(23, 59, 59) - nowTime + (adjOpen - TimeOnly.MinValue) + TimeSpan.FromSeconds(1);
                }
            }

            sessionStates.Add(new SessionState(def.Name, def.DisplayName, status, adjOpen, adjClose, untilOpen, untilClose));
        }

        var liquidity = CalculateLiquidity(sessionStates, isWeekend);
        var (phase, phaseName, activity) = CalculateMarketPhase(sessionStates, utcNow, isWeekend);
        var (nextMilestoneTime, nextMilestoneName) = GetNextMilestone(sessionStates, utcNow, isWeekend);

        return new MarketState(utcNow, isWeekend, sessionStates, liquidity, phase, phaseName, activity, nextMilestoneTime, nextMilestoneName);
    }

    private (TimeOnly Open, TimeOnly Close) GetDstAdjustedTimes(SessionDefinition session, DateTimeOffset utcNow)
    {
        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById(session.TimeZoneId); }
        catch { tz = FallbackToIana(session.TimeZoneId); }
        
        var baseOffset = tz.BaseUtcOffset;
        var currentOffset = tz.GetUtcOffset(utcNow);
        var dstAdjustment = currentOffset - baseOffset;
        
        // Ensure TimeSpan represents valid hours
        var adjOpen = session.OpenUtc.Add(TimeSpan.FromHours(-dstAdjustment.TotalHours));
        var adjClose = session.CloseUtc.Add(TimeSpan.FromHours(-dstAdjustment.TotalHours));

        return (adjOpen, adjClose);
    }
    
    private TimeZoneInfo FallbackToIana(string windowsId)
    {
        string ianaId = windowsId switch
        {
            "GMT Standard Time" => "Europe/London",
            "Eastern Standard Time" => "America/New_York",
            "AUS Eastern Standard Time" => "Australia/Sydney",
            "Tokyo Standard Time" => "Asia/Tokyo",
            _ => "UTC"
        };
        return TimeZoneInfo.FindSystemTimeZoneById(ianaId);
    }

    private bool IsSessionActive(TimeOnly open, TimeOnly close, TimeOnly currentUtc)
    {
        if (open < close)
        {
            return currentUtc >= open && currentUtc < close;
        }
        else
        {
            return currentUtc >= open || currentUtc <= close;
        }
    }

    private LiquidityLevel CalculateLiquidity(IReadOnlyList<SessionState> sessions, bool isWeekend)
    {
        if (isWeekend) return LiquidityLevel.VeryLow;

        bool london = sessions.Any(s => s.Name == SessionName.London && s.Status == SessionStatus.Open);
        bool ny = sessions.Any(s => s.Name == SessionName.NewYork && s.Status == SessionStatus.Open);
        bool tokyo = sessions.Any(s => s.Name == SessionName.Tokyo && s.Status == SessionStatus.Open);
        bool sydney = sessions.Any(s => s.Name == SessionName.Sydney && s.Status == SessionStatus.Open);

        if (london && ny) return LiquidityLevel.VeryHigh;
        if (london || ny) return LiquidityLevel.High;
        if (tokyo) return LiquidityLevel.Medium;
        if (sydney) return LiquidityLevel.Low;
        
        return LiquidityLevel.VeryLow;
    }

    private (MarketPhaseType Phase, string DisplayName, string Activity) CalculateMarketPhase(
        IReadOnlyList<SessionState> sessions, DateTimeOffset utcNow, bool isWeekend)
    {
        if (isWeekend) return (MarketPhaseType.WeekendClosed, "Weekend Closed", "VERY LOW");

        var london = sessions.First(s => s.Name == SessionName.London);
        var ny = sessions.First(s => s.Name == SessionName.NewYork);
        var tokyo = sessions.First(s => s.Name == SessionName.Tokyo);
        var sydney = sessions.First(s => s.Name == SessionName.Sydney);

        bool lOpen = london.Status == SessionStatus.Open;
        bool nyOpen = ny.Status == SessionStatus.Open;

        if (lOpen && nyOpen) return (MarketPhaseType.LondonNewYorkOverlap, "London/NY Overlap", "VERY HIGH");
        
        var time = TimeOnly.FromTimeSpan(utcNow.TimeOfDay);

        if (lOpen && !nyOpen)
        {
            var timeSinceOpen = time >= london.OpenUtc ? time - london.OpenUtc : new TimeOnly(23,59,59) - london.OpenUtc + (time - TimeOnly.MinValue) + TimeSpan.FromSeconds(1);
            if (timeSinceOpen.TotalHours < 2) return (MarketPhaseType.LondonOpen, "London Open", "HIGH");
            return (MarketPhaseType.LondonMid, "London Mid", "HIGH");
        }

        if (!lOpen && nyOpen) return (MarketPhaseType.NewYorkAfternoon, "New York Afternoon", "HIGH");

        if (!lOpen && london.TimeUntilOpen.HasValue)
        {
            var timeSinceClose = time >= london.CloseUtc ? time - london.CloseUtc : new TimeOnly(23,59,59) - london.CloseUtc + (time - TimeOnly.MinValue) + TimeSpan.FromSeconds(1);
            if (timeSinceClose.TotalHours < 1) return (MarketPhaseType.LondonClose, "London Close", "HIGH");
        }

        if (!nyOpen && ny.TimeUntilOpen.HasValue)
        {
            var timeSinceClose = time >= ny.CloseUtc ? time - ny.CloseUtc : new TimeOnly(23,59,59) - ny.CloseUtc + (time - TimeOnly.MinValue) + TimeSpan.FromSeconds(1);
            if (timeSinceClose.TotalHours < 1) return (MarketPhaseType.NewYorkClose, "New York Close", "MEDIUM");
        }

        if (tokyo.Status == SessionStatus.Open) return (MarketPhaseType.AsianSession, "Asian Session", "MEDIUM");
        
        if (sydney.Status == SessionStatus.Open) return (MarketPhaseType.SydneyOpen, "Sydney Open", "LOW");

        return (MarketPhaseType.Transitioning, "Transitioning", "VERY LOW");
    }

    private (TimeSpan? TimeUntil, string? MilestoneName) GetNextMilestone(
        IReadOnlyList<SessionState> sessions, DateTimeOffset utcNow, bool isWeekend)
    {
        if (isWeekend)
        {
            var timeUntil = _weekendEngine.TimeUntilOpen(utcNow);
            return (timeUntil, "Market Open");
        }

        var events = new List<(TimeSpan TimeUntil, string Name)>();
        foreach (var s in sessions)
        {
            if (s.TimeUntilOpen.HasValue) events.Add((s.TimeUntilOpen.Value, $"{s.DisplayName} Open"));
            if (s.TimeUntilClose.HasValue) events.Add((s.TimeUntilClose.Value, $"{s.DisplayName} Close"));
        }
        
        var weekendClose = _weekendEngine.TimeUntilClose(utcNow);
        if (weekendClose.HasValue) events.Add((weekendClose.Value, "Weekend Close"));

        if (!events.Any()) return (null, null);

        var next = events.OrderBy(e => e.TimeUntil).First();
        return (next.TimeUntil, next.Name);
    }
}
