namespace ForexWidget.Tests;

using ForexWidget.Domain.Models;
using ForexWidget.Infrastructure.Cache;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

public class HolidayCacheTests
{
    private static string CreateTempConfigDir()
        => Path.Combine(Path.GetTempPath(), "ForexWidgetTest_" + Guid.NewGuid());

    [Fact]
    public void Case1_LoadWithoutFile_ReturnsEmptyListWithoutException()
    {
        string dir = CreateTempConfigDir();
        var cache = new HolidayCache(dir);

        var result = cache.Load();

        Assert.NotNull(result);
        Assert.Empty(result);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case2_SaveThenLoad_ReturnsSameHolidays()
    {
        string dir = CreateTempConfigDir();
        var cache = new HolidayCache(dir);
        var holidays = new List<HolidayEvent>
        {
            new("USD", "Independence Day", new DateOnly(2026, 7, 4)),
            new("GBP", "Bank Holiday", new DateOnly(2026, 8, 31)),
        };

        cache.Save(holidays);
        var loaded = cache.Load();

        Assert.Equal(2, loaded.Count);
        Assert.Equal(holidays[0], loaded[0]);
        Assert.Equal(holidays[1], loaded[1]);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case3_CorruptFile_LoadReturnsEmptyListWithoutException()
    {
        string dir = CreateTempConfigDir();
        var cache = new HolidayCache(dir);
        File.WriteAllText(Path.Combine(dir, "cache", "holidays.json"), "{ this is not valid json !!");

        var result = cache.Load();

        Assert.NotNull(result);
        Assert.Empty(result);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case4_GetLastUpdatedWithoutMeta_ReturnsNull()
    {
        string dir = CreateTempConfigDir();
        var cache = new HolidayCache(dir);

        Assert.Null(cache.GetLastUpdated());
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case5_SaveThenGetLastUpdated_ReturnsRecentTimestamp()
    {
        string dir = CreateTempConfigDir();
        var cache = new HolidayCache(dir);

        cache.Save(new List<HolidayEvent> { new("JPY", "Marine Day", new DateOnly(2026, 7, 20)) });
        var lastUpdated = cache.GetLastUpdated();

        Assert.NotNull(lastUpdated);
        Assert.True((DateTimeOffset.UtcNow - lastUpdated.Value).Duration() < TimeSpan.FromSeconds(5));
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case6_SaveEmptyList_LoadReturnsEmptyListNotNull()
    {
        string dir = CreateTempConfigDir();
        var cache = new HolidayCache(dir);

        cache.Save(new List<HolidayEvent>());
        var loaded = cache.Load();

        Assert.NotNull(loaded);
        Assert.Empty(loaded);
        Directory.Delete(dir, true);
    }
}
