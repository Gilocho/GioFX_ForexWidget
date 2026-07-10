namespace ForexWidget.Infrastructure.Scheduling;

using System;
using System.Threading.Tasks;
using System.Windows.Threading;

/// <summary>
/// Dispara una acción una vez al día a una hora UTC configurable (default 06:00 UTC).
/// Si el widget arranca después de la hora del día actual y aún no se ha disparado hoy,
/// dispara inmediatamente una vez en Start().
/// La decisión de disparo vive en <see cref="ShouldTrigger"/> (puro, testeable sin Dispatcher);
/// el DispatcherTimer es solo plumbing de WPF.
/// </summary>
public class DailyScheduler
{
    private readonly DispatcherTimer _timer;
    private readonly TimeOnly _triggerTimeUtc;
    private readonly Func<Task> _onTrigger;

    public DateTimeOffset? LastTriggeredUtc { get; private set; }

    public bool IsRunning => _timer.IsEnabled;

    public DailyScheduler(Func<Task> onTrigger, TimeOnly? triggerTimeUtc = null)
    {
        _onTrigger = onTrigger;
        _triggerTimeUtc = triggerTimeUtc ?? new TimeOnly(6, 0);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += async (_, _) => await CheckAndTriggerAsync();
    }

    public void Start()
    {
        _timer.Start();
        _ = CheckAndTriggerAsync();
    }

    public void Stop() => _timer.Stop();

    /// <summary>
    /// Lógica pura de "¿debo disparar ahora?": aún no disparado hoy y ya pasó la hora objetivo.
    /// </summary>
    public static bool ShouldTrigger(DateTimeOffset utcNow, DateTimeOffset? lastTriggeredUtc, TimeOnly triggerTimeUtc)
    {
        bool alreadyTriggeredToday = lastTriggeredUtc?.UtcDateTime.Date == utcNow.UtcDateTime.Date;
        bool pastTriggerTime = TimeOnly.FromTimeSpan(utcNow.TimeOfDay) >= triggerTimeUtc;
        return !alreadyTriggeredToday && pastTriggerTime;
    }

    public async Task CheckAndTriggerAsync()
    {
        var now = DateTimeOffset.UtcNow;
        if (ShouldTrigger(now, LastTriggeredUtc, _triggerTimeUtc))
        {
            LastTriggeredUtc = now;
            await _onTrigger();
        }
    }
}
