namespace ForexWidget.Infrastructure.Scheduling;

using System;
using System.Windows.Threading;

/// <summary>
/// Wraps a DispatcherTimer so UI-thread periodic refresh doesn't need to be
/// instantiated directly inside ViewModels.
/// </summary>
public sealed class DispatcherScheduler : IDisposable
{
    private readonly DispatcherTimer _timer;

    public event Action? Tick;

    public DispatcherScheduler(TimeSpan interval)
    {
        _timer = new DispatcherTimer { Interval = interval };
        _timer.Tick += (_, _) => Tick?.Invoke();
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public void Dispose() => Stop();
}
