namespace ForexWidget.Tests;

using ForexWidget.Domain.Models;
using ForexWidget.Infrastructure.Configuration;
using System;
using System.IO;
using System.Linq;
using Xunit;

public class ConfigurationSaveTests
{
    private static string CreateTempConfigDir()
        => Path.Combine(Path.GetTempPath(), "ForexWidgetTest_" + Guid.NewGuid());

    [Fact]
    public void Case1_SaveSettingsThenLoad_PersistsThemeAndOpacity()
    {
        string dir = CreateTempConfigDir();
        var loader = new ConfigurationLoader(dir);
        var updated = AppSettings.Default with { Theme = "Light", Opacity = 0.7 };

        loader.SaveSettings(updated);
        var loaded = loader.LoadSettings();

        Assert.Equal("Light", loaded.Theme);
        Assert.Equal(0.7, loaded.Opacity);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Case2_SaveKillzonesWithToggledEnabled_PersistsChange()
    {
        string dir = CreateTempConfigDir();
        var loader = new ConfigurationLoader(dir);
        var killzones = loader.LoadKillzones(); // crea los 5 defaults

        var toggled = killzones
            .Select(k => k.Name == "Asian Killzone" ? k with { Enabled = false } : k)
            .ToList();
        loader.SaveKillzones(toggled);
        var reloaded = loader.LoadKillzones();

        Assert.Equal(killzones.Count, reloaded.Count);
        Assert.False(reloaded.First(k => k.Name == "Asian Killzone").Enabled);
        Assert.True(reloaded.First(k => k.Name == "London Open").Enabled);
        Directory.Delete(dir, true);
    }
}
