namespace ForexWidget.Infrastructure;

using Microsoft.Win32;
using System;

/// <summary>
/// "Start with Windows" vía HKCU Run key — no requiere admin.
/// El nombre del valor es parametrizable para que los tests no toquen
/// la entrada real de producción.
/// </summary>
public class StartupRegistryService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _appName;

    public StartupRegistryService(string appName = "ForexWidget")
    {
        _appName = appName;
    }

    public void SetStartWithWindows(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (enabled)
            key?.SetValue(_appName, Environment.ProcessPath ?? "");
        else
            key?.DeleteValue(_appName, throwOnMissingValue: false);
    }

    public bool IsStartWithWindowsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(_appName) is not null;
    }
}
