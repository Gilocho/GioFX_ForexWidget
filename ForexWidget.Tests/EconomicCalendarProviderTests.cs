namespace ForexWidget.Tests;

using ForexWidget.Domain.Models;
using ForexWidget.Infrastructure.Cache;
using ForexWidget.Infrastructure.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

public class EconomicCalendarProviderTests
{
    private static string CreateTempConfigDir()
        => Path.Combine(Path.GetTempPath(), "ForexWidgetTest_" + Guid.NewGuid());

    private static readonly DateTimeOffset Now = new(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);

    private static EconomicEvent Event(string currency, string name, string impact, int hoursFromNow)
        => new(currency, name, impact, Now.AddHours(hoursFromNow), null, null);

    private static EconomicCalendarProvider ProviderWithEvents(string dir, params EconomicEvent[] events)
    {
        var cache = new EconomicCalendarCache(dir);
        cache.Save(new List<EconomicEvent>(events));
        return new EconomicCalendarProvider(cache);
    }

    [Fact]
    public void Case1_UpcomingHighImpact_ExcludesPastAndLowImpactAndUntrackedCurrencies()
    {
        string dir = CreateTempConfigDir();
        var provider = ProviderWithEvents(dir,
            Event("USD", "NFP", "High", 2),
            Event("USD", "Old CPI", "High", -3),         // pasado
            Event("EUR", "PMI", "Low", 4),               // impacto bajo
            Event("CAD", "BOC Rate", "High", 1),         // moneda no trackeada
            Event("GBP", "BOE Statement", "High", 6));

        var upcoming = provider.GetUpcomingHighImpact(Now);

        Assert.Equal(2, upcoming.Count);
        Assert.All(upcoming, e => Assert.Equal("High", e.Impact));
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case2_UpcomingHighImpact_SortedByTimeAscending()
    {
        string dir = CreateTempConfigDir();
        var provider = ProviderWithEvents(dir,
            Event("GBP", "Later", "High", 10),
            Event("USD", "Soonest", "High", 1),
            Event("JPY", "Middle", "High", 5));

        var upcoming = provider.GetUpcomingHighImpact(Now);

        Assert.Equal(new[] { "Soonest", "Middle", "Later" },
            upcoming.Select(e => e.EventName).ToArray());
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case3_UpcomingHighImpact_RespectsMaxCount()
    {
        string dir = CreateTempConfigDir();
        var provider = ProviderWithEvents(dir,
            Event("USD", "A", "High", 1),
            Event("EUR", "B", "High", 2),
            Event("GBP", "C", "High", 3),
            Event("JPY", "D", "High", 4));

        var upcoming = provider.GetUpcomingHighImpact(Now, maxCount: 2);

        Assert.Equal(2, upcoming.Count);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case4_EmptyCache_UpcomingHighImpactReturnsEmptyWithoutException()
    {
        string dir = CreateTempConfigDir();
        var provider = new EconomicCalendarProvider(new EconomicCalendarCache(dir));

        var upcoming = provider.GetUpcomingHighImpact(Now);

        Assert.NotNull(upcoming);
        Assert.Empty(upcoming);
        Directory.Delete(dir, true);
    }
}
