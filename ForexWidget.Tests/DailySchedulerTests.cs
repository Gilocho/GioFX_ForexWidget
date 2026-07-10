namespace ForexWidget.Tests;

using ForexWidget.Infrastructure.Scheduling;
using System;
using System.Threading.Tasks;
using Xunit;

public class DailySchedulerTests
{
    private static readonly TimeOnly SixUtc = new(6, 0);

    // ── Lógica pura ShouldTrigger (sin Dispatcher) ────────────────────

    [Fact]
    public void Case1_PastTriggerTimeNeverTriggered_ShouldTrigger()
    {
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);

        Assert.True(DailyScheduler.ShouldTrigger(now, null, SixUtc));
    }

    [Fact]
    public void Case2_AlreadyTriggeredToday_ShouldNotTriggerAgain()
    {
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);
        var triggeredEarlierToday = new DateTimeOffset(2026, 7, 9, 6, 0, 30, TimeSpan.Zero);

        Assert.False(DailyScheduler.ShouldTrigger(now, triggeredEarlierToday, SixUtc));
    }

    [Fact]
    public void Case3_BeforeTriggerTime_ShouldNotTrigger()
    {
        var now = new DateTimeOffset(2026, 7, 9, 5, 59, 0, TimeSpan.Zero);

        Assert.False(DailyScheduler.ShouldTrigger(now, null, SixUtc));
    }

    [Fact]
    public void Case4_TriggeredYesterdayPastTriggerTime_ShouldTriggerToday()
    {
        var now = new DateTimeOffset(2026, 7, 9, 6, 1, 0, TimeSpan.Zero);
        var triggeredYesterday = new DateTimeOffset(2026, 7, 8, 6, 0, 30, TimeSpan.Zero);

        Assert.True(DailyScheduler.ShouldTrigger(now, triggeredYesterday, SixUtc));
    }

    // ── Comportamiento del scheduler (callback + Stop) ────────────────

    [Fact]
    public void Case5_StartPastTriggerTime_FiresCallbackExactlyOnce()
    {
        int fired = 0;
        // Trigger a las 00:00 → cualquier hora del día ya la pasó
        var scheduler = new DailyScheduler(() => { fired++; return Task.CompletedTask; }, new TimeOnly(0, 0));

        scheduler.Start();
        scheduler.Stop();

        Assert.Equal(1, fired);
        Assert.NotNull(scheduler.LastTriggeredUtc);
    }

    [Fact]
    public async Task Case6_SecondCheckSameDay_DoesNotFireAgain()
    {
        int fired = 0;
        var scheduler = new DailyScheduler(() => { fired++; return Task.CompletedTask; }, new TimeOnly(0, 0));

        await scheduler.CheckAndTriggerAsync();
        await scheduler.CheckAndTriggerAsync();

        Assert.Equal(1, fired);
    }

    [Fact]
    public void Case7_StopDisablesTimer()
    {
        var scheduler = new DailyScheduler(() => Task.CompletedTask, new TimeOnly(23, 59));

        scheduler.Start();
        Assert.True(scheduler.IsRunning);

        scheduler.Stop();
        Assert.False(scheduler.IsRunning);
    }
}
