namespace ForexWidget.Domain.Models;

using System;

/// <summary>
/// Runtime state of a Killzone at a specific point in time.
/// </summary>
public record KillzoneState(
    string Name,
    bool IsActive,
    TimeOnly StartUtc,
    TimeOnly EndUtc,
    string Color,
    string Methodology,
    TimeSpan? TimeUntilStart,
    TimeSpan? TimeUntilEnd
);
