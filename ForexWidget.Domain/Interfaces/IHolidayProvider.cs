namespace ForexWidget.Domain.Interfaces;

using ForexWidget.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IHolidayProvider
{
    /// <summary>
    /// Returns cached holidays immediately. Never blocks on network.
    /// </summary>
    IReadOnlyList<HolidayEvent> GetCachedHolidays();

    /// <summary>
    /// Attempts to refresh the cache from the remote source.
    /// Never throws. Returns true if refresh succeeded, false if it fell back to cache.
    /// </summary>
    Task<bool> RefreshAsync(CancellationToken ct = default);

    ProviderHealth GetHealth();
}
