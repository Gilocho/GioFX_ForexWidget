namespace ForexWidget.Domain.Models;

using ForexWidget.Domain.Enums;
using System;

/// <summary>
/// Static definition of a Forex session. Times are in UTC (standard, non-DST).
/// The SessionEngine will apply DST offsets at runtime.
/// </summary>
public record SessionDefinition(
    SessionName Name,
    string DisplayName,
    TimeOnly OpenUtc,
    TimeOnly CloseUtc,
    string TimeZoneId  // Windows TimeZoneInfo ID for DST calculation
);
