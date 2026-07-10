namespace ForexWidget.Domain.Models;

using ForexWidget.Domain.Enums;
using System;
using System.Collections.Generic;

/// <summary>
/// Complete snapshot of market state at a given moment in UTC.
/// </summary>
public record MarketState(
    DateTimeOffset SnapshotUtc,
    bool IsWeekendClosed,
    IReadOnlyList<SessionState> Sessions,
    LiquidityLevel LiquidityLevel,
    MarketPhaseType CurrentPhase,
    string PhaseDisplayName,
    string InstitutionalActivity,   // "VERY HIGH", "HIGH", "MEDIUM", "LOW", "VERY LOW"
    TimeSpan? TimeUntilNextMilestone,
    string? NextMilestoneName
);
