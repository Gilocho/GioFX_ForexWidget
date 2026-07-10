namespace ForexWidget.Core;

using ForexWidget.Domain.Interfaces;
using ForexWidget.Domain.Models;
using System;
using System.Collections.Generic;

public class DstEngine : IDstEngine
{
    private static readonly (string TimeZoneId, string RegionName)[] TrackedRegions =
    [
        ("GMT Standard Time",       "London"),
        ("Eastern Standard Time",   "New York"),
        ("AUS Eastern Standard Time","Sydney"),
        ("Tokyo Standard Time",     "Tokyo")
    ];

    public DstStatus GetDstStatus(DateTimeOffset utcNow)
    {
        var regions = new List<DstInfo>();
        bool hasWarning = false;
        string? warningMessage = null;

        foreach (var (tzId, name) in TrackedRegions)
        {
            var info = GetRegionDstInfo(tzId, name, utcNow);
            regions.Add(info);

            if (info.DaysUntilNextTransition.HasValue)
            {
                int days = info.DaysUntilNextTransition.Value;
                if (days >= -2 && days <= 7)
                {
                    hasWarning = true;
                    if (days >= 0)
                        warningMessage = $"DST changes in {name} in {days} days.";
                    else
                        warningMessage = $"DST changed in {name} {-days} days ago.";
                }
            }
        }

        return new DstStatus(regions, hasWarning, warningMessage);
    }

    public DstInfo GetRegionDstInfo(string timeZoneId, string regionName, DateTimeOffset utcNow)
    {
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            tz = FallbackToIana(timeZoneId);
        }

        bool isDst = tz.IsDaylightSavingTime(utcNow);
        var currentOffset = tz.GetUtcOffset(utcNow);
        
        var nextTransition = GetNextTransition(tz, utcNow);
        int? daysUntil = nextTransition.HasValue ? (int)Math.Ceiling((nextTransition.Value - utcNow).TotalDays) : null;

        return new DstInfo(
            RegionName: regionName,
            TimeZoneId: tz.Id,
            IsDaylightSavingTime: isDst,
            CurrentOffsetDisplay: FormatOffset(currentOffset),
            NextTransition: nextTransition,
            DaysUntilNextTransition: daysUntil
        );
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

    public bool IsTransitionWarningActive(DateTimeOffset utcNow)
    {
        return GetDstStatus(utcNow).HasActiveTransitionWarning;
    }

    private DateTimeOffset? GetNextTransition(TimeZoneInfo tz, DateTimeOffset utcNow)
    {
        if (!tz.SupportsDaylightSavingTime) return null;

        var startRules = tz.GetAdjustmentRules();
        if (startRules.Length == 0) return null;

        bool currentIsDst = tz.IsDaylightSavingTime(utcNow);

        for (int i = 1; i <= 400; i++)
        {
            var checkDate = utcNow.AddDays(i);
            if (tz.IsDaylightSavingTime(checkDate) != currentIsDst)
            {
                var dayStart = checkDate.Date;
                for(int h = 0; h < 24; h++)
                {
                    var testTime = new DateTimeOffset(dayStart.AddHours(h), TimeSpan.Zero);
                    if (tz.IsDaylightSavingTime(testTime) != currentIsDst)
                    {
                        return testTime;
                    }
                }
                return new DateTimeOffset(dayStart, TimeSpan.Zero);
            }
        }
        return null;
    }

    private static string FormatOffset(TimeSpan offset)
    {
        if (offset == TimeSpan.Zero) return "UTC+0";
        string sign = offset.Ticks > 0 ? "+" : "-";
        return $"UTC{sign}{Math.Abs(offset.Hours)}";
    }
}
