namespace ForexWidget.Infrastructure.Configuration;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ForexWidget.Domain.Interfaces;
using ForexWidget.Domain.Models;
using ForexWidget.Infrastructure.Dto;

public class ConfigurationLoader : IConfigurationLoader
{
    private readonly string _configDirectory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public ConfigurationLoader(string? configDirectory = null)
    {
        _configDirectory = configDirectory ?? Path.Combine(AppPaths.DataRoot, "Config");
        Directory.CreateDirectory(_configDirectory);
    }

    public AppSettings LoadSettings()
    {
        string path = ConfigurationPaths.Settings(_configDirectory);
        try
        {
            if (!File.Exists(path))
            {
                var defaultSettings = AppSettings.Default;
                var dto = new AppSettingsDto
                {
                    Theme = defaultSettings.Theme,
                    AlwaysOnTop = defaultSettings.AlwaysOnTop,
                    Opacity = defaultSettings.Opacity,
                    TimeDisplay = defaultSettings.TimeDisplay,
                    HolidayProvider = defaultSettings.HolidayProvider,
                    FinnhubApiKey = defaultSettings.FinnhubApiKey,
                    FMPApiKey = defaultSettings.FMPApiKey,
                    Language = defaultSettings.Language
                };
                File.WriteAllText(path, JsonSerializer.Serialize(dto, JsonOptions));
                return defaultSettings;
            }

            var json = File.ReadAllText(path);
            var loadedDto = JsonSerializer.Deserialize<AppSettingsDto>(json, JsonOptions);
            
            if (loadedDto == null) return AppSettings.Default;
            
            return new AppSettings(
                Theme: loadedDto.Theme,
                AlwaysOnTop: loadedDto.AlwaysOnTop,
                Opacity: loadedDto.Opacity,
                TimeDisplay: loadedDto.TimeDisplay,
                HolidayProvider: loadedDto.HolidayProvider,
                FinnhubApiKey: loadedDto.FinnhubApiKey,
                FMPApiKey: loadedDto.FMPApiKey,
                Language: loadedDto.Language
            );
        }
        catch
        {
            return AppSettings.Default;
        }
    }

    public IReadOnlyList<KillzoneDefinition> LoadKillzones()
    {
        string path = ConfigurationPaths.Killzones(_configDirectory);
        try
        {
            if (!File.Exists(path))
            {
                var defaults = DefaultKillzones();
                var dtoWrapper = new KillzoneDtoWrapper
                {
                    Killzones = defaults.Select(d => new KillzoneDto
                    {
                        Name = d.Name,
                        StartUtc = d.StartUtc.ToString("HH:mm"),
                        EndUtc = d.EndUtc.ToString("HH:mm"),
                        Color = d.Color,
                        Methodology = d.Methodology,
                        Enabled = d.Enabled
                    }).ToList()
                };
                File.WriteAllText(path, JsonSerializer.Serialize(dtoWrapper, JsonOptions));
                return defaults;
            }

            var json = File.ReadAllText(path);
            var loadedDto = JsonSerializer.Deserialize<KillzoneDtoWrapper>(json, JsonOptions);
            
            if (loadedDto?.Killzones == null) return DefaultKillzones();

            var result = new List<KillzoneDefinition>();
            foreach (var kz in loadedDto.Killzones)
            {
                TimeOnly start = TimeOnly.ParseExact(kz.StartUtc, "HH:mm", null);
                TimeOnly end = TimeOnly.ParseExact(kz.EndUtc, "HH:mm", null);
                result.Add(new KillzoneDefinition(kz.Name, start, end, kz.Color, kz.Methodology, kz.Enabled));
            }
            return result;
        }
        catch
        {
            return DefaultKillzones();
        }
    }

    public IReadOnlyList<AlertDefinition> LoadAlerts()
    {
        string path = ConfigurationPaths.Alerts(_configDirectory);
        try
        {
            if (!File.Exists(path))
            {
                var defaults = DefaultAlerts();
                var dtoWrapper = new AlertDtoWrapper
                {
                    Alerts = defaults.Select(a => new AlertDto
                    {
                        Event = a.Event,
                        MinutesBefore = a.MinutesBefore,
                        Enabled = a.Enabled
                    }).ToList()
                };
                File.WriteAllText(path, JsonSerializer.Serialize(dtoWrapper, JsonOptions));
                return defaults;
            }

            var json = File.ReadAllText(path);
            var loadedDto = JsonSerializer.Deserialize<AlertDtoWrapper>(json, JsonOptions);
            
            if (loadedDto?.Alerts == null) return DefaultAlerts();

            return loadedDto.Alerts.Select(a => new AlertDefinition(a.Event, a.MinutesBefore, a.Enabled)).ToList();
        }
        catch
        {
            return DefaultAlerts();
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            var dto = new AppSettingsDto
            {
                Theme = settings.Theme,
                AlwaysOnTop = settings.AlwaysOnTop,
                Opacity = settings.Opacity,
                TimeDisplay = settings.TimeDisplay,
                HolidayProvider = settings.HolidayProvider,
                FinnhubApiKey = settings.FinnhubApiKey,
                FMPApiKey = settings.FMPApiKey,
                Language = settings.Language
            };
            File.WriteAllText(ConfigurationPaths.Settings(_configDirectory),
                JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch
        {
            // Un fallo de escritura de configuración no debe tumbar el widget
        }
    }

    public void SaveKillzones(IReadOnlyList<KillzoneDefinition> killzones)
    {
        try
        {
            var dtoWrapper = new KillzoneDtoWrapper
            {
                Killzones = killzones.Select(d => new KillzoneDto
                {
                    Name = d.Name,
                    StartUtc = d.StartUtc.ToString("HH:mm"),
                    EndUtc = d.EndUtc.ToString("HH:mm"),
                    Color = d.Color,
                    Methodology = d.Methodology,
                    Enabled = d.Enabled
                }).ToList()
            };
            File.WriteAllText(ConfigurationPaths.Killzones(_configDirectory),
                JsonSerializer.Serialize(dtoWrapper, JsonOptions));
        }
        catch
        {
            // Un fallo de escritura de configuración no debe tumbar el widget
        }
    }

    private static IReadOnlyList<KillzoneDefinition> DefaultKillzones() =>
    [
        new("London Open",       new TimeOnly( 6, 0), new TimeOnly( 9, 0), "#00AA00", "MMM", true),
        new("New York Open",     new TimeOnly(12, 0), new TimeOnly(15, 0), "#FF8800", "MMM", true),
        new("London-NY Overlap", new TimeOnly(12, 0), new TimeOnly(16, 0), "#00FFAA", "MMM", true),
        new("Asian Killzone",    new TimeOnly( 0, 0), new TimeOnly( 3, 0), "#4488FF", "MMM", true),
        new("London Close",      new TimeOnly(15, 0), new TimeOnly(16, 0), "#FF4444", "MMM", true),
    ];

    private static IReadOnlyList<AlertDefinition> DefaultAlerts() =>
    [
        new("LondonOpen",     10, true),
        new("NYOpen",         15, true),
        new("LondonClose",    10, true),
        new("KillzoneStart",   5, true),
        new("HighImpactNews", 15, true),
        new("USHoliday",       0, true),
        new("WeekendClose",   30, true),
    ];
}
