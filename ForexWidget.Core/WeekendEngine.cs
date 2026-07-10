namespace ForexWidget.Core;

using ForexWidget.Domain.Interfaces;
using System;

public class WeekendEngine : IWeekendEngine
{
    private static readonly TimeOnly MarketEventTime = new(22, 0, 0); // 22:00 UTC

    public bool IsWeekendClosed(DateTimeOffset utcNow)
    {
        var time = utcNow.TimeOfDay;
        var day = utcNow.DayOfWeek;

        if (day == DayOfWeek.Saturday) return true;
        if (day == DayOfWeek.Friday && time >= MarketEventTime.ToTimeSpan()) return true;
        if (day == DayOfWeek.Sunday && time < MarketEventTime.ToTimeSpan()) return true;
        
        return false;
    }

    public DateTimeOffset GetNextOpen(DateTimeOffset utcNow)
    {
        int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)utcNow.DayOfWeek + 7) % 7;
        var targetDate = utcNow.Date.AddDays(daysUntilSunday).Add(MarketEventTime.ToTimeSpan());
        
        if (daysUntilSunday == 0 && utcNow >= targetDate)
        {
            targetDate = targetDate.AddDays(7);
        }
        
        return new DateTimeOffset(targetDate, TimeSpan.Zero);
    }

    public DateTimeOffset GetNextClose(DateTimeOffset utcNow)
    {
        int daysUntilFriday = ((int)DayOfWeek.Friday - (int)utcNow.DayOfWeek + 7) % 7;
        var targetDate = utcNow.Date.AddDays(daysUntilFriday).Add(MarketEventTime.ToTimeSpan());
        
        if (daysUntilFriday == 0 && utcNow >= targetDate)
        {
            targetDate = targetDate.AddDays(7);
        }
        
        return new DateTimeOffset(targetDate, TimeSpan.Zero);
    }

    public TimeSpan? TimeUntilOpen(DateTimeOffset utcNow)
    {
        if (!IsWeekendClosed(utcNow)) return null;
        return GetNextOpen(utcNow) - utcNow;
    }

    public TimeSpan? TimeUntilClose(DateTimeOffset utcNow)
    {
        if (IsWeekendClosed(utcNow)) return null;
        return GetNextClose(utcNow) - utcNow;
    }
}
