namespace ForexWidget.Domain.Models;

using System;

/// <summary>
/// Static definition of a Killzone loaded from killzones.json.
/// All times are in UTC.
/// </summary>
public record KillzoneDefinition(
    string Name,
    TimeOnly StartUtc,
    TimeOnly EndUtc,
    string Color,
    string Methodology,
    bool Enabled
);
