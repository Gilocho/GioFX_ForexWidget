namespace ForexWidget.Infrastructure.Configuration;

using System;
using System.IO;

public static class ConfigurationPaths
{
    public static string GetConfigDirectory(string? overrideDirectory = null)
        => overrideDirectory ?? Path.Combine(AppPaths.DataRoot, "Config");

    public static string Settings(string? dir = null)  => Path.Combine(GetConfigDirectory(dir), "settings.json");
    public static string Killzones(string? dir = null) => Path.Combine(GetConfigDirectory(dir), "killzones.json");
    public static string Alerts(string? dir = null)    => Path.Combine(GetConfigDirectory(dir), "alerts.json");
    public static string Theme(string? dir = null)     => Path.Combine(GetConfigDirectory(dir), "theme.json");
}
