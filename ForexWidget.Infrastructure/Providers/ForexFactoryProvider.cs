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
/// Holiday provider backed by the Forex Factory weekly feed.
/// RefreshAsync nunca lanza; GetCachedHolidays nunca toca la red.
/// </summary>
public class ForexFactoryProvider : IHolidayProvider
{
    private readonly HttpClient _http;
    private readonly HolidayCache _cache;
    private DateTimeOffset? _lastSuccess;
    private string? _lastError;

    public ForexFactoryProvider(HolidayCache cache, HttpClient? httpClient = null)
    {
        _cache = cache;
        _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public IReadOnlyList<HolidayEvent> GetCachedHolidays() => _cache.Load();

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

            var holidays = ForexFactoryFeed.ParseEvents(xml)
                .Where(e => e.Impact.Equals("Holiday", StringComparison.OrdinalIgnoreCase))
                .Select(e => new HolidayEvent(e.Currency, e.EventName, DateOnly.FromDateTime(e.TimeUtc.UtcDateTime)))
                .ToList();

            _cache.Save(holidays);
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
        => new("ForexFactory", _lastError is null, _lastSuccess ?? _cache.GetLastUpdated(), _lastError);
}
