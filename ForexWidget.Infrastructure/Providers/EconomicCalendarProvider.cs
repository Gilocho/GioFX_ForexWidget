namespace ForexWidget.Infrastructure.Providers;

using ForexWidget.Domain.Interfaces;
using ForexWidget.Domain.Models;
using ForexWidget.Infrastructure.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Economic calendar provider backed by the Forex Factory weekly feed.
/// Mismo contrato que ForexFactoryProvider: RefreshAsync nunca lanza,
/// GetCachedEvents nunca toca la red.
/// </summary>
public class EconomicCalendarProvider : IEconomicCalendarProvider
{
    private static readonly string[] TrackedCurrencies = ["USD", "EUR", "GBP", "JPY"];

    private readonly HttpClient _http;
    private readonly EconomicCalendarCache _cache;
    private DateTimeOffset? _lastSuccess;
    private string? _lastError;

    public EconomicCalendarProvider(EconomicCalendarCache cache, HttpClient? httpClient = null)
    {
        _cache = cache;
        _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public IReadOnlyList<EconomicEvent> GetCachedEvents() => _cache.Load();

    public async Task<bool> RefreshAsync(CancellationToken ct = default)
    {
        try
        {
            var xml = await ForexFactoryFeed.DownloadAsync(_http, ct);
            if (xml is null)
            {
                _lastError = "Download failed (all URLs)";
                return false;
            }

            var events = ForexFactoryFeed.ParseEvents(xml)
                .Where(e => !e.Impact.Equals("Holiday", StringComparison.OrdinalIgnoreCase))
                .ToList();

            _cache.Save(events);
            _lastSuccess = DateTimeOffset.UtcNow;
            _lastError = null;
            return true;
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            return false;
        }
    }

    public ProviderHealth GetHealth()
        => new("EconomicCalendar", _lastError is null, _lastSuccess ?? _cache.GetLastUpdated(), _lastError);

    public IReadOnlyList<EconomicEvent> GetUpcomingHighImpact(DateTimeOffset utcNow, int maxCount = 5)
    {
        return GetCachedEvents()
            .Where(e => e.Impact.Equals("High", StringComparison.OrdinalIgnoreCase)
                        && TrackedCurrencies.Contains(e.Currency)
                        && e.TimeUtc >= utcNow)
            .OrderBy(e => e.TimeUtc)
            .Take(maxCount)
            .ToList();
    }
}
