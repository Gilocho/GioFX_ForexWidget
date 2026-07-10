namespace ForexWidget.Tests;

using ForexWidget.Infrastructure.Cache;
using ForexWidget.Infrastructure.Providers;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class ForexFactoryProviderTests
{
    private const string ValidXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <weeklyevents>
            <event>
                <title>Independence Day</title>
                <country>USD</country>
                <date><![CDATA[07-04-2026]]></date>
                <time><![CDATA[All Day]]></time>
                <impact><![CDATA[Holiday]]></impact>
                <forecast />
                <previous />
            </event>
            <event>
                <title>Non-Farm Payrolls</title>
                <country>USD</country>
                <date><![CDATA[07-10-2026]]></date>
                <time><![CDATA[12:30pm]]></time>
                <impact><![CDATA[High]]></impact>
                <forecast><![CDATA[110K]]></forecast>
                <previous><![CDATA[139K]]></previous>
            </event>
        </weeklyevents>
        """;

    private static string CreateTempConfigDir()
        => Path.Combine(Path.GetTempPath(), "ForexWidgetTest_" + Guid.NewGuid());

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public int CallCount { get; private set; }

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_responder(request));
        }
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new TaskCanceledException("Simulated timeout");
    }

    private static HttpResponseMessage Xml(HttpStatusCode status, string? body = null)
    {
        var response = new HttpResponseMessage(status);
        if (body is not null) response.Content = new StringContent(body);
        return response;
    }

    [Fact]
    public async Task Case1_Http200ValidXml_ReturnsTrueAndUpdatesCache()
    {
        string dir = CreateTempConfigDir();
        var cache = new HolidayCache(dir);
        var handler = new StubHandler(_ => Xml(HttpStatusCode.OK, ValidXml));
        var provider = new ForexFactoryProvider(cache, new HttpClient(handler));

        bool result = await provider.RefreshAsync();

        Assert.True(result);
        var holidays = cache.Load();
        Assert.Single(holidays);
        Assert.Equal("USD", holidays[0].Currency);
        Assert.Equal("Independence Day", holidays[0].Name);
        Assert.Equal(new DateOnly(2026, 7, 4), holidays[0].Date);
        Directory.Delete(dir, true);
    }

    [Fact]
    public async Task Case2_Http500_ReturnsFalseWithoutException()
    {
        string dir = CreateTempConfigDir();
        var cache = new HolidayCache(dir);
        var handler = new StubHandler(_ => Xml(HttpStatusCode.InternalServerError));
        var provider = new ForexFactoryProvider(cache, new HttpClient(handler));

        bool result = await provider.RefreshAsync();

        Assert.False(result);
        Directory.Delete(dir, true);
    }

    [Fact]
    public async Task Case3_Timeout_ReturnsFalseWithoutException()
    {
        string dir = CreateTempConfigDir();
        var cache = new HolidayCache(dir);
        var provider = new ForexFactoryProvider(cache, new HttpClient(new TimeoutHandler()));

        bool result = await provider.RefreshAsync();

        Assert.False(result);
        Directory.Delete(dir, true);
    }

    [Fact]
    public async Task Case4_MalformedXml_ReturnsFalseWithoutException()
    {
        string dir = CreateTempConfigDir();
        var cache = new HolidayCache(dir);
        var handler = new StubHandler(_ => Xml(HttpStatusCode.OK, "<<< this is not xml >>>"));
        var provider = new ForexFactoryProvider(cache, new HttpClient(handler));

        bool result = await provider.RefreshAsync();

        Assert.False(result);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case5_GetCachedHolidays_NeverMakesHttpCall()
    {
        string dir = CreateTempConfigDir();
        var cache = new HolidayCache(dir);
        var handler = new StubHandler(_ => Xml(HttpStatusCode.OK, ValidXml));
        var provider = new ForexFactoryProvider(cache, new HttpClient(handler));

        provider.GetCachedHolidays();
        provider.GetCachedHolidays();

        Assert.Equal(0, handler.CallCount);
        Directory.Delete(dir, true);
    }

    [Fact]
    public async Task Case6_HealthAfterSuccessfulRefresh_IsHealthyWithTimestamp()
    {
        string dir = CreateTempConfigDir();
        var cache = new HolidayCache(dir);
        var handler = new StubHandler(_ => Xml(HttpStatusCode.OK, ValidXml));
        var provider = new ForexFactoryProvider(cache, new HttpClient(handler));

        await provider.RefreshAsync();
        var health = provider.GetHealth();

        Assert.True(health.IsHealthy);
        Assert.NotNull(health.LastSuccessfulUpdate);
        Assert.Null(health.LastErrorMessage);
        Directory.Delete(dir, true);
    }
}
