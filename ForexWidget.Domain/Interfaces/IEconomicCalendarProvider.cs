namespace ForexWidget.Domain.Interfaces;

using ForexWidget.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IEconomicCalendarProvider
{
    IReadOnlyList<EconomicEvent> GetCachedEvents();
    Task<bool> RefreshAsync(CancellationToken ct = default);
    ProviderHealth GetHealth();

    /// <summary>
    /// Returns only High-impact events for the tracked currencies (USD, EUR, GBP, JPY),
    /// sorted by time ascending, from utcNow forward.
    /// </summary>
    IReadOnlyList<EconomicEvent> GetUpcomingHighImpact(DateTimeOffset utcNow, int maxCount = 5);
}
