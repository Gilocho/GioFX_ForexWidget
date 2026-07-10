namespace ForexWidget.Tests;

using ForexWidget.Infrastructure.Configuration;
using System;
using System.IO;
using System.Linq;
using Xunit;

public class ConfigurationLoaderTests
{
    private static string CreateTempConfigDir()
        => Path.Combine(Path.GetTempPath(), "ForexWidgetTest_" + Guid.NewGuid());

    [Fact]
    public void Case1_SettingsNotFound_ReturnsDefaultWithoutException()
    {
        string dir = CreateTempConfigDir();
        var loader = new ConfigurationLoader(dir);
        var settings = loader.LoadSettings();

        Assert.NotNull(settings);
        Assert.Equal("Dark", settings.Theme);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case2_SettingsNotFound_CreatesFileOnDisk()
    {
        string dir = CreateTempConfigDir();
        var loader = new ConfigurationLoader(dir);
        loader.LoadSettings();

        string path = ConfigurationPaths.Settings(dir);
        Assert.True(File.Exists(path));
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case3_KillzonesNotFound_Returns5Defaults()
    {
        string dir = CreateTempConfigDir();
        var loader = new ConfigurationLoader(dir);
        var kzs = loader.LoadKillzones();

        Assert.Equal(5, kzs.Count);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case4_AlertsNotFound_Returns7Alerts()
    {
        string dir = CreateTempConfigDir();
        var loader = new ConfigurationLoader(dir);
        var alerts = loader.LoadAlerts();

        Assert.Equal(7, alerts.Count);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case5_SettingsWithLightTheme_ReturnsLightTheme()
    {
        string dir = CreateTempConfigDir();
        Directory.CreateDirectory(dir);
        string path = ConfigurationPaths.Settings(dir);
        File.WriteAllText(path, """
            {
              "Theme": "Light"
            }
            """);

        var loader = new ConfigurationLoader(dir);
        var settings = loader.LoadSettings();

        Assert.Equal("Light", settings.Theme);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case6_KillzonesWithCustom_Returns1Custom()
    {
        string dir = CreateTempConfigDir();
        Directory.CreateDirectory(dir);
        string path = ConfigurationPaths.Killzones(dir);
        File.WriteAllText(path, """
            {
              "Killzones": [
                {
                  "Name": "Custom KZ",
                  "StartUtc": "10:00",
                  "EndUtc": "11:00",
                  "Color": "#FFFFFF",
                  "Methodology": "MMM",
                  "Enabled": true
                }
              ]
            }
            """);

        var loader = new ConfigurationLoader(dir);
        var kzs = loader.LoadKillzones();

        Assert.Single(kzs);
        Assert.Equal("Custom KZ", kzs[0].Name);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case7_KillzoneDisabled_ReturnsDisabled()
    {
        string dir = CreateTempConfigDir();
        Directory.CreateDirectory(dir);
        string path = ConfigurationPaths.Killzones(dir);
        File.WriteAllText(path, """
            {
              "Killzones": [
                {
                  "Name": "Custom KZ",
                  "StartUtc": "10:00",
                  "EndUtc": "11:00",
                  "Color": "#FFFFFF",
                  "Methodology": "MMM",
                  "Enabled": false
                }
              ]
            }
            """);

        var loader = new ConfigurationLoader(dir);
        var kzs = loader.LoadKillzones();

        Assert.False(kzs[0].Enabled);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case8_SettingsCorrupted_ReturnsDefaultWithoutException()
    {
        string dir = CreateTempConfigDir();
        Directory.CreateDirectory(dir);
        string path = ConfigurationPaths.Settings(dir);
        File.WriteAllText(path, "{ Corrupted JSON... ");

        var loader = new ConfigurationLoader(dir);
        var settings = loader.LoadSettings();

        Assert.NotNull(settings);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case9_KillzonesCorrupted_ReturnsDefaultsWithoutException()
    {
        string dir = CreateTempConfigDir();
        Directory.CreateDirectory(dir);
        string path = ConfigurationPaths.Killzones(dir);
        File.WriteAllText(path, "This is not valid JSON");

        var loader = new ConfigurationLoader(dir);
        var kzs = loader.LoadKillzones();

        Assert.Equal(5, kzs.Count);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case10_TimeOnlyParsedCorrectly()
    {
        string dir = CreateTempConfigDir();
        Directory.CreateDirectory(dir);
        string path = ConfigurationPaths.Killzones(dir);
        File.WriteAllText(path, """
            {
              "Killzones": [
                {
                  "Name": "Custom KZ",
                  "StartUtc": "06:00",
                  "EndUtc": "11:00",
                  "Color": "#FFFFFF",
                  "Methodology": "MMM",
                  "Enabled": true
                }
              ]
            }
            """);

        var loader = new ConfigurationLoader(dir);
        var kzs = loader.LoadKillzones();

        Assert.Equal(new TimeOnly(6, 0), kzs[0].StartUtc);
        Directory.Delete(dir, true);
    }
}
