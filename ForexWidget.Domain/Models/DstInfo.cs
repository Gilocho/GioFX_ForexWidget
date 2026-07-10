namespace ForexWidget.Domain.Models;

using System;

/// <summary>
/// DST status for a specific region at a point in time.
/// </summary>
public record DstInfo(
    string RegionName,
    string TimeZoneId,
    bool IsDaylightSavingTime,
    string CurrentOffsetDisplay,   // e.g. "UTC+1"
    DateTimeOffset? NextTransition,
    int? DaysUntilNextTransition
);
