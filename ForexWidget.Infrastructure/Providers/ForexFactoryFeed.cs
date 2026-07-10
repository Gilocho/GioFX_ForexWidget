namespace ForexWidget.Infrastructure.Providers;

using ForexWidget.Domain.Models;
using ForexWidget.Infrastructure.Security;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Shared download + parse logic for the Forex Factory weekly XML feed.
/// Schema verificado contra el feed real (nfs.faireconomy.media, jul 2026):
/// &lt;weeklyevents&gt;&lt;event&gt; con &lt;title&gt;, &lt;country&gt; (código de moneda),
/// &lt;date&gt; MM-dd-yyyy, &lt;time&gt; h:mmam/pm en UTC, &lt;impact&gt; High/Medium/Low/Holiday,
/// &lt;forecast&gt;, &lt;previous&gt;.
/// </summary>
internal static class ForexFactoryFeed
{
    public const string PrimaryUrl = "https://nfs.faireconomy.media/ff_calendar_thisweek.xml";
    public const string FallbackUrl = "https://cdn-nfs.faireconomy.media/ff_calendar_thisweek.xml";

    private sealed class CachedFeed
    {
        public string Xml = "";
        public DateTimeOffset FetchedAtUtc;
    }

    // Reuso de descargas recientes, keyed por HttpClient: los dos providers
    // comparten un HttpClient en producción (una sola request por ciclo de
    // refresh — el feed rate-limita con HTTP 429 ante requests consecutivos),
    // mientras que los tests usan clientes distintos y quedan aislados.
    // 60s coincide con el Cache-Control: max-age=60 del propio feed.
    private static readonly ConditionalWeakTable<HttpClient, CachedFeed> RecentDownloads = new();

    /// <summary>
    /// Descarga el feed intentando primero PrimaryUrl y luego FallbackUrl,
    /// reusando una descarga de menos de 60s hecha con el mismo HttpClient.
    /// Nunca lanza: retorna null si ambas URLs fallan.
    /// </summary>
    public static async Task<string?> DownloadAsync(HttpClient http, CancellationToken ct = default)
    {
        if (RecentDownloads.TryGetValue(http, out var recent)
            && DateTimeOffset.UtcNow - recent.FetchedAtUtc < TimeSpan.FromSeconds(60))
            return recent.Xml;

        foreach (var url in new[] { PrimaryUrl, FallbackUrl })
        {
            try
            {
                using var response = await http.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode) continue;
                var xml = await response.Content.ReadAsStringAsync(ct);
                RecentDownloads.AddOrUpdate(http, new CachedFeed
                {
                    Xml = xml,
                    FetchedAtUtc = DateTimeOffset.UtcNow
                });
                return xml;
            }
            catch
            {
                // Timeout, DNS, TLS, cancelación... probar la siguiente URL
            }
        }
        return null;
    }

    /// <summary>
    /// Parsea el XML del feed a eventos. Lanza si el XML está malformado
    /// o contiene DOCTYPE/DTD (hardening XXE vía SafeXml) — el caller
    /// (RefreshAsync) absorbe la excepción y conserva el cache.
    /// </summary>
    public static IReadOnlyList<EconomicEvent> ParseEvents(string xml)
    {
        var doc = SafeXml.Parse(xml);
        var result = new List<EconomicEvent>();

        foreach (var ev in doc.Descendants("event"))
        {
            string title = (string?)ev.Element("title") ?? "";
            string country = ((string?)ev.Element("country") ?? "").Trim();
            string impact = ((string?)ev.Element("impact") ?? "").Trim();
            string? forecast = NullIfEmpty((string?)ev.Element("forecast"));
            string? previous = NullIfEmpty((string?)ev.Element("previous"));

            var timeUtc = ParseEventTimeUtc((string?)ev.Element("date"), (string?)ev.Element("time"));
            if (timeUtc is null) continue; // sin fecha parseable no hay evento útil

            result.Add(new EconomicEvent(
                CountryToCurrency(country), title, impact, timeUtc.Value, forecast, previous));
        }

        return result;
    }

    private static string? NullIfEmpty(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>
    /// El feed usa fecha MM-dd-yyyy y hora "h:mmam" en UTC. Horas no parseables
    /// ("All Day", "Tentative", vacío) caen a 00:00 UTC del día del evento.
    /// </summary>
    private static DateTimeOffset? ParseEventTimeUtc(string? date, string? time)
    {
        if (!DateTime.TryParseExact(date?.Trim(), "MM-dd-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            return null;

        if (DateTime.TryParseExact(time?.Trim().ToUpperInvariant(), new[] { "h:mmtt", "hh:mmtt" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            day = day.Date + t.TimeOfDay;

        return new DateTimeOffset(day, TimeSpan.Zero);
    }

    private static string CountryToCurrency(string country) => country switch
    {
        "USD" or "United States" => "USD",
        "GBP" or "United Kingdom" => "GBP",
        "JPY" or "Japan" => "JPY",
        "AUD" or "Australia" => "AUD",
        "EUR" or "Euro Zone" or "European Union" => "EUR",
        _ => country
    };
}
