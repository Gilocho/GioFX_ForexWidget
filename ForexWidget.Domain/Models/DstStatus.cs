namespace ForexWidget.Domain.Models;

using System.Collections.Generic;

/// <summary>
/// Aggregated DST status for all tracked Forex regions.
/// </summary>
public record DstStatus(
    IReadOnlyList<DstInfo> Regions,
    bool HasActiveTransitionWarning,  // true during the week before/after any transition
    string? WarningMessage            // e.g. "DST changed yesterday â€” London-NY overlap shifted +1h"
);
